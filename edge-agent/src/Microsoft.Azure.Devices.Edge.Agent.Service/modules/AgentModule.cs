// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Agent.Service.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.Azure.Devices.Edge.Agent.Core;
    using Microsoft.Azure.Devices.Edge.Agent.Core.Planners;
    using Microsoft.Azure.Devices.Edge.Agent.Core.PlanRunners;
    using Microsoft.Azure.Devices.Edge.Agent.Core.Serde;
    using Microsoft.Azure.Devices.Edge.Agent.Docker;
    using Microsoft.Azure.Devices.Edge.Storage;
    using Microsoft.Azure.Devices.Edge.Storage.RocksDb;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Extensions.Logging;

    public class AgentModule : Module
    {
        readonly int maxRestartCount;
        readonly TimeSpan intensiveCareTime;
        readonly int coolOffTimeUnitInSeconds;
        readonly bool usePersistentStorage;
        readonly string storagePath;
        const string DockerType = "docker";

        static Dictionary<Type, IDictionary<string, Type>> DeploymentConfigTypeMapping
        {
            get
            {
                var moduleDeserializerTypes = new Dictionary<string, Type>
                {
                    [DockerType] = typeof(DockerDesiredModule)
                };

                var edgeAgentDeserializerTypes = new Dictionary<string, Type>
                {
                    [DockerType] = typeof(EdgeAgentDockerModule)
                };

                var edgeHubDeserializerTypes = new Dictionary<string, Type>
                {
                    [DockerType] = typeof(EdgeHubDockerModule)
                };

                var runtimeInfoDeserializerTypes = new Dictionary<string, Type>
                {
                    [DockerType] = typeof(DockerRuntimeInfo)
                };

                var deserializerTypesMap = new Dictionary<Type, IDictionary<string, Type>>
                {
                    [typeof(IModule)] = moduleDeserializerTypes,
                    [typeof(IEdgeAgentModule)] = edgeAgentDeserializerTypes,
                    [typeof(IEdgeHubModule)] = edgeHubDeserializerTypes,
                    [typeof(IRuntimeInfo)] = runtimeInfoDeserializerTypes,
                };
                return deserializerTypesMap;
            }
        }

        public AgentModule(int maxRestartCount, TimeSpan intensiveCareTime, int coolOffTimeUnitInSeconds,
            bool usePersistentStorage, string storagePath)
        {
            this.maxRestartCount = maxRestartCount;
            this.intensiveCareTime = intensiveCareTime;
            this.coolOffTimeUnitInSeconds = coolOffTimeUnitInSeconds;
            this.usePersistentStorage = usePersistentStorage;
            this.storagePath = usePersistentStorage ? Preconditions.CheckNonWhiteSpace(storagePath, nameof(storagePath)) : storagePath;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // ISerde<Diff>
            builder.Register(c => new DiffSerde(
                    new Dictionary<string, Type>
                    {
                        { DockerType, typeof(DockerModule) }
                    }
                ))
                .As<ISerde<Diff>>()
                .SingleInstance();

            // ISerde<ModuleSet>
            builder.Register(c => new ModuleSetSerde(
                    new Dictionary<string, Type>
                    {
                        { DockerType, typeof(DockerModule) }
                    }
                ))
                .As<ISerde<ModuleSet>>()
                .SingleInstance();

            // ISerde<DeploymentConfig>
            builder.Register(
                c =>
                {
                    ISerde<DeploymentConfig> serde = new TypeSpecificSerDe<DeploymentConfig>(DeploymentConfigTypeMapping);
                    return serde;
                })
                .As<ISerde<DeploymentConfig>>()
                .SingleInstance();

            // ISerde<DeploymentConfigInfo>
            builder.Register(
                c =>
                {
                    ISerde<DeploymentConfigInfo> serde = new TypeSpecificSerDe<DeploymentConfigInfo>(DeploymentConfigTypeMapping);
                    return serde;
                })
                .As<ISerde<DeploymentConfigInfo>>()
                .SingleInstance();

            // Detect system environment
            builder.Register(c => new SystemEnvironment())
                .As<ISystemEnvironment>()
                .SingleInstance();

            // IRocksDbOptionsProvider
            builder.Register(c => new RocksDbOptionsProvider(c.Resolve<ISystemEnvironment>()))
                .As<IRocksDbOptionsProvider>()
                .SingleInstance();

            // IDbStore
            builder.Register(
                c =>
                {
                    var loggerFactory = c.Resolve<ILoggerFactory>();
                    ILogger logger = loggerFactory.CreateLogger(typeof(AgentModule));

                    if (this.usePersistentStorage)
                    {
                        // Create partition for mma
                        var partitionsList = new List<string> { "moduleState", "deploymentConfig" };
                        try
                        {
                            IDbStoreProvider dbStoreprovider = DbStoreProvider.Create(c.Resolve<IRocksDbOptionsProvider>(),
                                this.storagePath, partitionsList);
                            logger.LogInformation($"Created persistent store at {this.storagePath}");
                            return dbStoreprovider;
                        }
                        catch (Exception ex) when (!ExceptionEx.IsFatal(ex))
                        {
                            logger.LogError(ex, "Error creating RocksDB store. Falling back to in-memory store.");
                            return new InMemoryDbStoreProvider();
                        }
                    }
                    else
                    {
                        logger.LogInformation($"Using in-memory store");
                        return new InMemoryDbStoreProvider();
                    }
                })
                .As<IDbStoreProvider>()
                .SingleInstance();

            // IStoreProvider
            builder.Register(c => new StoreProvider(c.Resolve<IDbStoreProvider>()))
                .As<IStoreProvider>()
                .SingleInstance();

            // IEntityStore<string, ModuleState>
            builder.Register(c => c.Resolve<IStoreProvider>().GetEntityStore<string, ModuleState>("moduleState"))
                .As<IEntityStore<string, ModuleState>>()
                .SingleInstance();

            // IEntityStore<string, DeploymentConfigInfo>
            builder.Register(c => c.Resolve<IStoreProvider>().GetEntityStore<string, string>("deploymentConfig"))
                .As<IEntityStore<string, string>>()
                .SingleInstance();

            // IRestartManager
            builder.Register(c => new RestartPolicyManager(this.maxRestartCount, this.coolOffTimeUnitInSeconds))
                .As<IRestartPolicyManager>()
                .SingleInstance();

            // IPlanner
            builder.Register(async c => new HealthRestartPlanner(
                    await c.Resolve<Task<ICommandFactory>>(),
                    c.Resolve<IEntityStore<string, ModuleState>>(),
                    this.intensiveCareTime,
                    c.Resolve<IRestartPolicyManager>()
                ) as IPlanner)
                .As<Task<IPlanner>>()
                .SingleInstance();

            // IPlanRunner
            builder.Register(c => new OrderedRetryPlanRunner(this.maxRestartCount, this.coolOffTimeUnitInSeconds, SystemTime.Instance))
                .As<IPlanRunner>()
                .SingleInstance();

            // Task<Agent>
            builder.Register(
                async c =>
                {
                    var configSource = c.Resolve<Task<IConfigSource>>();
                    var environmentProvider = c.Resolve<Task<IEnvironmentProvider>>();
                    var planner = c.Resolve<Task<IPlanner>>();
                    var planRunner = c.Resolve<IPlanRunner>();
                    var reporter = c.Resolve<IReporter>();
                    var moduleIdentityLifecycleManager = c.Resolve<IModuleIdentityLifecycleManager>();
                    var deploymentConfigInfoSerde = c.Resolve<ISerde<DeploymentConfigInfo>>();
                    var deploymentConfigInfoStore = c.Resolve<IEntityStore<string, string>>();
                    return await Agent.Create(
                        await configSource,
                        await planner,
                        planRunner,
                        reporter,
                        moduleIdentityLifecycleManager,
                        await environmentProvider,
                        deploymentConfigInfoStore,
                        deploymentConfigInfoSerde);
                })
                .As<Task<Agent>>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}

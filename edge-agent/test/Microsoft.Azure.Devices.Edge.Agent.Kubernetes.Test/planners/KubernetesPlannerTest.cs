// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Agent.Kubernetes.Test.Planners
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using k8s;
    using Microsoft.Azure.Devices.Edge.Agent.Core;
    using Microsoft.Azure.Devices.Edge.Agent.Docker;
    using Microsoft.Azure.Devices.Edge.Agent.Kubernetes.Commands;
    using Microsoft.Azure.Devices.Edge.Agent.Kubernetes.Planners;
    using Microsoft.Azure.Devices.Edge.Util.Test.Common;
    using Moq;
    using Xunit;

    [Unit]
    public class KubernetesPlannerTest
    {
        const string Ns = "namespace";
        const string Hostname = "hostname";
        const string DeviceId = "deviceId";
        static readonly IDictionary<string, EnvVal> EnvVars = new Dictionary<string, EnvVal>();
        static readonly DockerConfig Config1 = new DockerConfig("image1");
        static readonly DockerConfig Config2 = new DockerConfig("image2");
        static readonly ConfigurationInfo DefaultConfigurationInfo = new ConfigurationInfo("1");
        static readonly IRuntimeInfo RuntimeInfo = Mock.Of<IRuntimeInfo>();
        static readonly IKubernetes DefaultClient = Mock.Of<IKubernetes>();
        static readonly ICommandFactory DefaultCommandFactory = new KubernetesCommandFactory();
        static readonly ICombinedConfigProvider<CombinedDockerConfig> DefaultConfigProvider = Mock.Of<ICombinedConfigProvider<CombinedDockerConfig>>();

        [Fact]
        [Unit]
        public void ContructorThrowsOnNull()
        {
            Assert.Throws<ArgumentException>(() => new KubernetesPlanner<CombinedDockerConfig>(null, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider));
            Assert.Throws<ArgumentException>(() => new KubernetesPlanner<CombinedDockerConfig>(string.Empty, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider));
            Assert.Throws<ArgumentException>(() => new KubernetesPlanner<CombinedDockerConfig>(Ns, null,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider));
            Assert.Throws<ArgumentException>(() => new KubernetesPlanner<CombinedDockerConfig>(Ns, "  ",  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider));
            Assert.Throws<ArgumentException>(() => new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  null, DefaultClient, DefaultCommandFactory, DefaultConfigProvider));
            Assert.Throws<ArgumentException>(() => new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  string.Empty, DefaultClient, DefaultCommandFactory, DefaultConfigProvider));
            Assert.Throws<ArgumentNullException>(() => new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, null, DefaultCommandFactory, DefaultConfigProvider));
            Assert.Throws<ArgumentNullException>(() => new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, null, DefaultConfigProvider));
            Assert.Throws<ArgumentNullException>(() => new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, null));
        }

        [Fact]
        [Unit]
        public async void KubernetesPlannerNoModulesNoPlan()
        {
            var planner = new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider);
            Plan addPlan = await planner.PlanAsync(ModuleSet.Empty, ModuleSet.Empty, RuntimeInfo, ImmutableDictionary<string, IModuleIdentity>.Empty);
            Assert.Equal(Plan.Empty, addPlan);
        }

        [Fact]
        [Unit]
        public async void KubernetesPlannerPlanFailsWithNonDistinctModules()
        {
            IModule m1 = new DockerModule("module1", "v1", ModuleStatus.Running, RestartPolicy.Always, Config1, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars);
            IModule m2 = new DockerModule("Module1", "v1", ModuleStatus.Running, RestartPolicy.Always, Config1, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars);
            ModuleSet addRunning = ModuleSet.Create(m1, m2);

            var planner = new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider);

            await Assert.ThrowsAsync<InvalidIdentityException>( () => planner.PlanAsync(addRunning, ModuleSet.Empty, RuntimeInfo, ImmutableDictionary<string, IModuleIdentity>.Empty));
        }

        [Fact]
        [Unit]
        public async void KubernetesPlannerPlanFailsWithNonDockerModules()
        {
            IModule m1 = new NonDockerModule("module1", "v1", "unknown", ModuleStatus.Running, RestartPolicy.Always, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars, string.Empty);
            ModuleSet addRunning = ModuleSet.Create(m1);

            var planner = new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider);
            await Assert.ThrowsAsync<InvalidModuleException>(() => planner.PlanAsync(addRunning, ModuleSet.Empty, RuntimeInfo, ImmutableDictionary<string, IModuleIdentity>.Empty));
        }

        [Fact]
        [Unit]
        public async void KubernetesPlannerEmptyPlanWhenNoChanges()
        {
            IModule m1 = new DockerModule("module1", "v1", ModuleStatus.Running, RestartPolicy.Always, Config1, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars);
            IModule m2 = new DockerModule("module2", "v1", ModuleStatus.Running, RestartPolicy.Always, Config1, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars);
            ModuleSet desired = ModuleSet.Create(m1, m2);
            ModuleSet current = ModuleSet.Create(m1, m2);

            var planner = new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider);
            var plan = await planner.PlanAsync(desired, current, RuntimeInfo, ImmutableDictionary<string, IModuleIdentity>.Empty);
            Assert.Equal(Plan.Empty, plan);
        }

        [Fact]
        [Unit]
        public async void KubernetesPlannerPlanExistsWhenChangesMade()
        {
            IModule m1 = new DockerModule("module1", "v1", ModuleStatus.Running, RestartPolicy.Always, Config1, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars);
            IModule m2 = new DockerModule("module2", "v1", ModuleStatus.Running, RestartPolicy.Always, Config1, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars);
            ModuleSet desired = ModuleSet.Create(m1);
            ModuleSet current = ModuleSet.Create(m2);

            var planner = new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider);
            var plan = await planner.PlanAsync(desired, current, RuntimeInfo, ImmutableDictionary<string, IModuleIdentity>.Empty);
            Assert.Single(plan.Commands);
            Assert.True(plan.Commands.First() is KubernetesCrdCommand<CombinedDockerConfig>);
        }

        [Fact]
        [Unit]
        public async void KubernetesPlannerShutdownTest()
        {
            IModule m1 = new DockerModule("module1", "v1", ModuleStatus.Running, RestartPolicy.Always, Config1, ImagePullPolicy.OnCreate, DefaultConfigurationInfo, EnvVars);
            ModuleSet current = ModuleSet.Create(m1);

            var planner = new KubernetesPlanner<CombinedDockerConfig>(Ns, Hostname,  DeviceId, DefaultClient, DefaultCommandFactory, DefaultConfigProvider);
            var plan = await planner.CreateShutdownPlanAsync(current);
            Assert.Equal(Plan.Empty, plan);
        }

        class NonDockerModule : IModule<string>
        {
            public NonDockerModule(string name, string version, string type, ModuleStatus desiredStatus, RestartPolicy restartPolicy, ImagePullPolicy imagePullPolicy, ConfigurationInfo configurationInfo, IDictionary<string, EnvVal> env, string config)
            {
                this.Name = name;
                this.Version = version;
                this.Type = type;
                this.DesiredStatus = desiredStatus;
                this.RestartPolicy = restartPolicy;
                this.ImagePullPolicy = imagePullPolicy;
                this.ConfigurationInfo = configurationInfo;
                this.Env = env;
                this.Config = config;
            }

            public bool Equals(IModule other) => throw new NotImplementedException();

            public string Name { get; set; }

            public string Version { get; }

            public string Type { get; }

            public ModuleStatus DesiredStatus { get; }

            public RestartPolicy RestartPolicy { get; }

            public ImagePullPolicy ImagePullPolicy { get; }

            public ConfigurationInfo ConfigurationInfo { get; }

            public IDictionary<string, EnvVal> Env { get; }

            public bool IsOnlyModuleStatusChanged(IModule other) => throw new NotImplementedException();

            public bool Equals(IModule<string> other) => throw new NotImplementedException();

            public string Config { get; }
        }
    }
}

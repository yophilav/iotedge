// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Hub.Core
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Edge.Hub.Core.Cloud;
    using Microsoft.Azure.Devices.Edge.Hub.Core.Device;
    using Microsoft.Azure.Devices.Edge.Storage;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Azure.Devices.Edge.Util.Concurrency;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;

    public class TwinManager : ITwinManager
    {
        const int TwinPropertyMaxDepth = 5; // taken from IoTHub
        const int TwinPropertyNameMaxLength = 512; // bytes. taken from IoTHub
        const long TwinPropertyMaxSafeValue = 4503599627370495; // (2^52) - 1. taken from IoTHub
        const long TwinPropertyMinSafeValue = -4503599627370496; // -2^52. taken from IoTHub
        const int TwinPropertyDocMaxLength = 8 * 1024; // 8K bytes. taken from IoTHub
        readonly IMessageConverter<TwinCollection> twinCollectionConverter;
        readonly IMessageConverter<Twin> twinConverter;
        readonly IConnectionManager connectionManager;
        readonly AsyncLock reportedPropertiesLock;
        readonly AsyncLock twinLock;
        readonly ActionBlock<IIdentity> actionBlock;
        internal Option<IEntityStore<string, TwinInfo>> TwinStore { get; }

        public TwinManager(IConnectionManager connectionManager, IMessageConverter<TwinCollection> twinCollectionConverter, IMessageConverter<Twin> twinConverter, Option<IEntityStore<string, TwinInfo>> twinStore)
        {
            this.connectionManager = Preconditions.CheckNotNull(connectionManager, nameof(connectionManager));
            this.twinCollectionConverter = Preconditions.CheckNotNull(twinCollectionConverter, nameof(twinCollectionConverter));
            this.twinConverter = Preconditions.CheckNotNull(twinConverter, nameof(twinConverter));
            this.TwinStore = Preconditions.CheckNotNull(twinStore, nameof(twinStore));
            this.reportedPropertiesLock = new AsyncLock();
            this.twinLock = new AsyncLock();
            this.actionBlock = new ActionBlock<IIdentity>(this.ProcessConnectionEstablishedForDevice);
        }

        public static ITwinManager CreateTwinManager(IConnectionManager connectionManager, IMessageConverterProvider messageConverterProvider, Option<IStoreProvider> storeProvider)
        {
            Preconditions.CheckNotNull(connectionManager, nameof(connectionManager));
            Preconditions.CheckNotNull(messageConverterProvider, nameof(messageConverterProvider));
            Preconditions.CheckNotNull(storeProvider, nameof(storeProvider));
            TwinManager twinManager = new TwinManager(connectionManager, messageConverterProvider.Get<TwinCollection>(), messageConverterProvider.Get<Twin>(),
                storeProvider.Match(
                    s => Option.Some(s.GetEntityStore<string, TwinInfo>(Constants.TwinStorePartitionKey)),
                    () => Option.None<IEntityStore<string, TwinInfo>>()));
            connectionManager.CloudConnectionEstablished += twinManager.ConnectionEstablishedCallback;
            connectionManager.CloudConnectionLost += twinManager.ConnectionLostCallback;
            return twinManager;
        }

        async Task ProcessConnectionEstablishedForDevice(IIdentity identity)
        {
            // Report pending reported properties up to the cloud

            using (await this.reportedPropertiesLock.LockAsync())
            {
                await this.TwinStore.Match(
                    async (store) =>
                    {
                        Option<TwinInfo> twinInfo = await store.Get(identity.Id);
                        await twinInfo.Match(
                            async (t) =>
                            {
                                if (t.ReportedPropertiesPatch.Count != 0)
                                {
                                    IMessage reported = this.twinCollectionConverter.ToMessage(t.ReportedPropertiesPatch);
                                    await this.SendReportedPropertiesToCloudProxy(identity.Id, reported);
                                    await store.Update(identity.Id, u => new TwinInfo(u.Twin, null, u.SubscribedToDesiredPropertyUpdates));
                                    Events.ReportedPropertiesSyncedToCloudSuccess(identity.Id, t.ReportedPropertiesPatch.Version);
                                }
                            },
                            () => { return Task.CompletedTask; });
                    },
                    () => { return Task.CompletedTask; }
                    );
            }

            // Refresh local copy of the twin
            Option<ICloudProxy> cloudProxy = this.connectionManager.GetCloudConnection(identity.Id);
            await cloudProxy.Map<Task>(
                (cp) =>
                    this.GetTwinInfoWhenCloudOnlineAsync(identity.Id, cp, true /* send update to device */)
                ).GetOrElse(Task.CompletedTask);
        }

        internal void ConnectionEstablishedCallback(object sender, IIdentity identity)
        {
            Events.ConnectionEstablished(identity.Id);
            this.actionBlock.Post(identity);
        }

        void ConnectionLostCallback(object sender, IIdentity identity)
        {
            Events.ConnectionLost(identity.Id);
        }

        public async Task<IMessage> GetTwinAsync(string id)
        {
            return await this.TwinStore.Match(
                async (store) =>
                {
                    TwinInfo twinInfo = await this.GetTwinInfoWithStoreSupportAsync(id);
                    return this.twinConverter.ToMessage(twinInfo.Twin);
                },
                async () =>
                {
                    // pass through to cloud proxy
                    Option<ICloudProxy> cloudProxy = this.connectionManager.GetCloudConnection(id);
                    return await cloudProxy.Match(async (cp) => await cp.GetTwinAsync(), () => throw new InvalidOperationException($"Cloud proxy unavailable for device {id}"));
                });
        }

        async Task<TwinInfo> GetTwinInfoWithStoreSupportAsync(string id)
        {
            try
            {
                Option<ICloudProxy> cloudProxy = this.connectionManager.GetCloudConnection(id);
                return await cloudProxy.Map(
                        cp => this.GetTwinInfoWhenCloudOnlineAsync(id, cp, false)
                    ).GetOrElse(() => this.GetTwinInfoWhenCloudOfflineAsync(id, new InvalidOperationException($"Error accessing cloud proxy for device {id}")));
            }
            catch (Exception e)
            {
                return await this.GetTwinInfoWhenCloudOfflineAsync(id, e);
            }
        }

        internal async Task ExecuteOnTwinStoreResultAsync(string id, Func<TwinInfo, Task> twinStoreHit, Func<Task> twinStoreMiss)
        {
            Option<TwinInfo> cached = await this.TwinStore.Match(s => s.Get(id), () => throw new InvalidOperationException("Missing twin store"));
            await cached.Match(c => twinStoreHit(c), () => twinStoreMiss());
        }

        public async Task UpdateDesiredPropertiesAsync(string id, IMessage desiredProperties)
        {
            await this.TwinStore.Map(
                    s => this.UpdateDesiredPropertiesWithStoreSupportAsync(id, desiredProperties)
                ).GetOrElse(() => this.SendDesiredPropertiesToDeviceProxy(id, desiredProperties));
        }

        async Task SendDesiredPropertiesToDeviceProxy(string id, IMessage desired)
        {
            Option<IDeviceProxy> deviceProxy = this.connectionManager.GetDeviceConnection(id);
            await deviceProxy.Match(dp => dp.OnDesiredPropertyUpdates(desired), () => throw new InvalidOperationException($"Device proxy unavailable for device {id}"));
        }

        async Task UpdateDesiredPropertiesWithStoreSupportAsync(string id, IMessage desiredProperties)
        {
            try
            {
                TwinCollection desired = this.twinCollectionConverter.FromMessage(desiredProperties);
                await this.ExecuteOnTwinStoreResultAsync(
                    id,
                    t => this.UpdateDesiredPropertiesWhenTwinStoreHasTwinAsync(id, desired),
                    () => this.UpdateDesiredPropertiesWhenTwinStoreNeedsTwinAsync(id, desired));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error processing desired properties for device {id}", e);
            }
        }

        async Task UpdateDesiredPropertiesWhenTwinStoreHasTwinAsync(string id, TwinCollection desired)
        {
            bool getTwin = false;
            IMessage message = this.twinCollectionConverter.ToMessage(desired);
            using (await this.twinLock.LockAsync())
            {
                await this.TwinStore.Match(
                    (s) => s.Update(
                        id,
                        u =>
                        {
                            // Save the patch only if it is the next one that can be applied
                            if (desired.Version == u.Twin.Properties.Desired.Version + 1)
                            {
                                string mergedJson = JsonEx.Merge(u.Twin.Properties.Desired, desired, /*treatNullAsDelete*/ true);
                                u.Twin.Properties.Desired = new TwinCollection(mergedJson);
                            }
                            else
                            {
                                getTwin = true;
                            }
                            return new TwinInfo(u.Twin, u.ReportedPropertiesPatch, true);
                        }),
                    () => throw new InvalidOperationException("Missing twin store"));
            }

            // Refresh local copy of the twin since we received an out-of-order patch
            if (getTwin)
            {
                Option<ICloudProxy> cloudProxy = this.connectionManager.GetCloudConnection(id);
                await cloudProxy.Map<Task>(
                        cp => this.GetTwinInfoWhenCloudOnlineAsync(id, cp, true /* send update to device */)
                    ).GetOrElse(Task.CompletedTask);
            }
            else
            {
                await this.SendDesiredPropertiesToDeviceProxy(id, message);
            }
        }

        async Task UpdateDesiredPropertiesWhenTwinStoreNeedsTwinAsync(string id, TwinCollection desired)
        {
            await this.GetTwinInfoWithStoreSupportAsync(id);
            await this.UpdateDesiredPropertiesWhenTwinStoreHasTwinAsync(id, desired);
        }

        internal async Task<TwinInfo> GetTwinInfoWhenCloudOnlineAsync(string id, ICloudProxy cp, bool sendDesiredPropertyUpdate)
        {
            TwinCollection diff = null;
            // Used for returning value to caller
            TwinInfo cached = null;

            using (await this.twinLock.LockAsync())
            {
                IMessage twinMessage = await cp.GetTwinAsync();
                Twin cloudTwin = twinConverter.FromMessage(twinMessage);
                Events.GotTwinFromCloudSuccess(id, cloudTwin.Properties.Desired.Version, cloudTwin.Properties.Reported.Version);
                TwinInfo newTwin = new TwinInfo(cloudTwin, null, false);
                cached = newTwin;
                await this.TwinStore.Match(
                    (s) => s.PutOrUpdate(
                        id,
                        newTwin,
                        t =>
                        {
                            // If the new twin is more recent than the cached twin, update the cached copy.
                            // If not, reject the cloud twin
                            if ((cloudTwin.Properties.Desired.Version > t.Twin.Properties.Desired.Version) ||
                                (cloudTwin.Properties.Reported.Version > t.Twin.Properties.Reported.Version))
                            {
                                Events.UpdateCachedTwin(id,
                                    t.Twin.Properties.Desired.Version, cloudTwin.Properties.Desired.Version,
                                    t.Twin.Properties.Reported.Version, cloudTwin.Properties.Reported.Version);
                                cached = new TwinInfo(cloudTwin, t.ReportedPropertiesPatch, t.SubscribedToDesiredPropertyUpdates);
                                // If the device is subscribed to desired property updates and we are refreshing twin as a result
                                // of a connection reset or desired property update, send a patch to the downstream device
                                if (sendDesiredPropertyUpdate && t.SubscribedToDesiredPropertyUpdates)
                                {
                                    Events.SendDesiredPropertyUpdateToSubscriber(id,
                                        t.Twin.Properties.Desired.Version, cloudTwin.Properties.Desired.Version);
                                    diff = new TwinCollection(JsonEx.Diff(t.Twin.Properties.Desired, cloudTwin.Properties.Desired));
                                }
                            }
                            else
                            {
                                Events.PreserveCachedTwin(id,
                                    t.Twin.Properties.Desired.Version, cloudTwin.Properties.Desired.Version,
                                    t.Twin.Properties.Reported.Version, cloudTwin.Properties.Reported.Version);
                                cached = t;
                            }
                            return cached;
                        }),
                    () => throw new InvalidOperationException("Missing twin store"));
            }
            if (diff != null)
            {
                IMessage message = this.twinCollectionConverter.ToMessage(diff);
                await this.SendDesiredPropertiesToDeviceProxy(id, message);
            }
            return cached;
        }

        async Task<TwinInfo> GetTwinInfoWhenCloudOfflineAsync(string id, Exception e)
        {
            TwinInfo twinInfo = null;
            await this.ExecuteOnTwinStoreResultAsync(
                id,
                t =>
                {
                    twinInfo = t;
                    Events.GetTwinFromStoreWhenOffline(id,
                        twinInfo.Twin.Properties.Desired.Version,
                        twinInfo.Twin.Properties.Reported.Version,
                        e);
                    return Task.CompletedTask;
                },
                () => throw new InvalidOperationException($"Error getting twin for device {id}", e));
            return twinInfo;
        }

        async Task UpdateReportedPropertiesWhenTwinStoreHasTwinAsync(string id, TwinCollection reported, bool cloudVerified)
        {
            using (await this.twinLock.LockAsync())
            {
                await this.TwinStore.Match(
                    (s) => s.Update(
                        id,
                        u =>
                        {
                            string mergedJson = JsonEx.Merge(u.Twin.Properties.Reported, reported, /*treatNullAsDelete*/ true);
                            TwinCollection mergedProperty = new TwinCollection(mergedJson);
                            if (!cloudVerified)
                            {
                                ValidateTwinCollectionSize(mergedProperty);
                            }
                            u.Twin.Properties.Reported = mergedProperty;
                            Events.UpdatedCachedReportedProperties(id, u.Twin.Properties.Reported.Version, cloudVerified);
                            return u;
                        }),
                    () => throw new InvalidOperationException("Missing twin store"));
            }
        }

        async Task UpdateReportedPropertiesWhenTwinStoreNeedsTwinAsync(string id, TwinCollection reported, bool cloudVerified)
        {
            TwinInfo twinInfo = await this.GetTwinInfoWithStoreSupportAsync(id);
            await this.UpdateReportedPropertiesWhenTwinStoreHasTwinAsync(id, reported, cloudVerified);
        }

        public async Task UpdateReportedPropertiesAsync(string id, IMessage reportedProperties)
        {
            await this.TwinStore.Match(
                (s) => this.UpdateReportedPropertiesWithStoreSupportAsync(id, reportedProperties),
                () => this.SendReportedPropertiesToCloudProxy(id, reportedProperties));
        }

        async Task UpdateReportedPropertiesPatch(string id, TwinCollection reportedProperties)
        {
            await this.TwinStore.Match(
                (s) => s.Update(
                    id,
                    u =>
                    {
                        string mergedJson = JsonEx.Merge(u.ReportedPropertiesPatch, reportedProperties, /*treatNullAsDelete*/ false);
                        TwinCollection mergedPatch = new TwinCollection(mergedJson);
                        Events.UpdatingReportedPropertiesPatchCollection(id, mergedPatch.Version);
                        return new TwinInfo(u.Twin, mergedPatch, u.SubscribedToDesiredPropertyUpdates);
                    }),
                () => throw new InvalidOperationException("Missing twin store"));
        }

        async Task UpdateReportedPropertiesWithStoreSupportAsync(string id, IMessage reportedProperties)
        {
            using (await this.reportedPropertiesLock.LockAsync())
            {
                bool updatePatch = false;
                bool cloudVerified = false;
                await this.TwinStore.Match(
                    async (s) =>
                    {
                        Option<TwinInfo> info = await s.Get(id);
                        // If the reported properties patch is not null, we will not attempt to write the reported
                        // properties to the cloud as we are still waiting for a connection established callback
                        // to sync the local reported properties with that of the cloud
                        updatePatch = info.Map((ti) =>
                        {
                            if (ti.ReportedPropertiesPatch.Count != 0)
                            {
                                Events.NeedsUpdateCachedReportedPropertiesPatch(id, ti.ReportedPropertiesPatch.Version);
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }).GetOrElse(false);
                    },
                    () => throw new InvalidOperationException("Missing twin store")
                    );

                TwinCollection reported = this.twinCollectionConverter.FromMessage(reportedProperties);

                if (!updatePatch)
                {
                    try
                    {
                        await this.SendReportedPropertiesToCloudProxy(id, reportedProperties);
                        Events.SentReportedPropertiesToCloud(id, reported.Version);
                        cloudVerified = true;
                    }
                    catch (IotHubException e)
                    {
                        throw new InvalidOperationException("Error sending reported properties to cloud", e);
                    }
                    catch (Exception e)
                    {
                        Events.UpdateReportedToCloudException(id, e);
                        updatePatch = true;
                    }
                }

                if (!cloudVerified)
                {
                    ValidateTwinProperties(JToken.Parse(reported.ToJson()), 1);
                    Events.ValidatedTwinPropertiesSuccess(id, reported.Version);
                }

                // Update the local twin's reported properties
                // At this point, if we are offline and if we somehow missed caching the twin before we
                // went offline, we would call UpdateReportedPropertiesWhenTwinStoreNeedsTwinAsync and
                // it would throw because it was unable to get the twin. This might not be acceptable as we
                // never want a reported properties update to fail even if we are offline. TBD.   
                await this.ExecuteOnTwinStoreResultAsync(
                    id,
                    (t) => this.UpdateReportedPropertiesWhenTwinStoreHasTwinAsync(id, reported, cloudVerified),
                    () => this.UpdateReportedPropertiesWhenTwinStoreNeedsTwinAsync(id, reported, cloudVerified));

                if (updatePatch)
                {
                    // Update the collective patch of reported properties
                    await this.ExecuteOnTwinStoreResultAsync(
                        id,
                        (t) => this.UpdateReportedPropertiesPatch(id, reported),
                        () => throw new InvalidOperationException($"Missing cached twin for device {id}"));
                }
            }
        }

        async Task SendReportedPropertiesToCloudProxy(string id, IMessage reported)
        {
            Option<ICloudProxy> cloudProxy = this.connectionManager.GetCloudConnection(id);
            await cloudProxy.Match(
                async (cp) =>
                {
                    await cp.UpdateReportedPropertiesAsync(reported);
                }, () => throw new InvalidOperationException($"Cloud proxy unavailable for device {id}"));
        }

        static void ValidatePropertyNameAndLength(string name)
        {
            if (name != null && Encoding.UTF8.GetByteCount(name) > TwinPropertyNameMaxLength)
            {
                string truncated = name.Substring(0, 10);
                throw new InvalidOperationException($"Length of property name {truncated}.. exceeds maximum length of {TwinPropertyNameMaxLength}");
            }

            for (int index = 0; index < name.Length; index++)
            {
                char ch = name[index];
                // $ is reserved for service properties like $metadata, $version etc.
                // However, $ is already a reserved character in Mongo, so we need to substitute it with another character like #.
                // So we're also reserving # for service side usage.
                if (char.IsControl(ch) || ch == '.' || ch == '$' || ch == '#' || char.IsWhiteSpace(ch))
                {
                    throw new InvalidOperationException($"Property name {name} contains invalid character '{ch}'");
                }
            }
        }

        static void ValidatePropertyValueLength(string name, string value)
        {
            if (value != null && Encoding.UTF8.GetByteCount(value) > TwinPropertyNameMaxLength)
            {
                throw new InvalidOperationException($"Value associated with property name {name} exceeds maximum length of {TwinPropertyNameMaxLength}");
            }
        }

        static void ValidateIntegerValue(string name, long value)
        {
            if (value > TwinPropertyMaxSafeValue || value < TwinPropertyMinSafeValue)
            {
                throw new InvalidOperationException($"Property {name} has an out of bound value. Valid values are between {TwinPropertyMinSafeValue} and {TwinPropertyMaxSafeValue}");
            }
        }

        static void ValidateValueType(string property, JToken value)
        {
            if (!JsonEx.IsValidToken(value))
            {
                throw new InvalidOperationException($"Property {property} has a value of unsupported type. Valid types are integer, float, string, bool, null and nested object");
            }
        }

        static void ValidateTwinCollectionSize(TwinCollection collection)
        {
            long size = Encoding.UTF8.GetByteCount(collection.ToJson());
            if (size > TwinPropertyDocMaxLength)
            {
                throw new InvalidOperationException($"Twin properties size {size} exceeds maximum {TwinPropertyDocMaxLength}");
            }
        }

        internal static void ValidateTwinProperties(JToken properties)
        {
            ValidateTwinProperties(properties, 1);
        }

        static void ValidateTwinProperties(JToken properties, int currentDepth)
        {
            foreach (var kvp in ((JObject)properties).Properties())
            {
                ValidatePropertyNameAndLength(kvp.Name);

                ValidateValueType(kvp.Name, kvp.Value);

                string s = kvp.Value.ToString();
                if (s != null)
                {
                    ValidatePropertyValueLength(kvp.Name, s);
                }

                if ((kvp.Value is JValue) && (kvp.Value.Type is JTokenType.Integer))
                {
                    ValidateIntegerValue(kvp.Name, (long)kvp.Value);
                }

                if ((kvp.Value != null) && (kvp.Value is JObject))
                {
                    if (currentDepth > TwinPropertyMaxDepth)
                    {
                        throw new InvalidOperationException($"Nested depth of twin property exceeds {TwinPropertyMaxDepth}");
                    }

                    // do validation recursively
                    ValidateTwinProperties(kvp.Value, currentDepth + 1);
                }
            }
        }

        static class Events
        {
            static readonly ILogger Log = Logger.Factory.CreateLogger<TwinManager>();
            const int IdStart = HubCoreEventIds.TwinManager;

            enum EventIds
            {
                UpdateReportedToCloudException = IdStart,
                StoreTwinFailed,
                ReportedPropertiesSyncedToCloudSuccess,
                ValidatedTwinPropertiesSuccess,
                SentReportedPropertiesToCloud,
                NeedsUpdateCachedReportedPropertiesPatch,
                UpdatingReportedPropertiesPatchCollection,
                UpdatedCachedReportedProperties,
                GetTwinFromStoreWhenOffline,
                GotTwinFromCloudSuccess,
                UpdateCachedTwin,
                SendDesiredPropertyUpdateToSubscriber,
                PreserveCachedTwin,
                ConnectionLost,
                ConnectionEstablished
            }

            public static void UpdateReportedToCloudException(string identity, Exception e)
            {
                Log.LogInformation((int)EventIds.UpdateReportedToCloudException, $"Updating reported properties for {identity} in cloud failed with error {e.GetType()} {e.Message}");
            }

            public static void StoreTwinFailed(string identity, Exception e, long v, long desired, long reported)
            {
                Log.LogDebug((int)EventIds.StoreTwinFailed, $"Storing twin for {identity} failed with error {e.GetType()} {e.Message}. Retrieving last stored twin with version {v}, desired version {desired} and reported version {reported}");
            }

            public static void ReportedPropertiesSyncedToCloudSuccess(string identity, long version)
            {
                Log.LogInformation((int)EventIds.ReportedPropertiesSyncedToCloudSuccess, $"Synced cloud's reported properties at version {version} with edge for {identity}");
            }

            public static void ValidatedTwinPropertiesSuccess(string id, long version)
            {
                Log.LogDebug((int)EventIds.ValidatedTwinPropertiesSuccess, $"Successfully validated reported properties of " +
                    $"twin with id {id} and reported properties version {version}");
            }

            public static void SentReportedPropertiesToCloud(string id, long version)
            {
                Log.LogInformation((int)EventIds.SentReportedPropertiesToCloud, $"Successfully sent reported properties to cloud " +
                    $"for {id} and reported properties version {version}");
            }

            public static void NeedsUpdateCachedReportedPropertiesPatch(string id, long version)
            {
                Log.LogDebug((int)EventIds.NeedsUpdateCachedReportedPropertiesPatch, $"Collective reported properties needs " +
                    $"update for {id} and reported properties version {version}");
            }

            public static void UpdatingReportedPropertiesPatchCollection(string id, long version)
            {
                Log.LogDebug((int)EventIds.UpdatingReportedPropertiesPatchCollection, $"Updating collective reported properties " +
                    $"patch for {id} at version {version}");
            }

            public static void UpdatedCachedReportedProperties(string id, long reportedVersion, bool cloudVerified)
            {
                Log.LogDebug((int)EventIds.UpdatedCachedReportedProperties, $"Updated cached reported property for {id} " +
                    $"at reported property version {reportedVersion} cloudVerified {cloudVerified}");
            }

            public static void GetTwinFromStoreWhenOffline(string id, long desiredVersion, long reportedVersion, Exception e)
            {
                Log.LogDebug((int)EventIds.GetTwinFromStoreWhenOffline, $"Getting twin for {id} at desired version " +
                    $"{desiredVersion} reported version {reportedVersion} from local store. Get from cloud threw {e.GetType()} {e.Message}");
            }

            public static void GotTwinFromCloudSuccess(string id, long desiredVersion, long reportedVersion)
            {
                Log.LogDebug((int)EventIds.GotTwinFromCloudSuccess, $"Successfully got twin for {id} from cloud at " +
                    $"desired version {desiredVersion} reported version {reportedVersion}");
            }

            public static void UpdateCachedTwin(string id, long cachedDesired, long cloudDesired, long cachedReported, long cloudReported)
            {
                Log.LogDebug((int)EventIds.UpdateCachedTwin, $"Updating cached twin for {id} from " +
                    $"desired version {cachedDesired} to {cloudDesired} and reported version {cachedReported} to " +
                    $"{cloudReported}");
            }

            public static void SendDesiredPropertyUpdateToSubscriber(string id, long oldDesiredVersion, long cloudDesiredVersion)
            {
                Log.LogDebug((int)EventIds.SendDesiredPropertyUpdateToSubscriber, $"Sending desired property update for {id}" +
                    $" old desired version {oldDesiredVersion} cloud desired version {cloudDesiredVersion}");
            }

            public static void PreserveCachedTwin(string id, long cachedDesired, long cloudDesired, long cachedReported, long cloudReported)
            {
                Log.LogDebug((int)EventIds.PreserveCachedTwin, $"Local twin for {id} at higher or equal desired version " +
                    $"{cachedDesired} compared to cloud {cloudDesired} or reported version {cachedReported} compared to cloud" +
                    $" {cloudReported}");
            }

            public static void ConnectionLost(string id)
            {
                Log.LogDebug((int)EventIds.ConnectionLost, $"ConnectionLost for {id}");
            }

            public static void ConnectionEstablished(string id)
            {
                Log.LogDebug((int)EventIds.ConnectionEstablished, $"ConnectionEstablished for {id}");
            }
        }
    }
}

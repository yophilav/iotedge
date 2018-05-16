// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Hub.CloudProxy.Test
{
    using System;
    using System.Text;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Edge.Hub.Core.Identity;
    using Microsoft.Azure.Devices.Edge.Storage;
    using Microsoft.Azure.Devices.Edge.Util.Test.Common;
    using Moq;
    using Xunit;

    [Unit]
    public class ClientProviderTest
    {
        const string IotHubHostName = "iothub.test";
        const string DeviceId = "device1";
        const string ModuleId = "module1";
        const string IotEdgedUriVariableName = "IOTEDGE_WORKLOADURI";
        const string IotHubHostnameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";

        readonly string authKey = Convert.ToBase64String("key".ToBytes());

        [Fact]
        public void Test_Create_DeviceIdentity_WithAuthMethod_ShouldCreateDeviceClient()
        {
            string token = TokenHelper.CreateSasToken(IotHubHostName);
            IIdentity identity = new DeviceIdentity(IotHubHostName, DeviceId);
            var authenticationMethod = new DeviceAuthenticationWithToken(DeviceId, token);

            var transportSettings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            IClient client = new ClientProvider().Create(identity, authenticationMethod, transportSettings);

            Assert.NotNull(client);
            Assert.True(client is DeviceClientWrapper);
        }

        [Fact]
        public void Test_Create_DeviceIdentity_WithConnectionString_ShouldCreateDeviceClient()
        {
            string connectionString = $"HostName={IotHubHostName};DeviceId=device1;SharedAccessKey={this.authKey}";
            IIdentity identity = new DeviceIdentity(IotHubHostName, DeviceId);

            var transportSettings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            IClient client = new ClientProvider().Create(identity, connectionString, transportSettings);

            Assert.NotNull(client);
            Assert.True(client is DeviceClientWrapper);
        }

        [Fact]
        public void Test_Create_DeviceIdentity_WithEnv_ShouldThrow()
        {
            IIdentity identity = new DeviceIdentity(IotHubHostName, DeviceId);

            var transportSettings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };

            Assert.Throws<InvalidOperationException>(() => new ClientProvider().Create(identity, transportSettings));
        }

        [Fact]
        public void Test_Create_ModuleIdentity_WithAuthMethod_ShouldCreateModuleClient()
        {
            string token = TokenHelper.CreateSasToken(IotHubHostName);
            IIdentity identity = new ModuleIdentity(IotHubHostName, DeviceId, ModuleId);
            var authenticationMethod = new ModuleAuthenticationWithToken(DeviceId, ModuleId, token);

            var transportSettings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            IClient client = new ClientProvider().Create(identity, authenticationMethod, transportSettings);

            Assert.NotNull(client);
            Assert.True(client is ModuleClientWrapper);
        }

        [Fact]
        public void Test_Create_ModuleIdentity_WithConnectionString_ShouldCreateModuleClient()
        {
            string connectionString = $"HostName={IotHubHostName};DeviceId={DeviceId};ModuleId={ModuleId};SharedAccessKey={this.authKey}";
            IIdentity identity = new ModuleIdentity(IotHubHostName, DeviceId, ModuleId);

            var transportSettings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };
            IClient client = new ClientProvider().Create(identity, connectionString, transportSettings);

            Assert.NotNull(client);
            Assert.True(client is ModuleClientWrapper);
        }

        [Fact]
        public void Test_Create_ModuleIdentity_WithEnv_ShouldCreateModuleClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, "localhost");
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            IIdentity identity = new ModuleIdentity(IotHubHostName, DeviceId, ModuleId);

            var transportSettings = new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) };

            IClient client = new ClientProvider().Create(identity, transportSettings);

            Assert.NotNull(client);
            Assert.True(client is ModuleClientWrapper);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }
    }
}

namespace SampleModule
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Edge.Test.Common;
    using Microsoft.Azure.Devices.Edge.Test.Common.Config;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Configuration;
    using IotHubConnectionStringBuilder = Microsoft.Azure.Devices.Client.IotHubConnectionStringBuilder;

    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config/settings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            //try
            //{
            //    //string deviceConnectionString = $"HostName={this.iotHubHostName};DeviceId={this.deviceId};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey};GatewayHostName={this.gatewayHostName}";
            //    // var connectionString = "";
            //    // var deviceClient = DeviceClient.CreateFromConnectionString(
            //    //     connectionString,
            //    //     "iotedge-seabear-Amqp-leaf",
            //    //     new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) });

            //    // IEnumerable<X509Certificate2> certs = await CertificateHelper.GetTrustBundleFromEdgelet(new Uri("unix:///var/run/iotedge/workload.sock"), "2019-11-05", "2019-01-30", "cloudToDeviceMessageReceiver1", configuration.GetValue<string>("IOTEDGE_MODULEGENERATIONID"));
            //    // ITransportSettings transportSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            //    // OsPlatform.Current.InstallCaCertificates(certs, transportSettings);

            //    // Make sure you're in azureiotedge network, not host (this is a default behavior).
            //    AmqpTransportSettings transportSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            //    transportSettings.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
            //    var connectionString = "";
            //    var deviceClient = DeviceClient.CreateFromConnectionString(
            //        connectionString,
            //        new ITransportSettings[] { transportSettings });
            //    var message = await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(30));
            //    Console.WriteLine($"{message}");
            //}
            //catch (Exception exception)
            //{
            //    throw exception;
            //}

            string iotHubConnectionString = "";
            string edgeDeviceId = "";

            Console.WriteLine("Getting or Creating device Identity.");
            var settings = new HttpTransportSettings();

            IotHubConnectionStringBuilder builder = IotHubConnectionStringBuilder.Create(iotHubConnectionString);
            RegistryManager rm = RegistryManager.CreateFromConnectionString(builder.ToString(), settings);

            bool forceNew = false;
            Device device = await rm.GetDeviceAsync(edgeDeviceId);

            if (device == null || (device != null && forceNew))
            {
                if (device != null && forceNew)
                {
                    await rm.RemoveDeviceAsync(device);
                }

                await CreateEdgeDeviceIdentity(rm, iotHubConnectionString, edgeDeviceId);
            }

            static async Task CreateEdgeDeviceIdentity(RegistryManager rm, string iotHubConnectionString, string deviceId)
            {
                var device = new Device(deviceId)
                {
                    Authentication = new AuthenticationMechanism() { Type = AuthenticationType.Sas },
                    Capabilities = new DeviceCapabilities() { IotEdge = true }
                };

                IotHubConnectionStringBuilder builder = IotHubConnectionStringBuilder.Create(iotHubConnectionString);
                Console.WriteLine($"Registering device '{device.Id}' on IoT hub '{builder.HostName}'");

                await rm.AddDeviceAsync(device);
            }
        }
    }
}

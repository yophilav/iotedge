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
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Edge.Test.Common;
    using Microsoft.Azure.Devices.Edge.Test.Common.Config;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Extensions.Configuration;

    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config/settings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            try
            {
                //string deviceConnectionString = $"HostName={this.iotHubHostName};DeviceId={this.deviceId};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey};GatewayHostName={this.gatewayHostName}";
                // var connectionString = "";
                // var deviceClient = DeviceClient.CreateFromConnectionString(
                //     connectionString,
                //     "iotedge-seabear-Amqp-leaf",
                //     new ITransportSettings[] { new AmqpTransportSettings(TransportType.Amqp_Tcp_Only) });

                // IEnumerable<X509Certificate2> certs = await CertificateHelper.GetTrustBundleFromEdgelet(new Uri("unix:///var/run/iotedge/workload.sock"), "2019-11-05", "2019-01-30", "cloudToDeviceMessageReceiver1", configuration.GetValue<string>("IOTEDGE_MODULEGENERATIONID"));
                // ITransportSettings transportSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
                // OsPlatform.Current.InstallCaCertificates(certs, transportSettings);

                // Make sure you're in azureiotedge network, not host (this is a default behavior).
                AmqpTransportSettings transportSettings = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
                transportSettings.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;
                var connectionString = "";
                var deviceClient = DeviceClient.CreateFromConnectionString(
                    connectionString,
                    new ITransportSettings[] { transportSettings });
                var message = await deviceClient.ReceiveAsync(TimeSpan.FromSeconds(30));
                Console.WriteLine($"{message}");
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }
    }
}

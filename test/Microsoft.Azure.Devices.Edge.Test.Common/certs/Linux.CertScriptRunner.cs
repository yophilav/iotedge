// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Test.Common.Certs.Linux
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Edge.Test.Common;
    using Microsoft.Azure.Devices.Edge.Test.Common.Certs;

    public class CertScriptRunner : ICertScriptRunner
    {
        string BuildCertCommand(string command, string scriptPath) =>
            $"-c \"FORCE_NO_PROD_WARNING=true '{Path.Combine(scriptPath, "certGen.sh")}' {command}\"";

        public async Task GenerateDeviceIdentityCertAsync(string deviceId, string scriptPath, CancellationToken token)
        {
            var command = BuildCertCommand($"create_device_certificate '{deviceId}'", scriptPath);
            await OsPlatform.RunScriptAsync(("bash", command), token);

            // Verify the cert and key were generated
            OsPlatform.NormalizeFiles(new string[]{
                FixedPaths.DeviceIdentityCert.Cert(deviceId), 
                FixedPaths.DeviceIdentityCert.Key(deviceId)
            }, scriptPath);
        }

        public async Task GenerateDeviceCaCertAsync(string deviceId, string scriptPath, CancellationToken token)
        {
            var command = BuildCertCommand($"create_edge_device_certificate '{deviceId}'", scriptPath);
            await OsPlatform.RunScriptAsync(("bash", command), token);

            OsPlatform.NormalizeFiles(new string[]{
                FixedPaths.DeviceCaCert.Cert(deviceId),
                FixedPaths.DeviceCaCert.Key(deviceId),
                FixedPaths.DeviceCaCert.TrustCert(deviceId)
            }, scriptPath);
        }

        public async Task InstallRootCertificateAsync(string certPath, string keyPath, string password, string scriptPath, CancellationToken token)
        {
            var command = BuildCertCommand($"install_root_ca_from_files '{certPath}' '{keyPath}' '{password}'", scriptPath);
            await OsPlatform.RunScriptAsync(("bash", command), token);

            OsPlatform.NormalizeFiles(new string[]{
                FixedPaths.RootCaCert.Cert,
                FixedPaths.RootCaCert.Key
            }, scriptPath);
        }
    }
}
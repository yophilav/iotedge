// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.Test.Common.Certs.Windows
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Edge.Test.Common;

    public class CertScriptRunner : ICertScriptRunner
    {
        static string BuildCertCommand(string command, string scriptPath)
        {
            var commands = new[]
            {
                $". {Path.Combine(scriptPath, "ca-certs.ps1")}",
                command
            };

            return string.Join(";", commands);
        }

        public async Task GenerateDeviceIdentityCertAsync(string deviceId, string scriptPath, CancellationToken token)
        {
            var command = BuildCertCommand($"New-CACertsDevice '{deviceId}'", scriptPath);
            await OsPlatform.RunScriptAsync(("powershell", command), token);

            OsPlatform.NormalizeFiles(new string[]{
                FixedPaths.DeviceIdentityCert.Cert(deviceId), 
                FixedPaths.DeviceIdentityCert.Key(deviceId)
            }, scriptPath);

            // BEARWASHERE -- Migrate this to 'Install once it's done
            // Windows requires all the certificates from root up to leaf to be installed.
            await OsPlatform.RunScriptAsync(("powershell", $"Import-Certificate -CertStoreLocation 'cert:\\LocalMachine\\Root' -FilePath " + Path.Combine(scriptPath, "certs", "azure-iot-test-only.root.ca.cert.pem") + " | Out-Host"), token);
            await OsPlatform.RunScriptAsync(("powershell", $"Import-Certificate -CertStoreLocation 'cert:\\LocalMachine\\Root' -FilePath " + Path.Combine(scriptPath, "certs", "azure-iot-test-only.intermediate.cert.pem") + " | Out-Host"), token);
        }

        public async Task GenerateDeviceCaCertAsync(string deviceId, string scriptPath, CancellationToken token)
        {
            string command = BuildCertCommand(
                $"New-CACertsEdgeDevice '{deviceId}'",
                scriptPath);
            await OsPlatform.RunScriptAsync(("powershell", command), token);

             OsPlatform.NormalizeFiles(new string[]{
                FixedPaths.DeviceCaCert.Cert(deviceId),
                FixedPaths.DeviceCaCert.Key(deviceId),
                FixedPaths.DeviceCaCert.TrustCert(deviceId)
            }, scriptPath);
        }

        public async Task InstallRootCertificateAsync(string certPath, string keyPath, string password, string scriptPath, CancellationToken token)
        {
            string command = BuildCertCommand(
                $"Install-RootCACertificate '{certPath}' '{keyPath}' 'rsa' {password}",
                scriptPath);
            await OsPlatform.RunScriptAsync(("powershell", command), token);

            OsPlatform.NormalizeFiles(new string[]{
                FixedPaths.RootCaCert.Cert,
                FixedPaths.RootCaCert.Key
            }, scriptPath);
        }
    }
}
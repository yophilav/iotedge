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

            // Verify the cert and key were generated
            string path = FixedPaths.DeviceIdentityCert.Cert(deviceId);
            if(!File.Exists(path))
            {
                throw new System.ArgumentException($"Fail to generate: {path}");
            }

            path = FixedPaths.DeviceIdentityCert.Key(deviceId);
            if(!File.Exists(path))
            {
                throw new System.ArgumentException($"Fail to generate: {path}");
            }

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

            string path = FixedPaths.DeviceCaCert.Cert(deviceId);
            if(!File.Exists(path))
            {
                throw new System.ArgumentException($"Fail to generate: {path}");
            }
            path = FixedPaths.DeviceCaCert.Key(deviceId);
            if(!File.Exists(path))
            {
                throw new System.ArgumentException($"Fail to generate: {path}");
            }
            path = FixedPaths.DeviceCaCert.TrustCert(deviceId);
            if(!File.Exists(path))
            {
                throw new System.ArgumentException($"File Not Found: {path}");
            }
        }
    }
}
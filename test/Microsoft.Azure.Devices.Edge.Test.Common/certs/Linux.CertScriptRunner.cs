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
        }

        public async Task GenerateDeviceCaCertAsync(string deviceId, string scriptPath, CancellationToken token)
        {
            var command = BuildCertCommand($"create_edge_device_certificate '{deviceId}'", scriptPath);
            await OsPlatform.RunScriptAsync(("bash", command), token);

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
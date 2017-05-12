﻿// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Hub.Core.Cloud
{
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Edge.Hub.Core.Device;

    class CloudListener : ICloudListener
    {
        readonly IDeviceProxy deviceProxy;

        public CloudListener(IDeviceProxy deviceProxy)
        {
            this.deviceProxy = deviceProxy;
        }

        public Task<object> CallMethodAsync(string methodName, object parameters, string deviceId)
        {
            return this.deviceProxy.CallMethodAsync(methodName, parameters);
        }

        public Task ProcessMessageAsync(IMessage message)
        {
            return this.deviceProxy.SendMessageAsync(message);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Hub.Amqp
{
    using Microsoft.Azure.Amqp;

    public static class Constants
    {
        public const string AmqpsScheme = "amqps";
        public const string AmqpScheme = "amqp";
        public const int AmqpsPort = 5671;
        public const string ServiceBusCbsSaslMechanismName = "MSSBCBS";
        public const uint DefaultAmqpConnectionIdleTimeoutInMilliSeconds = 4 * 60 * 1000;
        public const uint MinimumAmqpHeartbeatSendInterval = 5 * 1000;
        public const uint DefaultAmqpHeartbeatSendInterval = 2 * 60 * 1000;
        public const uint MinAmqpConnectionIdleTimeoutInMilliSeconds = DefaultAmqpConnectionIdleTimeoutInMilliSeconds;
        public const uint MaxAmqpConnectionIdleTimeoutInMilliSeconds = 25 * 60 * 1000;
        public static readonly AmqpVersion AmqpVersion100 = new AmqpVersion(1, 0, 0);

        // NOTE: IoT Hub service has this note on this constant:
        // Temporarily accept messages upto 1Mb in size. Reduce to 256 kb after fixing client behavior
        public const ulong AmqpMaxMessageSize = 256 * 1024 * 4;
    }
}

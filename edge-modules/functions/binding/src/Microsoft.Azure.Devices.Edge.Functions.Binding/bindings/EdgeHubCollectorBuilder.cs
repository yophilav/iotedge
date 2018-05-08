// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Functions.Binding.Bindings
{
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.WebJobs;

    class EdgeHubCollectorBuilder : IConverter<EdgeHubAttribute, IAsyncCollector<Message>>
    {
        readonly TransportType transportType;

        public EdgeHubCollectorBuilder(TransportType transportType)
        {
            this.transportType = transportType;
        }

        public IAsyncCollector<Message> Convert(EdgeHubAttribute attribute)
        {
            ModuleClient client = ModuleClientCache.Instance.GetOrCreate(this.transportType);
            return new EdgeHubAsyncCollector(client, attribute);
        }
    }
}

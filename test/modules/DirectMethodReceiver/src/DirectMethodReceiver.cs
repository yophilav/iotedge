// Copyright (c) Microsoft. All rights reserved.
namespace DirectMethodReceiver
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Edge.ModuleUtil;
    using Microsoft.Azure.Devices.Edge.ModuleUtil.TestResults;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    class DirectMethodReceiver : IDisposable
    {
        Guid batchId;
        IConfiguration configuration;
        long directMethodCount = 1;
        ILogger logger;
        ModuleClient moduleClient;
        Option<TestResultReportingClient> testResultReportingClient;

        public DirectMethodReceiver(
            ILogger logger,
            IConfiguration configuration)
        {
            Preconditions.CheckNotNull(logger, nameof(logger));
            Preconditions.CheckNotNull(configuration, nameof(configuration));

            TestResultReportingClient testResultReportingClient = null;
            Option<Uri> testReportCoordinatorUrl = Option.Maybe(configuration.GetValue<Uri>("testResultCoordinatorUrl"));
            testReportCoordinatorUrl.ForEach(
                (Uri uri) => testResultReportingClient = new TestResultReportingClient { BaseUrl = uri.AbsoluteUri });

            this.logger = logger;
            this.configuration = configuration;
            this.testResultReportingClient = Option.Maybe(testResultReportingClient);
            this.batchId = Guid.NewGuid();
        }

        public void Dispose() => this.moduleClient?.Dispose();

        async Task<MethodResponse> HelloWorldMethodAsync(MethodRequest methodRequest, object userContext)
        {
            this.logger.LogInformation("Received direct method call.");
            // Send the report to Test Result Coordinator
            await this.ReportTestResult();
            this.directMethodCount++;
            return new MethodResponse((int)HttpStatusCode.OK);
        }

        public async Task ReportTestResult()
        {
            DirectMethodTestResult testResult = null;
            await this.testResultReportingClient.ForEachAsync(
                async (TestResultReportingClient testResultReportingClient) =>
                    {
                        testResult = new DirectMethodTestResult(this.configuration.GetValue<string>("IOTEDGE_MODULEID") + ".receive", DateTime.UtcNow)
                        {
                            TrackingId = Option.Maybe(this.configuration.GetValue<string>("trackingId"))
                                .Expect(() => new ArgumentException("TrackingId is empty")),
                            BatchId = this.batchId.ToString(),
                            SequenceNumber = this.directMethodCount.ToString(),
                            Result = HttpStatusCode.OK.ToString()
                        };
                        await ModuleUtil.ReportTestResultAsync(testResultReportingClient, this.logger, testResult);
                    });
        }

        public async Task InitAsync()
        {
            this.moduleClient = await ModuleUtil.CreateModuleClientAsync(
                this.configuration.GetValue("ClientTransportType", TransportType.Amqp_Tcp_Only),
                ModuleUtil.DefaultTimeoutErrorDetectionStrategy,
                ModuleUtil.DefaultTransientRetryStrategy,
                this.logger);

            await this.moduleClient.OpenAsync();
            await this.moduleClient.SetMethodHandlerAsync("HelloWorldMethod", this.HelloWorldMethodAsync, null);
        }
    }
}
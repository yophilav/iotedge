// Copyright (c) Microsoft. All rights reserved.
namespace Microsoft.Azure.Devices.Edge.ModuleUtil.ReporterClients
{
    using System;
    using Microsoft.Azure.Devices.Edge.Util;

    public sealed class ReportContent
    {
        public string BatchId {get; private set;}        
        public string ResultMessage {get; private set;}
        public string SequenceNumber {get; private set;}
        public TestOperationResultType TestOperationResultType {get; private set;}
        public string TrackingId {get; private set;}


        public void SetBatchId(string batchId) => this.BatchId = batchId;
        public void SetResultMessage(string resultMessage) => this.ResultMessage = resultMessage;
        public void SetSequenceNumber(string sequenceNumber) => this.SequenceNumber = sequenceNumber;
        public void SetTestOperationResultType(TestOperationResultType testOperationResultType) => this.TestOperationResultType = testOperationResultType;
        public void SetTrackingId(string trackingId) => this.TrackingId = trackingId;

        public Object GenerateReport() => GenerateReport(this.TestOperationResultType);
        public Object GenerateReport(TestOperationResultType testOperationResultType)
        {
            Preconditions.CheckNotNull(testOperationResultType, nameof(testOperationResultType));
            
            // TODO: Add the formatting for other type of reports
            switch (testOperationResultType)
            {
                // Send to TestResultCoordinator endpoint
                case TestOperationResultType.DirectMethod:
                    return
                        new Microsoft.Azure.Devices.Edge.ModuleUtil.TestResultCoordinatorClient.TestOperationResult
                        {
                            CreatedAt = DateTime.UtcNow,
                            Result = $"{this.TrackingId};{this.BatchId};{this.SequenceNumber};{this.ResultMessage}",
                            Type = Enum.GetName(typeof(TestOperationResultType), testOperationResultType)
                        };

                // Send to TestAnalyzer endpoint
                case TestOperationResultType.LegacyDirectMethod:
                default:
                    return
                        new Microsoft.Azure.Devices.Edge.ModuleUtil.TestOperationResult
                        {
                            CreatedAt = DateTime.UtcNow,
                            Result = this.ResultMessage,
                            Type = Enum.GetName(typeof(TestOperationResultType), testOperationResultType)
                        };
            }
        }
    }
}
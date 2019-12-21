// Copyright (c) Microsoft. All rights reserved.
namespace DirectMethodSender
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Edge.ModuleUtil;
    using Microsoft.Azure.Devices.Edge.ModuleUtil.TestResultCoordinatorClient;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Extensions.Logging;
    using TestOperationResult = Microsoft.Azure.Devices.Edge.ModuleUtil.TestOperationResult;

    class Program
    {
        static readonly ILogger Logger = ModuleUtil.CreateLogger("DirectMethodSender");

        public static int Main() => MainAsync().Result;

        public static async Task<int> MainAsync()
        {
            Logger.LogInformation($"Starting DirectMethodSender with the following settings:\r\n{Settings.Current}");

            (CancellationTokenSource cts, ManualResetEventSlim completed, Option<object> handler) = ShutdownHandler.Init(TimeSpan.FromSeconds(5), Logger);
            DirectMethodSenderBase directMethodClient = null;
            ModuleClient reportClient = null;
            Option<Uri> analyzerUrl = Settings.Current.AnalyzerUrl;
            Option<Uri> testReportCoordinatorUrl = Settings.Current.TestResultCoordinatorUrl;

            try
            {
                Guid batchId = Guid.NewGuid();
                Logger.LogInformation($"Batch Id={batchId}");

                directMethodClient = await CreateClientAsync(Settings.Current.InvocationSource);

                reportClient = await ModuleUtil.CreateModuleClientAsync(
                    Settings.Current.TransportType,
                    ModuleUtil.DefaultTimeoutErrorDetectionStrategy,
                    ModuleUtil.DefaultTransientRetryStrategy,
                    Logger);

                Logger.LogInformation($"Load gen delay start for {Settings.Current.TestStartDelay}.");
                await Task.Delay(Settings.Current.TestStartDelay);

                DateTime testStartAt = DateTime.UtcNow;
                while (!cts.Token.IsCancellationRequested && IsTestTimeUp(testStartAt))
                {
                    (HttpStatusCode result, long dmCounter) = await directMethodClient.InvokeDirectMethodAsync(cts);

                    // TODO: Create an abstract class to handle the reporting client generation
                    await testReportCoordinatorUrl.ForEachAsync(
                        async (Uri uri) =>
                        {
                            TestResultCoordinatorClient trcClient = new TestResultCoordinatorClient { BaseUrl = uri.AbsoluteUri };
                            await ModuleUtil.ReportStatus(
                                    trcClient,
                                    Logger,
                                    Settings.Current.ModuleId + ".send",
                                    ModuleUtil.FormatDirectMethodTestResultValue(
                                        Settings.Current.TrackingId.Expect(() => new ArgumentException("TrackingId is empty")),
                                        batchId.ToString(),
                                        dmCounter.ToString(),
                                        result.ToString()),
                                    TestOperationResultType.DirectMethod.ToString());
                        });

                    await analyzerUrl.ForEachAsync(
                        async (Uri uri) =>
                        {
                            AnalyzerClient analyzerClient = new AnalyzerClient { BaseUrl = uri.AbsoluteUri };
                            await ReportStatus(Settings.Current.TargetModuleId, result, analyzerClient);
                        },
                        async () =>
                        {
                            await reportClient.SendEventAsync("AnyOutput", new Message(Encoding.UTF8.GetBytes("Direct Method call succeeded.")));
                        });

                    await Task.Delay(Settings.Current.DirectMethodDelay, cts.Token);
                }

                await cts.Token.WhenCanceled();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error occurred during direct method sender test setup");
            }
            finally
            {
                // Implicit CloseAsync()
                directMethodClient?.Dispose();
                reportClient?.Dispose();
            }

            completed.Set();
            handler.ForEach(h => GC.KeepAlive(h));
            Logger.LogInformation("DirectMethodSender Main() finished.");
            return 0;
        }

        public static async Task<DirectMethodSenderBase> CreateClientAsync(InvocationSource invocationSource)
        {
            DirectMethodSenderBase directMethodClient = null;
            switch (invocationSource)
            {
                case InvocationSource.Local:
                    // Implicit OpenAsync()
                    directMethodClient = await DirectMethodLocalSender.CreateAsync(
                            Settings.Current.TransportType,
                            ModuleUtil.DefaultTimeoutErrorDetectionStrategy,
                            ModuleUtil.DefaultTransientRetryStrategy,
                            Logger);
                    break;

                case InvocationSource.Cloud:
                    // Implicit OpenAsync()
                    directMethodClient = await DirectMethodCloudSender.CreateAsync(
                            Settings.Current.ServiceClientConnectionString.Expect(() => new ArgumentException("ServiceClientConnectionString is null")),
                            (Microsoft.Azure.Devices.TransportType)Settings.Current.TransportType,
                            Logger);
                    break;

                default:
                    throw new NotImplementedException("Invalid InvocationSource type");
            }

            return directMethodClient;
        }

        public static bool IsTestTimeUp(DateTime testStartAt)
        {
            return (Settings.Current.TestDuration == TimeSpan.Zero) || (DateTime.UtcNow - testStartAt < Settings.Current.TestDuration);
        }

        static async Task ReportStatus(string moduleId, HttpStatusCode result, AnalyzerClient analyzerClient)
        {
            try
            {
                await analyzerClient.ReportResultAsync(new TestOperationResult { Source = moduleId, Result = result.ToString(), CreatedAt = DateTime.UtcNow, Type = Enum.GetName(typeof(TestOperationResultType), TestOperationResultType.LegacyDirectMethod) });
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }
    }
}

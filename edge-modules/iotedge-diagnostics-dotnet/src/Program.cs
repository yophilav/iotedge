// Copyright (c) Microsoft. All rights reserved.
namespace Diagnostics
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Edge.Util;
    using Microsoft.Azure.Devices.Edge.Util.Concurrency;
    using Microsoft.Azure.Devices.Edge.Util.TransientFaultHandling;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using ProxyLib.Proxy;
    using ProxyLib.Proxy.Exceptions;

    class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                MainAsync(args).Wait();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            return 0;
        }

        static async Task MainAsync(string[] args)
        {
            var config = new ConfigurationBuilder().AddCommandLine(args).Build();
            switch (args[0])
            {
                case "edge-agent":
                    await EdgeAgent(config["management-uri"]);
                    break;
                case "iothub":
                    await Iothub(config["hostname"], config["port"], config["proxy"]);
                    break;
                case "local-time":
                    Console.WriteLine(DateTime.Now.ToUnixTimestamp());
                    break;
                default:
                    throw new Exception("Invalid args");
            }
        }

        static async Task EdgeAgent(string managementUri)
        {
            if (managementUri.EndsWith("sock"))
            {
                string response = GetSocket.GetSocketResponse(managementUri.TrimEnd('/'), "/modules/?api-version=2018-06-28");

                if (!response.StartsWith("HTTP/1.1 200 OK"))
                {
                    throw new Exception($"Got bad response: {response}");
                }
            }
            else
            {
                using (var http = new HttpClient())
                using (var response = await http.GetAsync(managementUri.TrimEnd('/') + "/modules/?api-version=2018-06-28"))
                {
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        static async Task Iothub(string hostname, string port, string proxy)
        {
            if (port == "443")
            {
                var httpClientHandler = new HttpClientHandler();

                if (proxy != null)
                {
                    Environment.SetEnvironmentVariable("https_proxy", proxy);
                }

                var httpClient = new HttpClient(httpClientHandler);
                var logsUrl = string.Format("https://{0}/devices/0000/modules", hostname);
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, logsUrl);
                HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);
            }
            else
            {
                // The current rust code never put proxy parameter when port is != than 443.
                // So the code below is never exercised. It was put there to avoid silently ignoring the proxy
                // if the rust code is changed.
                if (proxy != null)
                {
                    Uri proxyUri = new Uri(proxy);
                    IProxyClient proxyClient = MakeProxy(proxyUri);

                    // Setup timeouts
                    proxyClient.ReceiveTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;
                    proxyClient.SendTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;

                    // Get TcpClient to futher work
                    var client = proxyClient.CreateConnection(hostname, int.Parse(port));
                    client.GetStream();
                }
                else
                {
                    TcpClient client = new TcpClient();
                    await client.ConnectAsync(hostname, int.Parse(port));
                    client.GetStream();
                }
            }
        }

        static IProxyClient MakeProxy(Uri proxyUri)
        {
            // Uses https://github.com/grinay/ProxyLib
            ProxyClientFactory factory = new ProxyClientFactory();
            if (proxyUri.UserInfo == string.Empty)
            {
                return factory.CreateProxyClient(ProxyType.Http, proxyUri.Host, proxyUri.Port);
            }
            else
            {
                if (proxyUri.UserInfo.Contains(':'))
                {
                    var userPass = proxyUri.UserInfo.Split(':');
                    return factory.CreateProxyClient(ProxyType.Http, proxyUri.Host, proxyUri.Port, userPass[0], userPass[1]);
                }
                else
                {
                    throw new Exception($"Invalid user info: {proxyUri.UserInfo}");
                }
            }
        }
    }
}

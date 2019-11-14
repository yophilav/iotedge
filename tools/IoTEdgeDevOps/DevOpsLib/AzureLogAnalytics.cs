// Copyright (c) Microsoft. All rights reserved.
namespace DevOpsLib
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public sealed class AzureLogAnalytics
    {
        // static readonly ILogger Log = Logger.Factory.CreateLogger<AzureLogAnalytics>();
        static readonly AzureLogAnalytics instance = new AzureLogAnalytics();
        static readonly string apiVersion = "v1";
        static string accessToken = null;

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static AzureLogAnalytics()
        {
        }

        AzureLogAnalytics()
        {
        }

        public static AzureLogAnalytics Instance
        {
            get
            {
                return instance;
            }
        }

        // Trigger Azure Active Directory (AAD) for an OAuth2 client credential for an azure resource access.
        // API reference: https://dev.loganalytics.io/documentation/Authorization/OAuth2
        public async Task<string> GetAccessToken(
            string azureActiveDirTenant,
            string azureActiveDirClientId,
            string azureActiveDirClientSecret,
            string azureResource)
        {
            try
            {
                string requestUri = $"https://login.microsoftonline.com/{azureActiveDirTenant}/oauth2/token";
                const string grantType = "client_credentials";

                var client = new HttpClient();
                client.BaseAddress = new Uri(requestUri);
                //client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");

                var requestBody = new List<KeyValuePair<string, string>>();
                requestBody.Add(new KeyValuePair<string, string>("client_id", azureActiveDirClientId));
                requestBody.Add(new KeyValuePair<string, string>("client_secret", azureActiveDirClientSecret));
                requestBody.Add(new KeyValuePair<string, string>("grant_type", grantType));
                requestBody.Add(new KeyValuePair<string, string>("resource", azureResource));
                
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "");
                // By default, if FormUrlEncodedContent() is used, the "Content-Type" is set to "application/x-www-form-urlencoded"
                // request.Content.Headers = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Content = new FormUrlEncodedContent(requestBody);

                var response = await client.SendAsync(request).ConfigureAwait(false);
                var responseMsg = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return (string) JObject.Parse(responseMsg)["access_token"];
            }
            catch (Exception e)
            {
                // Log.LogError(e.Message);
                throw e;
            }
        }
    }
}

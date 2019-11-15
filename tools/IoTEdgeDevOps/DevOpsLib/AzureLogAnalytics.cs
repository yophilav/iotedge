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

        static bool isAzureActiveDirInited = false;
        static string azureActiveDirTenant = null;
        static string azureActiveDirClientId = null;
        static string azureActiveDirClientSecret = null;
        static string azureResource = null;
        static string accessToken = null;
        static DateTime accessTokenExpiration = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);

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
                if ((DateTime.Compare(DateTime.UtcNow, AzureLogAnalytics.accessTokenExpiration) >= 0) )
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

                    AzureLogAnalytics.azureActiveDirTenant = azureActiveDirTenant;
                    AzureLogAnalytics.azureActiveDirClientId = azureActiveDirClientId;
                    AzureLogAnalytics.azureActiveDirClientSecret = azureActiveDirClientSecret;
                    AzureLogAnalytics.azureResource = azureResource;
                    AzureLogAnalytics.isAzureActiveDirInited = true;

                    var responseJson = JObject.Parse(responseMsg);
                    AzureLogAnalytics.accessTokenExpiration = DateTime.UtcNow.AddSeconds((double)responseJson["expires_on"]);
                    AzureLogAnalytics.accessToken = (string)responseJson["access_token"];
                }

                return AzureLogAnalytics.accessToken;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        // Use get-request to do a Kusto query
        // API reference: https://dev.loganalytics.io/documentation/Using-the-API/RequestFormat
        public async Task<string> GetKqlQuery(
            string logAnalyticsWorkspaceId,
            string kqlQuery)
        {
            try
            {
                if ((DateTime.Compare(DateTime.UtcNow, AzureLogAnalytics.accessTokenExpiration) >= 0) ||
                   (AzureLogAnalytics.accessToken == null))
                {
                    if (AzureLogAnalytics.isAzureActiveDirInited)
                    {
                        await AzureLogAnalytics.Instance.GetAccessToken(
                            AzureLogAnalytics.azureActiveDirTenant,
                            AzureLogAnalytics.azureActiveDirClientId,
                            AzureLogAnalytics.azureActiveDirClientSecret,
                            AzureLogAnalytics.azureResource);
                    }
                    else
                    {
                        throw new ArgumentException("The access token is expired without Azure Active Directory credentials to renew. Please invoke GetAccessToken(4)", AzureLogAnalytics.accessToken);
                    }
                }

                string requestUri = $"https://api.loganalytics.io/{AzureLogAnalytics.apiVersion}/workspaces/{logAnalyticsWorkspaceId}/query?query=";

                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AzureLogAnalytics.accessToken}");

                var response = await client.GetAsync(requestUri + kqlQuery).ConfigureAwait(false);
                var responseMsg = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return responseMsg;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}

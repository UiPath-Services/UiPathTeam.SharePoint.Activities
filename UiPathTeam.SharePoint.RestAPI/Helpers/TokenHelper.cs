using Microsoft.Identity.Client;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Service;
using UiPathTeam.SharePoint.Service.Helpers;
using static System.Formats.Asn1.AsnWriter;
using static UiPathTeam.SharePoint.RestAPI.Helpers.TokenHelper.AcsMetadataParser;

namespace UiPathTeam.SharePoint.RestAPI.Helpers
{
    //public enum SharePointLoginMode
    //{
    //    Online,      // SPO using basic username/password (via token)
    //    OnPremises,  // On-premises using NetworkCredential (Windows Integrated Authentication)
    //    AppOnly,     // App-only using ClientId/ClientSecret
    //    WebLogin,    // Interactive login (MFA popup, etc.)
    //    AzureApp     // Azure App login (impersonation via Azure AD)
    //}

    public static class TokenHelper
    {
        /// <summary>
        /// Acquires an access token based on the provided login mode and parameters.
        /// </summary>
        /// <param name="loginMode">The login mode to use.</param>
        /// <param name="sharePointURL">The resource URL (e.g. https://yourtenant.sharepoint.com).</param>
        /// <param name="clientId">The Application (client) ID registered in Azure AD.</param>
        /// <param name="authority">The authority URL (e.g. https://login.microsoftonline.com/yourtenant.onmicrosoft.com).</param>
        /// <param name="scopes">A collection of scopes. For SharePoint, typically something like "resource/.default".</param>
        /// <param name="clientSecret">The client secret (required for confidential client flows).</param>
        /// <param name="username">The username for online mode.</param>
        /// <param name="password">The password for online mode.</param>
        /// <param name="azureAppID">Azure App ID (used for AppOnly authentication).</param>
        /// <param name="azureAppPermissions">Azure App Permissions (AzureApp mode).</param>
        /// <returns>An access token string.</returns>
        public static async Task<string> GetAccessTokenAsync(
            SharePointLoginMode loginMode,
            string sharePointURL,
            string clientId,
            string authority,
            IEnumerable<string> scopes,
            string clientSecret = null,
            string username = null,
            string password = null,
            string azureAppID = null,
            string[] azureAppPermissions = null)
        {
            switch (loginMode)
            {
                case SharePointLoginMode.Online:
                    return await GetOnlineTokenAsync(sharePointURL,clientId, authority, scopes, username, password);
                    //return await GetOnlineToken2Async(clientId, authority, scopes, username, password);
                case SharePointLoginMode.OnPremises:
                    return await GetOnPremisesTokenAsync(clientId, authority, sharePointURL, username);
                case SharePointLoginMode.AppOnly:
                    return await TokenHelper.GetAppOnlyAccessTokenAsync(sharePointURL, clientId, clientSecret);
                    //return await GetAppOnlyTokenAsync(clientId, authority, scopes, clientSecret);
                case SharePointLoginMode.WebLogin:
                    return await GetTokenInteractiveAsync(clientId, authority, scopes);
                case SharePointLoginMode.AzureApp:
                    return await GetAzureAppTokenAsync(sharePointURL, azureAppID, username, password, azureAppPermissions);
                    //return await GetAzureAppTokenAsync(clientId, authority, scopes, clientSecret, userAssertionToken);
                //case SharePointLoginMode.OnPremAppOnly:
                //    return await GetOnPremAppOnlyTokenAsync(clientId, clientSecret, sharePointURL);
                default:
                    throw new ArgumentException("Invalid login mode");
            }
        }

        // Online mode: Acquire token using username/password (ROPc flow).
        private static async Task<string> GetOnlineTokenAsync(
            string siteUrl,
            string clientId,
            string authority,
            IEnumerable<string> scopes,
            string username,
            string password)
        {

            var siteUri = new Uri(siteUrl);
            if (string.IsNullOrEmpty(authority))
            {
                try
                {
                    // Get the tenant ID from the SharePoint site
                    var tenantId = GetRealmFromTargetUrl(siteUri);
                    authority = $"https://login.microsoftonline.com/{tenantId}";
                }
                catch (Exception)
                {
                    string tenantName = siteUri.Host.Split('.')[0]; // Extracts "tenant name" from the host
                    authority = $"https://login.microsoftonline.com/{tenantName}.onmicrosoft.com"; // 95% of the time, this is the authority
                }
                
            }
            

            if (string.IsNullOrEmpty(clientId))
                clientId = "9bc3ab49-b65d-410a-85ad-de819febfddc"; // SharePoint Client Extensibility ID

            if (scopes == null || scopes.Count() == 0)
            {
                scopes = new[] { $"{siteUri.Scheme}://{siteUri.Host}/.default" };
            }
            var app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .Build();

            try
            {
                // Secure the password
                var securePassword = new SecureString();
                foreach (char c in password)
                {
                    securePassword.AppendChar(c);
                }

                var result = await app.AcquireTokenByUsernamePassword(scopes, username, securePassword)
                    .ExecuteAsync();
                return result.AccessToken;
            }
            //catch (MsalUiRequiredException ex)
            //{
            //    var interactiveResult = await app.AcquireTokenInteractive(scopes)
            //    .WithPrompt(Prompt.SelectAccount)
            //    .ExecuteAsync();

            //    return interactiveResult.AccessToken;
            //    //throw new Exception("Interactive login is required or the account uses MFA. ROPC flow cannot be used.", ex);
            //}
            catch (MsalException ex)
            {
                Console.WriteLine($"Error acquiring token: {ex.Message}");
                throw;
            }
        }

        //public static async Task<string> GetTenantIdFromSharePointSiteAsync(string siteUrl)
        //{
        //    using (var httpClient = new HttpClient(new HttpClientHandler
        //    {
        //        AllowAutoRedirect = false // Critical to prevent redirects from stripping headers
        //    }))
        //    {
        //        string apiUrl = $"{siteUrl.TrimEnd('/')}/_api/web";

        //        // Add User Agent to match original implementation
        //        httpClient.DefaultRequestHeaders.Add("User-Agent", "NONISV|UiPath|SharePointActivities/1.5.3");
        //        HttpResponseMessage response;
        //        try
        //        {
        //            response = await httpClient.GetAsync(apiUrl);
        //        }
        //        catch (HttpRequestException ex)
        //        {
        //            throw new InvalidOperationException(
        //                $"Network error: {ex.Message}. Check site URL and connectivity.", ex);
        //        }
        //        // SharePoint will return 401 Unauthorized with tenant hint
        //        // Handle unexpected status codes
        //        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
        //        {
        //            throw new InvalidOperationException(
        //                $"Expected 401 Unauthorized from {apiUrl}. Got {response.StatusCode} instead. " +
        //                "Possible causes:\n" +
        //                "1. Site requires authentication for all access (modern SharePoint behavior)\n" +
        //                "2. URL points to a resource that doesn't exist\n" +
        //                "3. Tenant has strict anonymous access policies");
        //        }

        //        var authenticateHeaders = response.Headers.WwwAuthenticate.ToString();

        //        if (string.IsNullOrEmpty(authenticateHeaders))
        //        {
        //            throw new InvalidOperationException(
        //                "No WWW-Authenticate header found in response");
        //        }

        //        // Extract the tenant ID from the Bearer challenge
        //        var bearerRealm = authenticateHeaders
        //            .Split(',')
        //            .FirstOrDefault(p => p.Trim().StartsWith("Bearer realm="))?
        //            .Split('=')[1]
        //            .Trim('"', ' ');

        //        if (string.IsNullOrEmpty(bearerRealm))
        //        {
        //            throw new InvalidOperationException(
        //                "Bearer realm not found in WWW-Authenticate header");
        //        }

        //        return bearerRealm;
        //    }
        //}
        // Online mode: Acquire token using username/password (ROPc flow).
        private static async Task<string> GetOnlineToken2Async(
            string clientId,
            string authority,
            IEnumerable<string> scopes,
            string username,
            string password)
        {
            var app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .Build();

            try
            {
                // Secure the password
                var securePassword = new SecureString();
                foreach (char c in password)
                {
                    securePassword.AppendChar(c);
                }

                var result = await app.AcquireTokenByUsernamePassword(scopes, username, securePassword)
                    .ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalUiRequiredException ex)
            {
                var interactiveResult = await app.AcquireTokenInteractive(scopes)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync();

                return interactiveResult.AccessToken;
                //throw new Exception("Interactive login is required or the account uses MFA. ROPC flow cannot be used.", ex);
            }
            catch (MsalException ex)
            {
                throw new Exception("Error acquiring token via Online mode (username/password).", ex);
            }
        }

        // OnPremises mode: Acquire token using Windows Integrated Authentication.
        private static async Task<string> GetOnPremisesTokenAsync(string clientId, string authority, string sharePointURL, string username)
        {
            // Note: Windows Integrated Authentication requires the current machine to be domain joined.
            // Adjust the clientId and authority values as per your configuration.
            //var clientId = "your-onprem-client-id"; // Replace with your actual client ID for on-premises scenarios.
            //var authority = "https://login.microsoftonline.com/yourtenant.onmicrosoft.com"; // Adjust as needed.
            var scopes = new List<string> { $"{sharePointURL}/.default" };

            var app = PublicClientApplicationBuilder.Create(clientId)
                        .WithAuthority(authority)
                        .Build();

            try
            {
                var result = await app.AcquireTokenByIntegratedWindowsAuth(scopes)
                                      .WithUsername(username)
                                      .ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalException ex)
            {
                throw new Exception("Error acquiring token via OnPremises mode (Integrated Windows Authentication).", ex);
            }
        }

        // AppOnly mode: Acquire token using client credentials flow.
        private static async Task<string> GetAppOnlyTokenAsync(
            string clientId,
            string authority,
            IEnumerable<string> scopes,
            string clientSecret)
        {
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(authority)
                .Build();

            try
            {
                var result = await app.AcquireTokenForClient(scopes)
                    .ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalException ex)
            {
                throw new Exception("Error acquiring token via AppOnly mode.", ex);
            }
        }

        

        // WebLogin mode: Acquire token using interactive authentication.
        private static async Task<string> GetTokenInteractiveAsync(
            string clientId,
            string authority,
            IEnumerable<string> scopes)
        {
            var app = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .WithRedirectUri("http://localhost") // Adjust redirect URI if necessary.
                .Build();
            //var app = PublicClientApplicationBuilder
            //    .Create(clientId)
            //    .WithTenantId("94b1e4e4-f92b-43b8-ba7b-197e1f6a0f5a")
            //    .WithRedirectUri("http://localhost")
            //    .Build();

            try
            {
                var result = await app.AcquireTokenInteractive(scopes)
                    .ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalException ex)
            {
                throw new Exception("Error acquiring token via WebLogin mode.", ex);
            }
        }
#if WINDOWS
        public static HttpClient GetWebLoginHttpClient(string siteUrl, Icon icon = null, bool scriptErrorsSuppressed = true)
        {
            CookieContainer authCookiesContainer = new CookieContainer();
            Uri siteUri = new Uri(siteUrl);
            Thread thread = new Thread((ThreadStart)delegate
            {
                System.Windows.Forms.Form form = new System.Windows.Forms.Form();
                if (icon != null)
                {
                    form.Icon = icon;
                }

                WebBrowser webBrowser = new WebBrowser
                {
                    ScriptErrorsSuppressed = scriptErrorsSuppressed,
                    Dock = DockStyle.Fill
                };
                form.SuspendLayout();
                form.Width = 900;
                form.Height = 500;
                form.Text = "Log in to " + siteUrl;
                form.Controls.Add(webBrowser);
                form.ResumeLayout(performLayout: false);
                webBrowser.Navigate(siteUri);
                webBrowser.Navigated += delegate (object sender, WebBrowserNavigatedEventArgs args)
                {
                    if (siteUri.Host.Equals(args.Url.Host))
                    {
                        string text = CookieReader.GetCookie(siteUrl).Replace("; ", ",").Replace(";", ",");
                        IEnumerable<string> enumerable = null;
                        if (Regex.IsMatch(text, "FedAuth", RegexOptions.IgnoreCase))
                        {
                            enumerable = from c in text.Split(',')
                                         where c.StartsWith("FedAuth", StringComparison.InvariantCultureIgnoreCase) || c.StartsWith("rtFa", StringComparison.InvariantCultureIgnoreCase)
                                         select c;
                        }
                        else if (Regex.IsMatch(text, "EdgeAccessCookie", RegexOptions.IgnoreCase))
                        {
                            enumerable = from c in text.Split(',')
                                         where c.StartsWith("EdgeAccessCookie", StringComparison.InvariantCultureIgnoreCase)
                                         select c;
                        }

                        if (enumerable != null)
                        {
                            authCookiesContainer.SetCookies(siteUri, string.Join(",", enumerable));
                            form.Close();
                        }
                    }
                };
                form.Focus();
                form.ShowDialog();
                webBrowser.Dispose();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (authCookiesContainer.Count > 0)
            {
                HttpClientHandler handler = new HttpClientHandler
                {
                    CookieContainer = authCookiesContainer,
                    UseCookies = true
                };
                HttpClient client = new HttpClient(handler)
                {
                    BaseAddress = siteUri
                };
                return client;
            }
            return null;
        }
        public static class CookieReader
        {
            [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern bool InternetGetCookieEx(
                string url, string cookieName, StringBuilder cookieData,
                ref uint cookieDataSize, int flags, IntPtr reserved);

            public static string GetCookie(string url)
            {
                uint size = 0;
                InternetGetCookieEx(url, null, null, ref size, 0x00002000, IntPtr.Zero);
                StringBuilder cookieData = new StringBuilder((int)size);
                if (InternetGetCookieEx(url, null, cookieData, ref size, 0x00002000, IntPtr.Zero))
                {
                    return cookieData.ToString();
                }
                return string.Empty;
            }
        }
#endif
        // AzureApp mode: Acquire token using the on-behalf-of flow.
        private static async Task<string> GetAzureAppToken2Async(
            string clientId,
            string authority,
            IEnumerable<string> scopes,
            string clientSecret,
            string userAssertionToken)
        {
            if (string.IsNullOrEmpty(userAssertionToken))
                throw new ArgumentNullException(nameof(userAssertionToken), "User assertion token is required for AzureApp mode.");

            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(authority)
                .Build();

            try
            {
                // The on-behalf-of flow exchanges the provided user token for a new access token.
                var userAssertion = new UserAssertion(userAssertionToken);
                var result = await app.AcquireTokenOnBehalfOf(scopes, userAssertion)
                    .ExecuteAsync();
                return result.AccessToken;
            }
            catch (MsalException ex)
            {
                throw new Exception("Error acquiring token via AzureApp mode (On-Behalf-Of flow).", ex);
            }
        }
        public static async Task<string> GetAzureAppTokenAsync(
            string Url, string AzureAppId, string UserName, string Password, string[] AzureAppPermissions)
        {

            string spoTenant = ExtractTenantFromSiteURL(Url);

            IPublicClientApplication publicClientApp = PublicClientApplicationBuilder.Create(AzureAppId)
                .WithRedirectUri("http://localhost")
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();
           

            try
            {

                AuthenticationResult authResult = await publicClientApp.AcquireTokenByUsernamePassword(
                    AzureAppPermissions, UserName, new System.Net.NetworkCredential("", Password).SecurePassword)
                    .ExecuteAsync();
                
                return authResult.AccessToken;
            }
            catch (MsalUiRequiredException ex)
            {
                if(ex.Message.ToLower().Contains("consent"))
                {
                    return await GetClientConsent(Url, AzureAppId, AzureAppPermissions);
                }

                throw ex;
            }
            catch (MsalException ex)
            {
                throw new Exception("Error acquiring token via AzureApp mode. Exception: " + ex.Message, ex);
            }
        }

        public static async Task<string> GetClientConsent(
            string Url, string AzureAppId, string[] AzureAppPermissions)
        {

            //string spoTenant = ExtractTenantFromSiteURL(Url);

            var publicClientApp = PublicClientApplicationBuilder.Create(AzureAppId)
                .WithRedirectUri("http://localhost")
                .WithAuthority(AadAuthorityAudience.AzureAdMultipleOrgs)
                .Build();

            try
            {

                var authResult = await publicClientApp
                           .AcquireTokenInteractive(AzureAppPermissions)
                           .ExecuteAsync();

                return authResult.AccessToken;
            }
            catch (MsalException ex)
            {
                throw new Exception("Error acquiring token via Azure APp - AcquireTokenInteractive.", ex);
            }
        }

        public static string ExtractTenantFromSiteURL(string siteURL)
        {
            var spoTenant = siteURL.Substring(0, siteURL.IndexOf('/', 8));
            return spoTenant;
        }
        public static string GetRealmFromTargetUrl(Uri targetApplicationUri)
        {
            WebRequest webRequest = WebRequest.Create(targetApplicationUri.ToString().TrimEnd('/') + "/_vti_bin/client.svc");
            webRequest.Headers.Add("Authorization: Bearer ");
            try
            {
                using (webRequest.GetResponse())
                {
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    return null;
                }

                string text = ex.Response.Headers["WWW-Authenticate"];
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }

                int num = text.IndexOf("Bearer realm=\"", StringComparison.Ordinal);
                if (num < 0)
                {
                    return null;
                }

                int num2 = num + "Bearer realm=\"".Length;
                if (text.Length >= num2 + 36)
                {
                    string text2 = text.Substring(num2, 36);
                    if (Guid.TryParse(text2, out var _))
                    {
                        return text2;
                    }
                }
            }

            return null;
        }

        private static async Task<string> GetOnPremAppOnlyTokenAsync(
    string clientId,
    string clientSecret,
    string sharePointSiteUrl)
        {
            try
            {
                // Get the realm from the SharePoint site
                var realm = GetRealmFromTargetUrl(new Uri(sharePointSiteUrl));
                if (string.IsNullOrEmpty(realm))
                    throw new Exception("Could not retrieve realm from SharePoint site.");

                string formattedClientId = $"{clientId}@{realm}";
                string resource = formattedClientId;

                // Construct the token endpoint URL
                string tokenEndpoint = $"{sharePointSiteUrl.TrimEnd('/')}/_vti_bin/oauth2.svc/token";

                using (var httpClient = new HttpClient())
                {
                    // Prepare the request content
                    var requestContent = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", formattedClientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("resource", resource)
            });

                    // Send the token request
                    HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, requestContent);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Token request failed. Status: {response.StatusCode}, Details: {responseContent}");

                    // Parse the access token from the response
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);
                    
                    return jsonResponse.access_token;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error acquiring on-premises app-only token. Ensure the client ID, secret, and site URL are correct.", ex);
            }
        }

        public static async Task<string> GetAppOnlyAccessTokenAsync(string siteUrl, string appId, string appSecret, string acsHostUrl = "accesscontrol.windows.net", string globalEndPointPrefix = "accounts")
        {
            var realm = GetRealmFromTargetUrl(new Uri(siteUrl));
            string SHAREPOINT_PRINCIPAL = "00000003-0000-0ff1-ce00-000000000000";
            string targetHost = new Uri(siteUrl).Authority;

            string formattedPrincipal = GetFormattedPrincipal(SHAREPOINT_PRINCIPAL, targetHost, realm);
            string clientid_formatted = GetFormattedPrincipal(appId, "", realm);
            //string clientid_formatted = GetFormattedPrincipal(appId, HostedAppHostName, realm);

            string scope = formattedPrincipal;

            string token = await IssueAsync(GetStsUrl(realm, acsHostUrl, globalEndPointPrefix), clientid_formatted, appSecret, scope, formattedPrincipal);

            return token;

        }
        public static string GetStsUrl(string realm, string acsHostUrl, string globalEndPointPrefix)
        {
            JsonEndpoint jsonEndpoint = GetMetadataDocument(realm, acsHostUrl, globalEndPointPrefix).endpoints.SingleOrDefault((JsonEndpoint e) => e.protocol == "OAuth2");
            if (jsonEndpoint != null)
            {
                return jsonEndpoint.location;
            }

            throw new Exception("Metadata document does not contain STS endpoint URL");
        }
        private static JsonMetadataDocument GetMetadataDocument(string realm, string acsHostUrl, string globalEndPointPrefix)
        {
            string text = string.Format(CultureInfo.InvariantCulture, "{0}?realm={1}", GetAcsMetadataEndpointUrl(acsHostUrl, globalEndPointPrefix), realm);
            byte[] bytes;
            using (WebClient webClient = new WebClient())
            {
                bytes = webClient.DownloadData(text);
            }

            string @string = Encoding.UTF8.GetString(bytes);
            //JsonMetadataDocument jsonMetadataDocument = new JavaScriptSerializer().Deserialize<JsonMetadataDocument>(@string);
            JsonMetadataDocument jsonMetadataDocument = JsonSerializer.Deserialize<JsonMetadataDocument>(@string);
            if (jsonMetadataDocument == null)
            {
                throw new Exception("No metadata document found at the global endpoint " + text);
            }

            return jsonMetadataDocument;
        }
        private static string GetAcsMetadataEndpointUrl(string acsHostUrl, string globalEndPointPrefix)
        {
            return Path.Combine(GetAcsGlobalEndpointUrl(acsHostUrl, globalEndPointPrefix), "metadata/json/1");
        }
        private static string GetAcsGlobalEndpointUrl(string acsHostUrl, string globalEndPointPrefix)
        {
            if (globalEndPointPrefix.Length == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "https://{0}/", acsHostUrl);
            }

            return string.Format(CultureInfo.InvariantCulture, "https://{0}.{1}/", globalEndPointPrefix, acsHostUrl);
        }
        public static async Task<string> IssueAsync(string securityTokenServiceUrl, string clientId, string clientSecret, string scope, string resource)
        {
            using var httpClient = new HttpClient();
            // Construct the request body as a URL-encoded form
            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "scope", scope },
                { "resource", resource }
            };
            //var content = new FormUrlEncodedContent(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, securityTokenServiceUrl)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            HttpResponseMessage response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // Safely extract access token using JSON Path
            var json = JObject.Parse(responseBody);
            if (json["access_token"]?.Value<string>() is string accessToken)
            {
                return accessToken;
            }

            throw new InvalidOperationException("Access token not found in response");

        }
        private static string GetFormattedPrincipal(string principalName, string hostName, string realm)
        {
            if (!String.IsNullOrEmpty(hostName))
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}/{1}@{2}", principalName, hostName, realm);
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}@{1}", principalName, realm);
        }

        public static class AcsMetadataParser
        {
            public class JsonMetadataDocument
            {
                public string serviceName { get; set; }

                public List<JsonEndpoint> endpoints { get; set; }

                public List<JsonKey> keys { get; set; }
            }

            public class JsonEndpoint
            {
                public string location { get; set; }

                public string protocol { get; set; }

                public string usage { get; set; }
            }
            public class JsonKeyValue
            {
                public string type { get; set; }

                public string value { get; set; }
            }

            public class JsonKey
            {
                public string usage { get; set; }

                public JsonKeyValue keyValue { get; set; }
            }
        }
    }
}

using Microsoft.Identity.Client;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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
                    //return await GetOnlineTokenAsync(sharePointURL,clientId, authority, scopes, username, password);
                    return await GetOnlineTokenV2Async(sharePointURL,clientId, authority, scopes, username, password);
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

        #region Shared HttpClient for MSAL
        // (UPDATED) Faster connect timeout to avoid lingering on bad IPv6/PAC paths
        private static readonly SocketsHttpHandler SystemProxyHandler = new()
        {
            // Keep: respect customer environment (WPAD/PAC or explicit proxy)
            UseProxy = true,
            Proxy = WebRequest.DefaultWebProxy,

            // (UPDATED) Quick connect; allows fast fallback to v4/other path
            ConnectTimeout = TimeSpan.FromSeconds(8),

            // Reasonable pool hygiene
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),

            // Throughput
            MaxConnectionsPerServer = 64,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

            // Be strict but normal TLS
            SslOptions = new SslClientAuthenticationOptions
            {
                // leave defaults; customers may use SSL inspection
            }
        };
        // (UPDATED) Explicit No-Proxy handler for automatic fallback
        private static readonly SocketsHttpHandler NoProxyHandler = new()
        {
            UseProxy = false,
            Proxy = null,

            ConnectTimeout = TimeSpan.FromSeconds(3),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 64,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        // Factory that lets us inject a specific handler per PCA
        private sealed class MsalHttpClientFactory : IMsalHttpClientFactory
        {
            private readonly HttpClient _client;

            public MsalHttpClientFactory(HttpMessageHandler handler)
            {
                _client = new HttpClient(handler, disposeHandler: false)
                {
                    // (UPDATED) Give HttpClient enough time; we control attempt budget via CTS
                    Timeout = Timeout.InfiniteTimeSpan
                };
            }

            public HttpClient GetHttpClient() => _client;
        }
        public static async Task<string> GetOnlineTokenV2Async(
        string siteUrl,
        string clientId,
        string authority,
        IEnumerable<string> scopes,
        string username,
        string password,
        TimeSpan? overallTimeout = null)
        {
            if (string.IsNullOrWhiteSpace(siteUrl))
                throw new ArgumentException("siteUrl is required", nameof(siteUrl));
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("username is required", nameof(username));
            if (password is null)
                throw new ArgumentNullException(nameof(password));

            var siteUri = new Uri(siteUrl);

            // (UPDATED) Authority resolution with its own short timeout
            if (string.IsNullOrWhiteSpace(authority))
            {
                authority = await ResolveAuthorityWithFallbackAsync(siteUri).ConfigureAwait(false);
            }

            // ------------------------------------------------------------
            // Client ID (keep SharePoint public client if caller didn't provide)
            // ------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(clientId))
                clientId = "9bc3ab49-b65d-410a-85ad-de819febfddc"; // SharePoint public client

            // ------------------------------------------------------------
            // Scopes default (resource/.default for SPO)
            // ------------------------------------------------------------
            if (scopes == null || !scopes.Any())
                scopes = new[] { $"{siteUri.Scheme}://{siteUri.Host}/.default" };

            // ------------------------------------------------------------
            // SecureString (MSAL ROPC API accepts it)
            // ------------------------------------------------------------
            var secure = new SecureString();
            foreach (var c in password) secure.AppendChar(c);

            TimeSpan timeout = overallTimeout ?? TimeSpan.FromSeconds(60);
            
            if (Debugger.IsAttached)
            {
                timeout = TimeSpan.FromSeconds(360); // longer when debugging interactively
            }
            var overallCts = new CancellationTokenSource(timeout);
            Debug.WriteLine("############:TimeoutStart:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));


            try
            {
                // 1) Try with system proxy first (respects customer env)
                var token = await TryAcquireAcrossAttemptsAsync(
                    siteUri, clientId, authority, scopes, username, secure,
                    useSystemProxy: true,
                    overallCts.Token).ConfigureAwait(false);
                
                if (token is not null)
                    return token;

                // 2) (UPDATED) Automatic fallback to direct (no proxy)
                token = await TryAcquireAcrossAttemptsAsync(
                    siteUri, clientId, authority, scopes, username, secure,
                    useSystemProxy: false,
                    overallCts.Token).ConfigureAwait(false);

                if (token is not null)
                    return token;

                throw new TimeoutException("Token acquisition timed out after trying both proxy and direct paths. Likely DNS/TCP/TLS/proxy connectivity issue.");
            }
           
            finally
            {
                Debug.WriteLine("############:TimeouEnd:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                overallCts.Dispose();
                secure.Dispose();
            }

        }
        // (UPDATED) Realm discovery guarded by its own short timeout (3s)
        private static async Task<string> ResolveAuthorityWithFallbackAsync(Uri siteUri)
        {
            using var discoveryCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                var tenantId = await Task.Run(() => GetRealmFromTargetUrl(siteUri), discoveryCts.Token)
                                         .ConfigureAwait(false);
                return $"https://login.microsoftonline.com/{tenantId}";
            }
            catch
            {
                // Heuristic fallback (works in many tenants)
                var tenantName = siteUri.Host.Split('.')[0];
                return $"https://login.microsoftonline.com/{tenantName}.onmicrosoft.com";
            }
        }
        // Attempts up to 3 times on the chosen path (proxy or direct)
        private static async Task<string?> TryAcquireAcrossAttemptsAsync(
            Uri siteUri,
            string clientId,
            string authority,
            IEnumerable<string> scopes,
            string username,
            SecureString securePassword,
            bool useSystemProxy,
            CancellationToken overallToken)
        {
            // (UPDATED) Build a PCA bound to a specific HttpClient configuration
            var handler = useSystemProxy ? (HttpMessageHandler)SystemProxyHandler : NoProxyHandler;
            var httpFactory = new MsalHttpClientFactory(handler); // large; CTS governs attempts
            Debug.WriteLine("############:TimeoutStart:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

            var pca = PublicClientApplicationBuilder.Create(clientId)
                .WithAuthority(authority)
                .WithHttpClientFactory(httpFactory)
                // (UPDATED) Verbose MSAL logs (toggle via listener)
                .WithLogging((lvl, msg, pii) => Debug.WriteLine($"{DateTime.Now.ToString()} | MSAL {lvl}: {msg}"),
                             LogLevel.Verbose, enablePiiLogging: false, enableDefaultPlatformLogging: true)
                .Build();

            // (UPDATED) Bounded retries with jitter
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                overallToken.ThrowIfCancellationRequested();

                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(overallToken);
                attemptCts.CancelAfter(TimeSpan.FromSeconds(20)); // per-attempt budget

                Debug.WriteLine($"[AAD] Path={(useSystemProxy ? "proxy" : "direct")}, attempt {attempt}, perAttempt=20s, connectTimeout={(useSystemProxy ? 8 : 3)}s");

                try
                {
                    var result = await pca
                        .AcquireTokenByUsernamePassword(scopes, username, securePassword)
                        .ExecuteAsync(attemptCts.Token)
                        .ConfigureAwait(false);

                    return result.AccessToken;
                }
                catch (TaskCanceledException ex) 
                {
                    // (UPDATED) Treat as transient; only throw if it’s the final attempt
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(300 * attempt, overallToken).ConfigureAwait(false);
                        continue;
                    }
                    // last attempt -> give caller a chance to try other path
                    Debug.WriteLine($"Final attempt canceled/timeboxed: {ex.GetType().Name}");
                    return null;
                }
                // Retry broader set of transient service-side failures
                catch (MsalServiceException ex) when (IsTransient(ex))
                {
                    // (UPDATED) Retry if not last; else return null for fallback
                    if (attempt < maxAttempts)
                    {
                        Debug.WriteLine($"MSAL transient ({ex.ErrorCode}, status? {SafeStatus(ex)}), retrying attempt {attempt + 1}...");
                        await Task.Delay(500 * attempt, overallToken).ConfigureAwait(false);
                        continue;
                    }
                    Debug.WriteLine($"MSAL transient on final attempt ({ex.ErrorCode}), returning null for fallback.");
                    return null; // <-- allow no-proxy path
                }
                catch (MsalClientException ex) when (LooksLikeNetwork(ex))
                {
                    // (UPDATED) Same idea for client/network issues
                    if (attempt < maxAttempts)
                    {
                        Debug.WriteLine($"MSAL client/network issue ({ex.ErrorCode}), retrying attempt {attempt + 1}...");
                        await Task.Delay(400 * attempt, overallToken).ConfigureAwait(false);
                        continue;
                    }
                    Debug.WriteLine($"MSAL client/network issue on final attempt ({ex.ErrorCode}), returning null for fallback.");
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    // (UPDATED) Transient socket/proxy errors: retry unless final
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(400 * attempt, overallToken).ConfigureAwait(false);
                        continue;
                    }
                    Debug.WriteLine($"HttpRequestException on final attempt: {ex.Message}, returning null for fallback.");
                    return null;
                }
                // Other MSAL errors (bad creds, invalid scope, etc.) bubble out
            }

            return null; //let caller try other path or fail
        }
        static bool LooksLikeNetwork(MsalClientException ex)
        {
            // MSAL surface codes vary; treat common network-ish cases as transient.
            var code = ex.ErrorCode?.ToLowerInvariant();
            return code is "network_not_available_error"
                         or "http_request_timeout"
                         or "authentication_canceled"     // sometimes thrown on network cancellation
                         or "unknown_error";              // be conservative; retries are bounded
        }
        static string SafeStatus(MsalServiceException ex)
        {
            try { return ex.StatusCode.ToString(); } catch { return "n/a"; }
        }
        private static bool IsTransient(MsalServiceException ex)
        {
            var code = ex.ErrorCode?.ToLowerInvariant();

            // Common transient buckets seen in the wild
            if (code is "temporarily_unavailable"           // AAD busy / retry
                     or "request_timeout"                   // AAD timed out the request
                     or "gateway_timeout"                   // 504 from a gateway
                     or "service_not_available"             // maintenance/partial outage
                     or "timeout"                           // generic timeout label
                     or "server_error")                     // 5xx umbrella
                return true;

            // (UPDATED) fall back on HTTP status if exposed
            // NOTE: MsalServiceException.StatusCode exists in recent MSAL versions
            try
            {
                var status = ex.StatusCode;
                if (status == 408 || status == 429 || (status >= 500 && status <= 599))
                    return true;
            }
            catch { /* older MSAL: StatusCode may not exist */ }

            return false;
        }
        #endregion
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

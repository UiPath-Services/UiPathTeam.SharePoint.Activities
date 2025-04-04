using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using UiPathTeam.SharePoint.RestAPI.Helpers;
using System.Net.Http;

namespace UiPathTeam.SharePoint.Service
{
    public enum SharePointLoginMode
    {
        Online,      // SPO using basic username/password (via token)
        OnPremises,  // On-premises using NetworkCredential
        AppOnly,     // App-only using ClientId/ClientSecret
        WebLogin,    // WebLogin
        AzureApp     // Azure App login (impersonation via AzureApp)
    }

    public class SharePointRestConnectionManager
    {
        // Required site URL.
        public string Url { get; set; }

        //// TenantId is required for Online, AppOnly, and AzureApp modes.
        //public string TenantId { get; set; }

        // TenantId is required for Online, AppOnly, and AzureApp modes.
        public string Authority { get; set; }

        // Credentials (only one of Password or SecurePassword should be provided).
        public string UserName { get; set; }
        public string Password { get; set; }
        //public SecureString SecurePassword { get; set; }

        // Login mode.
        public SharePointLoginMode LoginMode { get; set; } = SharePointLoginMode.Online;

        // AppOnly mode credentials.
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        // WebLogin mode options.
        public double LoginTimeout { get; set; } = 300000; // 5 minutes default
        public bool ResetCredentials { get; set; } = false;

        // AzureApp mode options.
        public string AzureAppId { get; set; }
        public string[] AzureAppPermissions { get; set; }

        // PlatformType for future use (Online or OnPremises).
        public string PlatformType { get; set; } = "Online";

        /// <summary>
        /// Returns an HttpClient configured for the selected login mode.
        /// </summary>
        public async Task<HttpClient> GetHttpClientAsync()
        {
            if (string.IsNullOrEmpty(Url))
                throw new ArgumentException("Site URL must be provided.");

            if(LoginMode == SharePointLoginMode.AzureApp)
            {

                var accessToken = await TokenHelper.GetAzureAppTokenAsync(Url, AzureAppId, UserName, Password, AzureAppPermissions);

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                return client;

            }
            else if (LoginMode == SharePointLoginMode.OnPremises)
            {
                var handler = new HttpClientHandler();
                handler.Credentials = new NetworkCredential(UserName, Password);
                return new HttpClient(handler);

            }
            else if (LoginMode == SharePointLoginMode.WebLogin && (string.IsNullOrEmpty(ClientId) || string.IsNullOrEmpty(Authority)))
            {


#if WINDOWS
                if (LoginTimeout == 0) LoginTimeout = 300000;
                HttpClient client = client = TokenHelper.GetWebLoginHttpClient(Url); ;
                var task = Task.Run(() =>
                {
                     client = TokenHelper.GetWebLoginHttpClient(Url);
                });
                if (task.Wait(TimeSpan.FromMilliseconds(LoginTimeout)))
                {
                    return client;
                }
                else
                {
                    throw new TimeoutException(Environment.NewLine + "Timed out! You took more than " + LoginTimeout.ToString() + " milliseconds to introduce your credentials!");
                }

                    
#else
                throw new Exception("WebLogin mode is not supported on non-Windows platforms.");
#endif
            }
            else
            {
                Uri siteUri = new Uri(Url);
                string tenantRoot = $"{siteUri.Scheme}://{siteUri.Host}";
                IEnumerable<string> defaultScope = new List<string> { $"{tenantRoot}/.default" };

                string accessToken = await TokenHelper.GetAccessTokenAsync(LoginMode, Url, ClientId, Authority, defaultScope, ClientSecret, UserName, Password);
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if(LoginMode == SharePointLoginMode.Online)
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "NONISV|UiPath|SharePointActivities/1.5.3"); // to match original implementation
                }
                return client;
            }
            
        }

        
    }
}

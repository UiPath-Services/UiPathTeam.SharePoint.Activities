using System.Drawing;
using UiPathTeam.SharePoint.RestAPI.Helpers;
using UiPathTeam.SharePoint.Service;

namespace UiPathTeam.SharePoint
{
    public class SharePointContextInfo
    {

        public static string Tag { get { return "SharePointContextInfoTag"; } }

        public string UserName;
        public string Url;
        public string Password;
        public SharePointType SharePointInstanceType;
        public SharePointPlatformType SharePointPlatformType;
        public string[] AzureAppPermissions;
        public bool ResetCredentials;
        public double LoginTimeout;
        public string ClientId;
        public string ClientSecret;
        public HttpClient currentClient;
        public bool groupQueries;
        public bool handleThrottling;

        public string AzureAppId;

        SharePointRestConnectionManager connManager;
        public SharePointContextInfo()
        {
            
        }
        

        public HttpClient GetSharePointContext()
        {
            if (currentClient != null)
                return currentClient;

            return Task.Run(() => GetSharePointContextAsync()).GetAwaiter().GetResult();

            //SharePointLoginMode loginMode = (SharePointLoginMode)Enum.Parse(typeof(SharePointLoginMode), SharePointInstanceType.ToString());
            //connManager = new SharePointRestConnectionManager
            //{
            //    Url = Url,
            //    ClientId = ClientId,
            //    // For Online mode, provide username and password (or SecurePassword).
            //    UserName = UserName,
            //    Password = Password,
            //    LoginMode = loginMode,
            //    ClientSecret = ClientSecret,
            //    ResetCredentials = ResetCredentials,
            //    LoginTimeout = LoginTimeout,
            //    AzureAppId = AzureAppId,
            //    AzureAppPermissions = AzureAppPermissions

            //};
            ////currentClient = await connManager.GetHttpClientAsync();
            //currentClient = connManager.GetHttpClientAsync().GetAwaiter().GetResult();
            //return currentClient;
            ////connManager = await connManager.GetHttpClientAsync()
        }
        private async Task<HttpClient> GetSharePointContextAsync()
        {
            if (currentClient != null)
                return currentClient;

            SharePointLoginMode loginMode = (SharePointLoginMode)Enum.Parse(typeof(SharePointLoginMode), SharePointInstanceType.ToString());
            connManager = new SharePointRestConnectionManager
            {
                Url = Url,
                ClientId = ClientId,
                // For Online mode, provide username and password (or SecurePassword).
                UserName = UserName,
                Password = Password,
                LoginMode = loginMode,
                ClientSecret = ClientSecret,
                ResetCredentials = ResetCredentials,
                LoginTimeout = LoginTimeout,
                AzureAppId = AzureAppId,
                AzureAppPermissions = AzureAppPermissions

            };
            currentClient = await connManager.GetHttpClientAsync().ConfigureAwait(false);
            //currentClient = connManager.GetHttpClientAsync().GetAwaiter().GetResult();
            return currentClient;
            //connManager = await connManager.GetHttpClientAsync()
        }
    }
}

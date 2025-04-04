using Microsoft.Extensions.Configuration;
using System.Net;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.Service;

namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    public class SharepointConnectionManagerTests
    {
        
        public static IConfigurationRoot Configuration { get; }
        static SharepointConnectionManagerTests()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // Use optional: false if you want to ensure the file exists; true otherwise
                .AddJsonFile("secrets.json", optional: false, reloadOnChange: true)
                .Build();
        }



        [Fact]
        public async Task ConnectionManager_OnPrem()
        {

            SharePointRestConnectionManager conn = new SharePointRestConnectionManager
            {
                LoginMode = SharePointLoginMode.OnPremises,
                Url = Configuration["SP_ONPREM_2019_SITE_URL"],
                UserName = Configuration["SP_ONPREM_2019_USERNAME"],
                Password = Configuration["SP_ONPREM_2019_PASSWORD"]
            };

            var _httpClient = await conn.GetHttpClientAsync();
            Assert.NotNull(_httpClient);

        }
        [Fact]
        public async Task ConnectionManager_Online()
        {

            SharePointRestConnectionManager conn = new SharePointRestConnectionManager
            {
                LoginMode = SharePointLoginMode.Online,
                Url = Configuration["SP_ONLINE_SITE_URL"],
                UserName = Configuration["SP_ONLINE_USERNAME"],
                Password = Configuration["SP_ONLINE_PASSWORD"]
            };

            var _httpClient = await conn.GetHttpClientAsync();
            Assert.NotNull(_httpClient);

        }

        [Fact]
        public async Task ConnectionManager_AppOnly_OnPrem()
        {

            SharePointRestConnectionManager conn = new SharePointRestConnectionManager
            {
                LoginMode = SharePointLoginMode.AppOnly,
                Url = Configuration["SP_ONPREM_2019_SITE_URL"],
                ClientId = Configuration["SP_ONPREM_2019_APPONLY_CLIENT_ID"],
                ClientSecret = Configuration["SP_ONPREM_2019_APPONLY_CLIENT_SECRET"]
            };

            var _httpClient = await conn.GetHttpClientAsync();
            Assert.NotNull(_httpClient);

        }
        [Fact]
        public async Task ConnectionManager_AppOnly_Online()
        {

            SharePointRestConnectionManager conn = new SharePointRestConnectionManager
            {
                LoginMode = SharePointLoginMode.AppOnly,
                Url = Configuration["SP_ONLINE_SITE_URL"],
                ClientId = Configuration["SP_ONLINE_APPONLY_CLIENT_ID"],
                ClientSecret = Configuration["SP_ONLINE_APPONLY_CLIENT_SECRET"]
            };

            var _httpClient = await conn.GetHttpClientAsync();
            Assert.NotNull(_httpClient);

        }


        [Fact]
        public async Task ConnectionManager_WebLogin_OnPrem()
        {

            SharePointRestConnectionManager conn = new SharePointRestConnectionManager
            {
                LoginMode = SharePointLoginMode.WebLogin,
                Url = Configuration["SP_ONPREM_2019_SITE_URL"],
                ResetCredentials = true
            };

            var _httpClient = await conn.GetHttpClientAsync();
            Assert.NotNull(_httpClient);
        }

        [Fact]
        public async Task ConnectionManager_WebLogin_Online()
        {

            SharePointRestConnectionManager conn = new SharePointRestConnectionManager
            {
                LoginMode = SharePointLoginMode.WebLogin,
                Url = Configuration["SP_ONLINE_SITE_URL"],
                ResetCredentials = false
            };
            if (conn.ResetCredentials)
            {
                await WebBrowserHelper.SharePointSignOutAsync(conn.Url);
            }
            var _httpClient = await conn.GetHttpClientAsync();
            Assert.NotNull(_httpClient);
        }

        [Fact]
        public async Task ConnectionManager_AzureApp()
        {
            string siteurl = Configuration["SP_ONLINE_SITE_URL"];
            AzureAppPermissions AzureAppPermissions = AzureAppPermissions.FullControl;

            var _scopes = TestsHelpers.GetAzureAppScopes(siteurl, AzureAppPermissions);

            SharePointRestConnectionManager conn = new SharePointRestConnectionManager
            {
                LoginMode = SharePointLoginMode.AzureApp,
                AzureAppId = Configuration["SP_ONLINE_AZUREAPP_APPID"],
                Url = siteurl,
                UserName = Configuration["SP_ONLINE_USERNAME"],
                Password = Configuration["SP_ONLINE_PASSWORD"],
                AzureAppPermissions = _scopes
            };

            var _httpClient = await conn.GetHttpClientAsync();
            Assert.NotNull(_httpClient);
        }
    }


}
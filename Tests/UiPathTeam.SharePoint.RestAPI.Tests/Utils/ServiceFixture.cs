using Microsoft.Extensions.Configuration;
using System.Security.Policy;
using UiPathTeam.SharePoint.RestAPI.Services;
using UiPathTeam.SharePoint.RestAPI.Tests;
using UiPathTeam.SharePoint.Service;



public class ServiceFixture<TService> where TService : SharePointBaseService
{
    public List<(LazyService<TService> Service, TestData Data)> ServicesWithData { get; }

    public static IConfigurationRoot Configuration { get; }

    static ServiceFixture()
    {
        Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // Use optional: false if you want to ensure the file exists; true otherwise
                .AddJsonFile("secrets.json", optional: false, reloadOnChange: true)
                .Build();
    }
    public ServiceFixture()
    {
        

        var connectionsWithData = new List<(SharePointRestConnectionManager Manager, TestData TestData)>
        {

            //// Online Username & Password site configuration
            (
                new SharePointRestConnectionManager
                {
                    Url = Configuration["SP_ONLINE_SITE_URL"],
                    LoginMode = SharePointLoginMode.Online,
                    UserName = Configuration["SP_ONLINE_USERNAME"],
                    Password = Configuration["SP_ONLINE_PASSWORD"],

                },
                TestDataHelper.GetTestData(SharePointType.Online, typeof(TService))
            ),

            ////On-premises 2016 site configuration
            //(
            //    new SharePointRestConnectionManager
            //    {
            //        Url = Configuration["SP_ONPREM_2016_SITE_URL"],
            //        UserName = Configuration["SP_ONPREM_2016_USERNAME"],
            //        Password = Configuration["SP_ONPREM_2016_PASSWORD"],
            //        LoginMode = SharePointLoginMode.OnPremises
            //    },
            //    TestDataHelper.GetTestData(SharePointType.Server2016, typeof(TService))
            //),
            //On-premises 2019 site configuration
            (
                new SharePointRestConnectionManager
                {
                    Url = Configuration["SP_ONPREM_2019_SITE_URL"],
                    UserName = Configuration["SP_ONPREM_2019_USERNAME"],
                    Password = Configuration["SP_ONPREM_2019_PASSWORD"],
                    LoginMode = SharePointLoginMode.OnPremises
                },
                TestDataHelper.GetTestData(SharePointType.Server2019, typeof(TService))
            )

            // Online Username & Password site configuration
            //(
            //    new SharePointRestConnectionManager
            //    {
            //        Url = Configuration["SP_ONLINE_SITE_URL"],
            //        LoginMode = SharePointLoginMode.AzureApp,
            //        AzureAppId = Configuration["SP_ONLINE_AZUREAPP_APPID"],
            //        UserName = Configuration["SP_ONLINE_USERNAME"],
            //        Password = Configuration["SP_ONLINE_PASSWORD"],
            //        AzureAppPermissions = Configuration["SP_ONLINE_AZUREAPP_PERMISSIONS"].Split(',')

            //    },
            //    TestDataHelper.GetTestData(SharePointType.Online, typeof(TService))
            //),

            ////// WebOnly site configuration
            ////(
            ////    new SharePointRestConnectionManager
            ////    {
            ////        Url = Configuration["SP_ONLINE_SITE_URL"],
            ////        LoginMode = SharePointLoginMode.WebLogin
            ////    },
            ////    TestDataHelper.GetTestData(SharePointType.Online, typeof(TService))
            ////),
            ////On-premises 2016 site configuration
            //(
            //    new SharePointRestConnectionManager
            //    {
            //        Url = Configuration["SP_ONPREM_2016_SITE_URL"],
            //        UserName = Configuration["SP_ONPREM_2016_USERNAME"],
            //        Password = Configuration["SP_ONPREM_2016_PASSWORD"],
            //        LoginMode = SharePointLoginMode.OnPremises
            //    },
            //    TestDataHelper.GetTestData(SharePointType.Server2016, typeof(TService))
            //),
            ////On-premises 2019 site configuration
            //(
            //    new SharePointRestConnectionManager
            //    {
            //        Url = Configuration["SP_ONPREM_2019_SITE_URL"],
            //        UserName = Configuration["SP_ONPREM_2019_USERNAME"],
            //        Password = Configuration["SP_ONPREM_2019_PASSWORD"],
            //        LoginMode = SharePointLoginMode.OnPremises
            //    },
            //    TestDataHelper.GetTestData(SharePointType.Server2019, typeof(TService))
            //)
        };


        

        ServicesWithData = connectionsWithData.Select(cwd => (
            Service: new LazyService<TService>(() => CreateServiceAsync(cwd.Manager), cwd.Manager), 
            TestData: cwd.TestData)).ToList();

    }

    private static async Task<TService> CreateServiceAsync(SharePointRestConnectionManager manager)
    {
        Console.WriteLine("Creating service started for: " + manager.Url);
        var client = await manager.GetHttpClientAsync();
        var service = Activator.CreateInstance(typeof(TService), client, manager.Url) as TService;
        Console.WriteLine("Create service ended for: " + manager.Url);
        return service;
    }
}
// Helper class for lazy service creation
public class LazyService<T> where T : SharePointBaseService
{
    private readonly Func<Task<T>> _factory;
    private T _service;
    private string Url { get; }
    private SharePointLoginMode LoginMode { get; }
    public LazyService(Func<Task<T>> factory, SharePointRestConnectionManager manager)
    {
        _factory = factory;
        Url = manager.Url;
        LoginMode = manager.LoginMode;
    }

    public async Task<T> GetServiceAsync()
    {
        if (_service == null)
        {
            _service = await _factory();
        }
        return _service;
    }
    public override string ToString()
    {
        return $"LazyService<{typeof(T).Name}> (Url: {Url}, LoginMode: {LoginMode})";
    }
}


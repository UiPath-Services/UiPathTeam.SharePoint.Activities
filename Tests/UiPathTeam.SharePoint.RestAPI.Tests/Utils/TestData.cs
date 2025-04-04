using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    public enum SharePointType
    {
        Online,      //SharePointOnline
        Server2016, //SharePointServer2016
        Server2019   //SharePointServer2019 
    }
    public abstract class TestData
    {
        
    }
    public class PermissionServiceTestData : TestData
    {
        
        public string SiteUrl { get; set; }
        public string ListName { get; set; }
        public string FolderPath { get; set; }
        public string Receiver { get; set; }

    }
    public class UserServiceTestData : TestData
    {
        public string UserName { get; set; }
        public string UserEmailToSearch { get; set; }

    }

    public class UtilsServiceTestData : TestData
    {
        public string SiteUrl { get; set; }

    }
    public class LibraryServiceTestData : TestData
    {
        public string SiteUrl { get; set; }
        public string LibraryName { get; set; }
        public string FolderPath { get; set; }
        public string FileRelativeUrl { get; set; }
        public string LocalTestFilePath { get; set; }
    }
    public class ListServiceTestData : TestData
    {
        public string ListName { get; set; }
    }

    public static class TestDataHelper
    {
        public static IConfigurationRoot Configuration { get; }
        static TestDataHelper()
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // Use optional: false if you want to ensure the file exists; true otherwise
                .AddJsonFile("secrets.json", optional: false, reloadOnChange: true)
                .Build();
        }
        public static TestData GetTestData(SharePointType serverType, Type serviceType)
        {

            if (serviceType == typeof(SharePointPermissionService))
            {
                switch(serverType)
                {
                    case SharePointType.Online:
                        return new PermissionServiceTestData
                        {
                            SiteUrl = Configuration["SP_ONLINE_SITE_URL"],
                            ListName = "Users List",
                            FolderPath = "/sites/IT/Shared Documents/folder_test_permissions",
                            Receiver = "abdullah@abumayar.com"
                        };
                    case SharePointType.Server2016:
                        return new PermissionServiceTestData
                        {
                            SiteUrl = Configuration["SP_ONPREM_2016_SITE_URL"],
                            ListName = "test_list",
                            FolderPath = "/sites/testsite2/Shared Documents/folder_test_permissions",
                            Receiver = "fahad"
                        };
                    case SharePointType.Server2019:
                        return new PermissionServiceTestData
                        {
                            SiteUrl = Configuration["SP_ONPREM_2019_SITE_URL"],
                            ListName = "laptop list",
                            FolderPath = "/sites/HR/HRLibrary/folder to test permission",
                            Receiver = "fahad"
                        };
                }
                
            }
            else if (serviceType == typeof(SharePointUserService))
            {
                switch (serverType)
                {
                    case SharePointType.Online:
                        return new UserServiceTestData
                        {
                            UserName = Configuration["SP_ONLINE_USERNAME"],
                            UserEmailToSearch = Configuration["SP_ONLINE_USERNAME"]
                        };
                    case SharePointType.Server2016:
                    case SharePointType.Server2019:
                        return new UserServiceTestData
                        {
                            UserName = "fahad",
                            UserEmailToSearch = "fahad@awlaqi.com"
                        };
                    
                }
                
            }
            else if (serviceType == typeof(SharePointListService))
            {
                switch (serverType)
                {
                    case SharePointType.Online:
                        return new ListServiceTestData
                        {
                            ListName = "Users List"
                        };
                    case SharePointType.Server2016:
                        return new ListServiceTestData
                        {
                            ListName = "test_list"
                        };
                    case SharePointType.Server2019:
                        return new ListServiceTestData
                        {
                            ListName = "laptop list"
                        };

                }
            }
            else if (serviceType == typeof(SharePointLibraryService))
            {
                switch (serverType)
                {
                    case SharePointType.Online:
                        return new LibraryServiceTestData
                        {
                            FileRelativeUrl = "/sites/IT/Shared Documents/folder1/TestFile.txt",
                            FolderPath = "/sites/IT/Shared Documents/folder1",
                            LibraryName = "Documents",
                            LocalTestFilePath = Path.Combine(Directory.GetCurrentDirectory(), "just_normal_textfile.txt"),
                            SiteUrl = Configuration["SP_ONLINE_SITE_URL"]

                        };
                    case SharePointType.Server2016:
                        return new LibraryServiceTestData
                        {
                            FileRelativeUrl = "/sites/testsite2/Shared Documents/folder1/TestFile.txt",
                            FolderPath = "/sites/testsite2/Shared Documents/folder1",
                            //LibraryName = Environment.GetEnvironmentVariable("SP_TEST_LIBRARY_NAME") ?? "Shared Documents",
                            LibraryName =  "Documents",
                            LocalTestFilePath = Path.Combine(Directory.GetCurrentDirectory(), "just_normal_textfile.txt"),
                            SiteUrl = Configuration["SP_ONPREM_2016_SITE_URL"]
                        };
                    case SharePointType.Server2019:
                        return new LibraryServiceTestData
                        {
                            FileRelativeUrl = "/sites/HR/HRLibrary/folder1/test file.txt",
                            FolderPath = "/sites/HR/HRLibrary/folder1",
                            LibraryName = "HRLibrary",
                            LocalTestFilePath = Path.Combine(Directory.GetCurrentDirectory(), "just_normal_textfile.txt"),
                            SiteUrl = Configuration["SP_ONPREM_2019_SITE_URL"]
                        };

                }
            }
            else if (serviceType == typeof(SharePointUtilsService))
            {
                switch (serverType)
                {
                    case SharePointType.Online:
                        return new UtilsServiceTestData
                        {
                            SiteUrl = Configuration["SP_ONLINE_SITE_URL"]
                        };
                    case SharePointType.Server2016:
                        return new UtilsServiceTestData
                        {
                            SiteUrl = Configuration["SP_ONPREM_2016_SITE_URL"]
                        };
                    case SharePointType.Server2019:
                        return new UtilsServiceTestData
                        {
                            SiteUrl = Configuration["SP_ONPREM_2019_SITE_URL"]
                        };

                }
            }
            return null;
            
        }
    }

}

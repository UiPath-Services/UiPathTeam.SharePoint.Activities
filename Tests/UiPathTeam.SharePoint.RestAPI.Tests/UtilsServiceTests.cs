using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    public class UtilsServiceTests : IClassFixture<ServiceFixture<SharePointUtilsService>>
    {
        private static readonly ServiceFixture<SharePointUtilsService> _fixture;
        //private readonly string _siteUrl;
        //private readonly string _testLibraryName;
        //private readonly string _testFolderPath;
        //private readonly string _testFileRelativeUrl;
        //private readonly string _localTestFilePath;

        static UtilsServiceTests()
        {
            _fixture = new ServiceFixture<SharePointUtilsService>();
        }
        public UtilsServiceTests()
        {
        }

        public static IEnumerable<object[]> GetUtilsServices()
        {
            return _fixture.ServicesWithData.Select(swd => new object[] { swd.Service, (UtilsServiceTestData)swd.Data });
        }

        [Theory]
        [MemberData(nameof(GetUtilsServices))]
        public async Task GetTimeZone_IntegrationTest(LazyService<SharePointUtilsService> lazyService, UtilsServiceTestData testData)
        {

            var _utilService = await lazyService.GetServiceAsync();
            // Assumes _testFileRelativeUrl refers to an existing file.
            var sizeZone = await _utilService.GetSPTimeZoneAsync(testData.SiteUrl);
            // If no error, file is checked out.
            Assert.NotNull(sizeZone);

        }

        [Theory]
        [MemberData(nameof(GetUtilsServices))]
        public async Task GetCurrentUser_IntegrationTest(LazyService<SharePointUtilsService> lazyService, UtilsServiceTestData testData)
        {

            var _utilService = await lazyService.GetServiceAsync();
            // Assumes _testFileRelativeUrl refers to an existing file.
            var cookies = WebBrowserHelper.SharepointCookies(testData.SiteUrl, true);

            var sizeZone = await _utilService.GetCurrentUserAsync(testData.SiteUrl);
            // If no error, file is checked out.
            Assert.NotNull(sizeZone);

        }

    }
}

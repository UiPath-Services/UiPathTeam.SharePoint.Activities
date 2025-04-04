using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    
    public class PermissionServiceTests : IClassFixture<ServiceFixture<SharePointPermissionService>>
    {
        private static readonly ServiceFixture<SharePointPermissionService> _fixture;
        static PermissionServiceTests()
        {
            _fixture = new ServiceFixture<SharePointPermissionService>();
        }
        ////private readonly SharePointPermissionService _permService;
        //// For list-level tests.
        //private readonly string _testListName;
        //// For folder-level tests, ensure this folder exists in your test list/library.
        //private readonly string _testFolderPath;
        //// For site-level tests, list name is empty.
        //// Receiver is the user or group to grant permission.
        //private readonly string _testReceiver;
        //// For our tests, we'll use a specific ListType (adjust if needed).
        //private readonly ListType _listType = ListType.Library;
        //private readonly string _siteUrl;

        public PermissionServiceTests()
        {
            //_siteUrl = "http://win-igsiph0j410/sites/testsite2";
            //_testListName = "test_list";
            //_testFolderPath = "/sites/testsite2/Shared Documents/folder1";
            //_testReceiver = "fahad";
        }

        public static IEnumerable<object[]> GetPermissionServices()
        {
            return _fixture.ServicesWithData.Select(swd => new object[] { swd.Service, (PermissionServiceTestData)swd.Data });
        }

        #region AddPermissionAsync Tests

        [Theory]
        [MemberData(nameof(GetPermissionServices))]
        public async Task AddAndGetPermissionAsync_SiteLevel_AddsPermission(LazyService<SharePointPermissionService> lazyService, PermissionServiceTestData testData)
        {
            string _siteUrl = testData.SiteUrl;
            string _testListName = testData.ListName;
            string _testFolderPath = testData.FolderPath;
            string _testReceiver = testData.Receiver;
            ListType _listType = ListType.Library;

            var _permService = await lazyService.GetServiceAsync();
            // Site-level: listName and folderPath are empty.
            await _permService.AddPermissionAsync("", _testReceiver, true, RoleType.Reader, "", _listType);

            var perms = await _permService.GetAllPermissionsAsync("", "", _listType);
            Assert.Contains(perms, p => p.Item1.Contains(_testReceiver, StringComparison.OrdinalIgnoreCase) &&
                                         p.Item2.Equals("Read", StringComparison.OrdinalIgnoreCase));

            // Cleanup: Remove permission.
            await _permService.RemovePermissionAsync("", "", _listType, _testReceiver, true);
        }

        [Theory]
        [MemberData(nameof(GetPermissionServices))]
        public async Task AddAndGetPermissionAsync_ListLevel_AddsPermission(LazyService<SharePointPermissionService> lazyService, PermissionServiceTestData testData)
        {
            string _siteUrl = testData.SiteUrl;
            string _testListName = testData.ListName;
            string _testFolderPath = testData.FolderPath;
            string _testReceiver = testData.Receiver;
            ListType _listType = ListType.Library;
            var _permService = await lazyService.GetServiceAsync();
            // List-level: listName provided, folderPath empty.
            await _permService.AddPermissionAsync(_testListName, _testReceiver, true, RoleType.Reader, "", _listType);

            var perms = await _permService.GetAllPermissionsAsync(_testListName, "", _listType);
            Assert.Contains(perms, p => p.Item1.Contains(_testReceiver, StringComparison.OrdinalIgnoreCase) &&
                                         p.Item2.Equals("Read", StringComparison.OrdinalIgnoreCase));

            // Cleanup: Remove permission.
            await _permService.RemovePermissionAsync(_testListName, "", _listType, _testReceiver, true);
        }

        [Theory]
        [MemberData(nameof(GetPermissionServices))]
        public async Task AddِAndGetPermissionAsync_FolderLevel_AddsPermission(LazyService<SharePointPermissionService> lazyService, PermissionServiceTestData testData)
        {
            string _siteUrl = testData.SiteUrl;
            string _testListName = testData.ListName;
            string _testFolderPath = testData.FolderPath;
            string _testReceiver = testData.Receiver;
            ListType _listType = ListType.Library;
            var _permService = await lazyService.GetServiceAsync();
            // Folder-level: both listName and folderPath provided.
            await _permService.AddPermissionAsync(_testListName, _testReceiver, true, RoleType.Reader, _testFolderPath, _listType);

            var perms = await _permService.GetAllPermissionsAsync(_testListName, _testFolderPath, _listType);
            Assert.Contains(perms, p => p.Item1.Contains(_testReceiver, StringComparison.OrdinalIgnoreCase) &&
                                         p.Item2.Equals("Read", StringComparison.OrdinalIgnoreCase));

            // Cleanup: Remove permission.
            await _permService.RemovePermissionAsync(_testListName, _testFolderPath, _listType, _testReceiver, true);
        }

        #endregion

        #region GetAllPermissionsAsync Tests

        //[Theory]
        //[MemberData(nameof(GetPermissionServices))]
        //public async Task GetAllPermissionsAsync_SiteLevel_ReturnsPermissions(LazyService<SharePointPermissionService> lazyService, PermissionServiceTestData testData)
        //{
        //    ListType _listType = ListType.Library;
        //    var _permService = await lazyService.GetServiceAsync();
        //    // Site-level: listName and folderPath empty.
        //    var perms = await _permService.GetAllPermissionsAsync("", "", _listType);
        //    Assert.NotEmpty(perms);
        //}


        #endregion

        #region RemovePermissionAsync Tests

        [Theory]
        [MemberData(nameof(GetPermissionServices))]
        public async Task RemovePermissionAsync_SiteLevel_RemovesPermission(LazyService<SharePointPermissionService> lazyService, PermissionServiceTestData testData)
        {
            string _testReceiver = testData.Receiver;
            ListType _listType = ListType.Library;
            var _permService = await lazyService.GetServiceAsync();
            // Add permission at site-level.
            await _permService.AddPermissionAsync("", _testReceiver, true, RoleType.Reader, "", _listType);
            await _permService.RemovePermissionAsync("", "", _listType, _testReceiver, true);

            var perms = await _permService.GetAllPermissionsAsync("", "", _listType);
            Assert.DoesNotContain(perms, p => p.Item1.Contains(_testReceiver, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [MemberData(nameof(GetPermissionServices))]
        public async Task RemovePermissionAsync_ListLevel_RemovesPermission(LazyService<SharePointPermissionService> lazyService, PermissionServiceTestData testData)
        {
            string _testListName = testData.ListName;
            string _testReceiver = testData.Receiver;
            ListType _listType = ListType.Library;

            var _permService = await lazyService.GetServiceAsync();
            await _permService.AddPermissionAsync(_testListName, _testReceiver, true, RoleType.Reader, "", _listType);
            await _permService.RemovePermissionAsync(_testListName, "", _listType, _testReceiver, true);

            var perms = await _permService.GetAllPermissionsAsync(_testListName, "", _listType);
            Assert.DoesNotContain(perms, p => p.Item1.Contains(_testReceiver, StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [MemberData(nameof(GetPermissionServices))]
        public async Task RemovePermissionAsync_FolderLevel_RemovesPermission(LazyService<SharePointPermissionService> lazyService, PermissionServiceTestData testData)
        {
            string _testListName = testData.ListName;
            string _testFolderPath = testData.FolderPath;
            string _testReceiver = testData.Receiver;
            ListType _listType = ListType.Library;
            var _permService = await lazyService.GetServiceAsync();
            await _permService.AddPermissionAsync(_testListName, _testReceiver, true, RoleType.Reader, _testFolderPath, _listType);
            await _permService.RemovePermissionAsync(_testListName, _testFolderPath, _listType, _testReceiver, true);

            var perms = await _permService.GetAllPermissionsAsync(_testListName, _testFolderPath, _listType);
            Assert.DoesNotContain(perms, p => p.Item1.Contains(_testReceiver, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}

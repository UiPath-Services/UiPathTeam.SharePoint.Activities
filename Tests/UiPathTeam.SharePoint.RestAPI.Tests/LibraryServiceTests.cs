using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    public class LibraryServiceTests : IClassFixture<ServiceFixture<SharePointLibraryService>>
    {
        private static readonly ServiceFixture<SharePointLibraryService> _fixture;

        static LibraryServiceTests()
        {
            _fixture = new ServiceFixture<SharePointLibraryService>();

            
        }
        public LibraryServiceTests()
        {
          
        }
        

        public static IEnumerable<object[]> GetLibraryServices()
        {
            return _fixture.ServicesWithData.Select(swd => new object[] { swd.Service, (LibraryServiceTestData)swd.Data });
        }


        #region File CheckIn/CheckOut Tests

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task CheckOutAndCheckInFile_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {
            //string _siteUrl = testData.SiteUrl;
            //string _testLibraryName = testData.LibraryName;
            //string _testFolderPath = testData.FolderPath;
            string _testFileRelativeUrl = testData.FileRelativeUrl;
            //string _localTestFilePath = testData.LocalTestFilePath;


            var _libService = await lazyService.GetServiceAsync();
            // Assumes _testFileRelativeUrl refers to an existing file.
            await _libService.CheckOutFileAsync(_testFileRelativeUrl);
            // If no error, file is checked out.


            await _libService.CheckInFileAsync(_testFileRelativeUrl, "Checked in via test", 1);
        }

        //[Theory]
        //[MemberData(nameof(GetLibraryServices))]
        //public async Task CheckOutFile_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        //{
        //    //string _siteUrl = testData.SiteUrl;
        //    //string _testLibraryName = testData.LibraryName;
        //    //string _testFolderPath = testData.FolderPath;
        //    //string _testFileRelativeUrl = testData.FileRelativeUrl;
        //    //string _localTestFilePath = testData.LocalTestFilePath;
        //    string _testFileRelativeUrl = "Shared Documents/folder120250327_110231/sample_file.txt";
        //    _testFileRelativeUrl = OtherHelpers.ResolveRelativePath("http://win-igsiph0j410/sites/testsite2/", _testFileRelativeUrl);

        //    var _libService = await lazyService.GetServiceAsync();
        //    // Assumes _testFileRelativeUrl refers to an existing file.
        //    await _libService.CheckOutFileAsync(_testFileRelativeUrl);
        //    // If no error, file is checked out.


        //    await _libService.CheckInFileAsync(_testFileRelativeUrl, "Checked in via test", 1);
        //}

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task DiscardCheckoutFile_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {

            string _testFileRelativeUrl = testData.FileRelativeUrl;
            //string _testFileRelativeUrl = "Shared Documents/folder120250327_110231/sample_file.txt";
            //_testFileRelativeUrl = OtherHelpers.ResolveRelativePath("http://win-igsiph0j410/sites/testsite2/", _testFileRelativeUrl);


            var _libService = await lazyService.GetServiceAsync();
            await _libService.CheckOutFileAsync(_testFileRelativeUrl);
            await _libService.DiscardCheckoutAsync(_testFileRelativeUrl);
        }


        #endregion

        #region Folder Operations Tests

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task CreateAndDeleteFolder_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {

            string _testLibraryName = testData.LibraryName;
            //string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";

            var _libService = await lazyService.GetServiceAsync();


            //string libraryRelativeUrl2 = await _libService.GetLibraryRootFolderRelativeUrl("Shared Documents");
            //string libraryRelativeUrl3 = await _libService.GetLibraryRootFolderRelativeUrl("Documents");
            // Create a new folder under the library.
            string newFolderName = "IntegrationTestFolder_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            // Construct a full server-relative URL for the new folder.
            //string newFolderRelativeUrl = $"{_testFolderPath}{newFolderName}";
            //await _libService.CreateFolderAsync(_testLibraryName, newFolderRelativeUrl);
            await _libService.CreateFolderAsync(_testLibraryName, newFolderName);

            string libraryRelativeUrl = await _libService.GetLibraryRootFolderRelativeUrl(_testLibraryName);
            var newFolderServerRelativeUrl = $"{libraryRelativeUrl.TrimEnd("/".ToCharArray())}/{newFolderName.TrimStart("/".ToCharArray())}";
            // Verify the folder exists by checking the children of its parent folder.
            //string parentFolder = $"/sites/testsite2/Shared Documents";
            //string parentFolder = $"{libraryRelativeUrl}";
            string parentFolder = newFolderServerRelativeUrl.Substring(0, newFolderServerRelativeUrl.LastIndexOf('/'));
            string[] children = await _libService.GetChildrenNamesAsync(parentFolder);
            Assert.Contains(newFolderName, children);

            // Clean up: delete the new folder.
            await _libService.DeleteAsync(_testLibraryName, newFolderName, false);
        }

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task GetChildrenNames_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {
            var _libService = await lazyService.GetServiceAsync();


            string _testFolderPath = testData.FolderPath;

            _testFolderPath = TestsHelpers.ResolveRelativePath(testData.SiteUrl, _testFolderPath);
            // Get children of the test folder.
            string[] children = await _libService.GetChildrenNamesAsync(_testFolderPath);
            Assert.NotNull(children);
            Assert.NotEmpty(children);
        }

        #endregion

        #region File Download/Move/Rename Tests

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task GetFileDownload_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {

            string _testFileRelativeUrl = testData.FileRelativeUrl;

            var _libService = await lazyService.GetServiceAsync();

            _testFileRelativeUrl = TestsHelpers.ResolveRelativePath(testData.SiteUrl, _testFileRelativeUrl);
            // Download a test file.
            string downloadPath = Path.Combine(Directory.GetCurrentDirectory(), "DownloadedTestFile.txt");
            await _libService.GetFileAsync(downloadPath, _testFileRelativeUrl);
            Assert.True(System.IO.File.Exists(downloadPath));
            System.IO.File.Delete(downloadPath); // Cleanup.
        }
        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task MoveItemAsync_File_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {

            string _testLibraryName = testData.LibraryName;
            string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";
            string _localTestFilePath = testData.LocalTestFilePath;
            var _libService = await lazyService.GetServiceAsync();
            // Create a unique file name for upload.
            string originalFileName = "IntegrationMoveTestFile_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt";
            string originalRelativeUrl = $"{_testFolderPath}{originalFileName}";

            // Ensure the local test file exists.
            if (!System.IO.File.Exists(_localTestFilePath))
            {
                await System.IO.File.WriteAllTextAsync(_localTestFilePath, "Test content for moving file");
            }

            // Upload the file.
            await _libService.UploadFileAsync(originalRelativeUrl, _localTestFilePath, null, true, true, false, false);

            // Define new destination relative URL for the file.
            string newFileName = "Moved_" + originalFileName;
            string destinationRelativeUrl = $"{_testFolderPath}{newFileName}";

            // Move the file.
            await _libService.MoveItemAsync(originalRelativeUrl, destinationRelativeUrl, true);

            // Verify the move.
            bool oldFileExists = await _libService.FileExistsAsync(originalRelativeUrl);
            bool newFileExists = await _libService.FileExistsAsync(destinationRelativeUrl);
            Assert.False(oldFileExists);
            Assert.True(newFileExists);

            // Cleanup: delete the moved file.
            await _libService.DeleteAsync(_testLibraryName, destinationRelativeUrl, true);
        }

        //[Theory]
        //[MemberData(nameof(GetLibraryServices))]
        //public async Task MoveItemAsync2_File_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        //{
        //    var sourceRelativeUrl = "Shared Documents/folder120250404_043905/sample_file20250404_043905_renamed.txt";
        //    //var destinationRelativeUrl = "/sites/testsite2/Shared Documents/folder120250404_043904";
        //    var destinationRelativeUrl = "/sites/IT/Shared Documents/folder120250404_043904";

        //    sourceRelativeUrl = TestsHelpers.ResolveRelativePath(testData.SiteUrl, sourceRelativeUrl);
        //    destinationRelativeUrl = TestsHelpers.ResolveRelativePath(testData.SiteUrl, destinationRelativeUrl);


        //    var _libService = await lazyService.GetServiceAsync();
            

        //    // Move the file.
        //    //await _libService.MoveItemAsync(sourceRelativeUrl, destinationRelativeUrl, true);
        //    await _libService.MoveItemOnPremAsync(sourceRelativeUrl, destinationRelativeUrl, true);


        //}

        //[Theory]
        //[MemberData(nameof(GetLibraryServices))]
        //public async Task MoveItemAsync2_Folder_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        //{

        //    var sourceRelativeUrl = "Shared Documents/IntegrationMoveTestFolder_49f65e77";
        //    //    //var destinationRelativeUrl = "/sites/testsite2/Shared Documents/folder120250404_043904";
        //    var destinationRelativeUrl = "Shared Documents/Moved_IntegrationMoveTestFolder_49f65e77";
        //    // Move the folder.

        //    sourceRelativeUrl = TestsHelpers.ResolveRelativePath(testData.SiteUrl, sourceRelativeUrl);
        //    destinationRelativeUrl = TestsHelpers.ResolveRelativePath(testData.SiteUrl, destinationRelativeUrl);

        //    var _libService = await lazyService.GetServiceAsync();
        //    await _libService.MoveItemAsync(sourceRelativeUrl, destinationRelativeUrl, true);

        //}

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task MoveItemAsync_Folder_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {

            string _testLibraryName = testData.LibraryName;
            string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";

            var _libService = await lazyService.GetServiceAsync();
            // Create a new folder.
            string originalFolderName = "IntegrationMoveTestFolder_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            //string originalFolderRelativeUrl = $"{_testFolderPath}{originalFolderName}";
            await _libService.CreateFolderAsync(_testLibraryName, originalFolderName);
            var libraryRootFolder = await _libService.GetLibraryRootFolderRelativeUrl(_testLibraryName);

            var originalFolderRelativeUrl = $"{libraryRootFolder.TrimEnd("/".ToCharArray())}/{originalFolderName.TrimStart("/".ToCharArray())}";
            // Define new destination folder URL.
            string newFolderName = "Moved_" + originalFolderName;
            await _libService.CreateFolderAsync(_testLibraryName, newFolderName);
            string destinationFolderRelativeUrl = $"{libraryRootFolder.TrimEnd("/".ToCharArray())}/{newFolderName.TrimStart("/".ToCharArray())}";

            // Move the folder.
            await _libService.MoveItemAsync(originalFolderRelativeUrl, destinationFolderRelativeUrl, true);

            // Verify: Check the parent folder for the new folder name.
            //string parentFolder = "/sites/testsite2/Shared Documents";
            string parentFolder = destinationFolderRelativeUrl.Substring(0, destinationFolderRelativeUrl.LastIndexOf('/'));
            string[] children = await _libService.GetChildrenNamesAsync(parentFolder);
            Assert.Contains(newFolderName, children);

            // Cleanup: delete the moved folder.
            await _libService.DeleteAsync(_testLibraryName, destinationFolderRelativeUrl, true);
        }

        //[Theory]
        //[MemberData(nameof(GetLibraryServices))]
        //public async Task MoveItem2Async_Folder_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        //{

        //    //string _testLibraryName = testData.LibraryName;
        //    //string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";

        //    var _libService = await lazyService.GetServiceAsync();
        //    // Create a new folder.
        //    string sourceRelativeUrl = OtherHelpers.ResolveRelativePath("http://win-igsiph0j410/sites/testsite2/", "Shared Documents/folder120250327_005738/robot_error.JPG");
        //    string destinationRelativeUrl = OtherHelpers.ResolveRelativePath("http://win-igsiph0j410/sites/testsite2/", "/sites/testsite2/Shared Documents/folder1");

        //    // Move the folder.
        //    await _libService.MoveItemAsync(sourceRelativeUrl, destinationRelativeUrl, true);


        //}
        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task MoveAndRenameItem_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {

            string _testLibraryName = testData.LibraryName;
            string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";
            string _localTestFilePath = testData.LocalTestFilePath;

            
            var _libService = await lazyService.GetServiceAsync();
            // Upload a test file for move/rename operations.
            string originalFileName = "IntegrationUploadTest_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt";
            string uploadRelativeUrl = $"{_testFolderPath}{originalFileName}";
            // Ensure local test file exists.
            if (!System.IO.File.Exists(_localTestFilePath))
                await System.IO.File.WriteAllTextAsync(_localTestFilePath, "Test content for move/rename");

            await _libService.UploadFileAsync(uploadRelativeUrl, _localTestFilePath, null, true, true, false, false);

            // Move the file to a new location within the same library.
            string movedFileUrl = $"{_testFolderPath}Moved_{originalFileName}";
            await _libService.MoveItemAsync(uploadRelativeUrl, movedFileUrl, true);

            // Rename the moved file.
            string renamedFileUrl = $"{_testFolderPath}Renamed_{originalFileName}";
            await _libService.RenameItemAsync(movedFileUrl, "Renamed_" + originalFileName);

            // Clean up: delete the renamed file.
            await _libService.DeleteAsync(_testLibraryName, renamedFileUrl, true);
        }

        #endregion

        #region Upload Tests

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task UploadFile_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {
            string _testLibraryName = testData.LibraryName;
            string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";

            string _localTestFilePath = testData.LocalTestFilePath;
            var _libService = await lazyService.GetServiceAsync();

            string uploadFileName = "IntegrationUploadTest_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt";
            string uploadRelativeUrl = $"{_testFolderPath}{uploadFileName}";

            // Ensure local file exists.
            if (!System.IO.File.Exists(_localTestFilePath))
            {
                await System.IO.File.WriteAllTextAsync(_localTestFilePath, "Test content for upload");
            }

            await _libService.UploadFileAsync(uploadRelativeUrl, _localTestFilePath, null, true, true, true, true);

            // Download to verify upload.
            string downloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloaded_" + uploadFileName);
            await _libService.GetFileAsync(downloadPath, uploadRelativeUrl);
            Assert.True(System.IO.File.Exists(downloadPath));

            // Clean up.
            System.IO.File.Delete(downloadPath);
            await _libService.DeleteAsync(_testLibraryName, uploadRelativeUrl, true);
        }
        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task RenameFile_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {
            string _testLibraryName = testData.LibraryName;
            string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";

            string _localTestFilePath = testData.LocalTestFilePath;
            var _libService = await lazyService.GetServiceAsync();

            string uploadFileName = "IntegrationUploadTest_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt";
            string uploadRelativeUrl = $"{_testFolderPath}{uploadFileName}";

            // Ensure local file exists.
            if (!System.IO.File.Exists(_localTestFilePath))
            {
                await System.IO.File.WriteAllTextAsync(_localTestFilePath, "Test content for upload");
            }

            await _libService.UploadFileAsync(uploadRelativeUrl, _localTestFilePath, null, true, true, true, true);

            // Rename the uploaded file.
            string newFileName = "Renamed_" + uploadFileName;
            string renamedFileUrl = $"{_testFolderPath}{newFileName}";
            await _libService.RenameItemAsync(uploadRelativeUrl, newFileName);
            // Verify the rename.
                
            // Download to verify upload.
            string downloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Downloaded_" + uploadFileName);


            // Clean up.
            System.IO.File.Delete(downloadPath);
            await _libService.DeleteAsync(_testLibraryName, renamedFileUrl, true);
        }

        [Theory]
        [MemberData(nameof(GetLibraryServices))]
        public async Task UploadLargeFile_IntegrationTest(LazyService<SharePointLibraryService> lazyService, LibraryServiceTestData testData)
        {

            string _testLibraryName = testData.LibraryName;
            string _testFolderPath = testData.FolderPath.EndsWith("/") ? testData.FolderPath : testData.FolderPath + "/";

            var _libService = await lazyService.GetServiceAsync();

            string uploadFileName = "IntegrationLargeUploadTest_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pdf";

            //string uploadRelativeUrl = $"/sites/testsite2/Shared Documents/{uploadFileName}";
            string uploadRelativeUrl = $"{_testFolderPath}{uploadFileName}";
            //string largeTestFilePath = _localTestFilePath; // Use local file as base.
            string largeTestFilePath = @"C:\Users\Abdullah.Al-Awlaqi\Downloads\598_WI2022_lecture04.pdf";

            // If file is too small, duplicate its content to simulate a larger file.
            FileInfo fi = new FileInfo(largeTestFilePath);
            if (fi.Length < 10 * 1024 * 1024)
            {
                string content = await System.IO.File.ReadAllTextAsync(largeTestFilePath);
                content = string.Concat(System.Linq.Enumerable.Repeat(content, 10000));
                await System.IO.File.WriteAllTextAsync(largeTestFilePath, content);
            }

            await _libService.UploadLargeFileAsync(uploadRelativeUrl, largeTestFilePath, null, true, true, 10, true, true);

            // Download to verify upload.
            string downloadPath = Path.Combine(Directory.GetCurrentDirectory(), "DownloadedLarge_" + uploadFileName);
            await _libService.GetFileAsync(downloadPath, uploadRelativeUrl);
            Assert.True(System.IO.File.Exists(downloadPath));

            // Clean up.
            System.IO.File.Delete(downloadPath);
            await _libService.DeleteAsync(_testLibraryName, uploadRelativeUrl, true);
        }
        
        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;
using UiPathTeam.SharePoint.Service;


namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    

    public class ListServiceTests : IClassFixture<ServiceFixture<SharePointListService>>
    {
        //private const string _listName = "test_list"; // Replace with your actual list name. If make sure the same testname available in all SharePointListService instances


        private static readonly ServiceFixture<SharePointListService> _fixture;

        static ListServiceTests()
        {
            _fixture = new ServiceFixture<SharePointListService>();
        }
        

        public static IEnumerable<object[]> GetListServices()
        {
            return _fixture.ServicesWithData.Select(swd => new object[] { swd.Service, (ListServiceTestData)swd.Data });
        }

        [Theory]
        [MemberData(nameof(GetListServices))]
        public async Task AddListItemAsync_Returns_AddedItemId_IntegrationTest(LazyService<SharePointListService> lazyService, ListServiceTestData testData)
        {
            var _listName = testData.ListName;
            var service = await lazyService.GetServiceAsync();
            //var result = await service.GetListsAsync(); // Replace with your actual test logic
            //Assert.NotNull(result);

            //Console.WriteLine(service.ToString());
            var properties = new Dictionary<string, object>
                    {
                        { "Title", "Integration Test Item2222" }
                    };

            int itemId = await service.AddListItemAsync(_listName, properties);
            Assert.True(itemId > 0, "Item ID should be greater than 0");
        }

        [Theory]
        [MemberData(nameof(GetListServices))]
        public async Task AddListItemAttachmentsAsync_Succeeds_IntegrationTest(LazyService<SharePointListService> lazyService, ListServiceTestData testData)
        {
            var _listName = testData.ListName;

            //var _listName = "YourListName"; // Replace with your actual list name
            var _service = await lazyService.GetServiceAsync();

            var properties = new Dictionary<string, object> { { "Title", "Item for Attachments" } };
            int itemId = await _service.AddListItemAsync(_listName, properties);

            var attachments = new List<Attachment>
            {
                new Attachment { FileName = "integration1.txt", FileContent = new byte[] { 1, 2, 3, 4 } },
            };

            string filePath = @"C:\Users\Abdullah.Al-Awlaqi\Downloads\SharePoint Custom Activities Documentation (4).pdf";
            if (!System.IO.File.Exists(filePath))
                throw new Exception($"File not found: {filePath}");

            attachments.Add(new Attachment
            {
                FileName = Path.GetFileName(filePath),
                FileContent = File.ReadAllBytes(filePath)
            });

            await _service.AddListItemAttachmentsAsync(_listName, itemId, attachments);

            var attNames = await _service.GetListItemAttachmentsAsync(_listName, itemId);
            var attList = attNames.ToList();

            Assert.Contains("integration1.txt", attList);
            Assert.Contains(Path.GetFileName(filePath), attList);
        }


        [Theory]
        [MemberData(nameof(GetListServices))]
        public async Task DeleteListItemAttachmentsAsync_Returns_CorrectDeletedCount_IntegrationTest(LazyService<SharePointListService> lazyService, ListServiceTestData testData)
        {
            var _listName = testData.ListName;

            var _service = await lazyService.GetServiceAsync();
            // Arrange
            // First, add an item and attachments.
            var properties = new Dictionary<string, object> { { "Title", "Item for Attachment Deletion" } };
            int itemId = await _service.AddListItemAsync(_listName, properties);
            var attachments = new List<Attachment>
            {
                new Attachment { FileName = "del1.txt", FileContent = new byte[] { 1, 2 } },
                new Attachment { FileName = "del2.txt", FileContent = new byte[] { 3, 4 } }
            };
            await _service.AddListItemAttachmentsAsync(_listName, itemId, attachments);

            // Act: Delete attachments.
            int deletedCount = await _service.DeleteListItemAttachmentsAsync(_listName, itemId, new List<string> { "del1.txt", "del2.txt" });

            // Assert
            Assert.Equal(2, deletedCount);
        }

        [Theory]
        [MemberData(nameof(GetListServices))]
        public async Task DeleteListItemsAsync_Returns_NumberOfRowsAffected_IntegrationTest(LazyService<SharePointListService> lazyService, ListServiceTestData testData)
        {
            var _listName = testData.ListName;

            var _service = await lazyService.GetServiceAsync();
            string TitleToDelete = "Item to Delete";
            // Arrange
            // Optionally add a test item that will be deleted.
            var properties = new Dictionary<string, object> { { "Title", TitleToDelete } };
            int addedItemID = await _service.AddListItemAsync(_listName, properties);

            //string camlQuery = $"<View><Query><Where><Eq><FieldRef Name=\"ID\" /><Value Type=\"Integer\">{addedItemID}</Value></Eq></<Where></Query></View>";
            string camlQuery = String.Format(@"<View>
                                  <Query>
                                    <Where>
                                      <Eq>
                                        <FieldRef Name=""Title"" />
                                        <Value Type=""Text"">{0}</Value>
                                      </Eq>
                                    </Where>
                                  </Query>
                                </View>", TitleToDelete);

            // Act: Delete all items in the list.
            int deletedCount = await _service.DeleteListItemsAsync(_listName, 2, camlQuery);

            // Assert
            Assert.True(deletedCount > 0, "At least one item should be deleted.");
        }

        [Theory]
        [MemberData(nameof(GetListServices))]
        public async Task GetListItemAttachmentsAsync_Returns_AttachmentNames_IntegrationTest(LazyService<SharePointListService> lazyService, ListServiceTestData testData)
        {
            var _listName = testData.ListName;
            var _service = await lazyService.GetServiceAsync();
            // Arrange
            // Add an item and attachments.
            var properties = new Dictionary<string, object> { { "Title", "Item for Getting Attachments" } };
            int itemId = await _service.AddListItemAsync(_listName, properties);
            var attachments = new List<Attachment>
            {
                new Attachment { FileName = "get1.txt", FileContent = new byte[] { 1 } }
            };
            await _service.AddListItemAttachmentsAsync(_listName, itemId, attachments);

            // Act
            IEnumerable<string> attNames = await _service.GetListItemAttachmentsAsync(_listName, itemId);
            var attList = new List<string>(attNames);

            // Assert
            Assert.Contains("get1.txt", attList);
        }

        [Theory]
        [MemberData(nameof(GetListServices))]
        public async Task ReadListItemsAsync_Returns_Items_IntegrationTest(LazyService<SharePointListService> lazyService, ListServiceTestData testData)
        {
            var _listName = testData.ListName;

            var _service = await lazyService.GetServiceAsync();
            // Arrange
            // Add an item so there is at least one item to read.
            var properties = new Dictionary<string, object> { { "Title", "Item for Reading" } };
            await _service.AddListItemAsync(_listName, properties);

            // Act
            var result = await _service.ReadListItemsAsync(_listName, "<View><Query></Query></View>");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ItemsDictArray);
            Assert.NotNull(result.ItemsTable);
            Assert.True(result.ItemsDictArray.Count > 0, "There should be at least one item returned.");
        }

        [Theory]
        [MemberData(nameof(GetListServices))]
        public async Task UpdateListItemsAsync_Returns_NumberOfRowsAffected_IntegrationTest(LazyService<SharePointListService> lazyService, ListServiceTestData testData)
        {
            var _listName = testData.ListName;

            var _service = await lazyService.GetServiceAsync();
            // Arrange
            // Add an item to update.
            var properties = new Dictionary<string, object> { { "Title", "mass updatess" } };
            int itemId = await _service.AddListItemAsync(_listName, properties);

            // Prepare updated properties.
            var propertiesToUpdate = new Dictionary<string, object>
            {
                { "Title", "mass updatess 22" }
            };

            // Act
            int updatedCount = await _service.UpdateListItemsAsync(_listName, propertiesToUpdate, "", 3);
            //int updatedCount2 = await _service.UpdateListItems2Async(_listName, propertiesToUpdate, "", 3);
            //int updatedCount3 = await _service.UpdateListItems4Async(_listName, propertiesToUpdate, "", 3);

            // Assert
            Assert.True(updatedCount > 0, "At least one item should have been updated.");
        }
       
    }

}

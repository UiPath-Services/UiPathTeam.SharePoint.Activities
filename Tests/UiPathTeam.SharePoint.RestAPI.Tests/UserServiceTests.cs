using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    public class UserServiceTests : IClassFixture<ServiceFixture<SharePointUserService>>
    {
        
        private static readonly ServiceFixture<SharePointUserService> _fixture;

        static UserServiceTests()
        {
            _fixture = new ServiceFixture<SharePointUserService>();
        }
        //public SharePointListServiceTests(SharePointServiceFixture<SharePointListService> fixture)
        //{
        //    _fixture = fixture;
        //    _staticFixture = fixture; // Store the fixture in a static field
        //}

        public static IEnumerable<object[]> GetUserServices()
        {
            return _fixture.ServicesWithData.Select(swd => new object[] { swd.Service, (UserServiceTestData)swd.Data });
        }

        [Theory]
        [MemberData(nameof(GetUserServices))]
        public async Task CreateUserGroupAsync_Creates_Group_IntegrationTest(LazyService<SharePointUserService> lazyService, UserServiceTestData testData)
        {

            var _service = await lazyService.GetServiceAsync();
            // Use a unique group name to avoid collisions.
            string groupName = "IntegrationTestGroup_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string groupDescription = "Test group created by integration tests";

            // Act: Create the group.
            await _service.CreateUserGroupAsync(groupName, groupDescription);

            // If no exception was thrown, assume success.
            // Cleanup: Remove the group.
            await _service.RemoveGroupAsync(groupName);
        }

        [Theory]
        [MemberData(nameof(GetUserServices))]
        public async Task AddUserToGroupAsync_Adds_User_IntegrationTest(LazyService<SharePointUserService> lazyService, UserServiceTestData testData)
        {
            string _username = testData.UserName;
            var _service = await lazyService.GetServiceAsync();

            // Arrange: Create a new group.
            string groupName = "IntegrationTestGroup_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string groupDescription = "Test group for adding user";
            await _service.CreateUserGroupAsync(groupName, groupDescription);

            //string groupName = "IntegrationTestGroup_83ba3f00";

            //string userEmail = "fahad";

            // Act: Add the test user to the group.
            await _service.AddUserToGroupAsync(groupName, _username);

            // Assert: Retrieve users and check that our test user is present.
            var users = await _service.GetAllUsersFromGroupAsync(groupName);
            Assert.Contains(users, u => u.LoginName.IndexOf(_username, StringComparison.OrdinalIgnoreCase) >= 0 || u.Email.ToString().ToLower().Equals(_username));

            //// Cleanup: Remove the user and then the group.
            //await _service.RemoveUserFromGroupAsync(groupName, _username);
            //await _service.RemoveGroupAsync(groupName);
        }

        [Theory]
        [MemberData(nameof(GetUserServices))]
        public async Task GetAllUsersFromGroupAsync_Returns_Users_IntegrationTest(LazyService<SharePointUserService> lazyService, UserServiceTestData testData)
        {
            string _username = testData.UserName;
            var _service = await lazyService.GetServiceAsync();

            // Arrange: Create a new group and add the test user.
            string groupName = "IntegrationTestGroup_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string groupDescription = "Test group for retrieving users";
            await _service.CreateUserGroupAsync(groupName, groupDescription);
            await _service.AddUserToGroupAsync(groupName, _username);

            //string groupName = "IntegrationTestGroup_83ba3f00";

            //Act: Retrieve all users from the group.
            var users = await _service.GetAllUsersFromGroupAsync(groupName);

            //Assert: There should be at least one user.
            Assert.NotEmpty(users);

            // Cleanup.
            await _service.RemoveUserFromGroupAsync(groupName, _username);
            await _service.RemoveGroupAsync(groupName);
        }

        [Theory]
        [MemberData(nameof(GetUserServices))]
        public async Task GetUserByEmailAsync_Returns_UserDetails_IntegrationTest(LazyService<SharePointUserService> lazyService, UserServiceTestData testData)
        {
            string _userEmail = testData.UserEmailToSearch;
            var _service = await lazyService.GetServiceAsync();

            // Act: Get user details by email (using our test username).
            var result = await _service.GetUserByEmailAsync(_userEmail);

            // Assert: Verify that the returned details are not empty.
            Assert.False(string.IsNullOrEmpty(result.LoginName));
        }

        [Theory]
        [MemberData(nameof(GetUserServices))]
        public async Task RemoveUserFromGroupAsync_Removes_User_IntegrationTest(LazyService<SharePointUserService> lazyService, UserServiceTestData testData)
        {
            string _username = testData.UserName;
            var _service = await lazyService.GetServiceAsync();

            // Arrange: Create a new group and add the test user.
            string groupName = "IntegrationTestGroup_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            //string groupName = "IntegrationTestGroup _ 35b0bc0c";
            string groupDescription = "Test group for removing user";
            await _service.CreateUserGroupAsync(groupName, groupDescription);
            await _service.AddUserToGroupAsync(groupName, _username);

            // Act: Remove the user from the group.
            await _service.RemoveUserFromGroupAsync(groupName, _username);

            // Assert: Retrieve the group's users and ensure the test user is not present.
            var users = await _service.GetAllUsersFromGroupAsync(groupName);
            Assert.DoesNotContain(users, u => u.LoginName.IndexOf(_username, StringComparison.OrdinalIgnoreCase) >= 0);

            // Cleanup: Remove the group.
            await _service.RemoveGroupAsync(groupName);
        }

        [Theory]
        [MemberData(nameof(GetUserServices))]
        public async Task RemoveGroupAsync_Removes_Group_IntegrationTest(LazyService<SharePointUserService> lazyService, UserServiceTestData testData)
        {
            var _service = await lazyService.GetServiceAsync();

            //// Arrange: Create a new group.
            string groupName = "IntegrationTestGroup_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string groupDescription = "Test group for removal";
            await _service.CreateUserGroupAsync(groupName, groupDescription);
            // Act: Remove the group.
            await _service.RemoveGroupAsync(groupName);

            // If no exception was thrown, assume that the group was removed successfully.
        }




    }
}

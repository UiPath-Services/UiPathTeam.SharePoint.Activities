using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Service.Helpers;

namespace UiPathTeam.SharePoint.RestAPI.Services
{
    public class SharePointUserService : SharePointBaseService
    {

        public SharePointUserService(HttpClient httpClient, string siteUrl) : base(httpClient, siteUrl)
        {
        }
    

        #region Public Methods
        /// <summary>
        /// Ensures a user exists in SharePoint and returns the user object.
        /// Input: loginNameOrEmail (can be an email address or login name)
        /// Output: A SharePointUser with properties (Id, Title, Email, LoginName) in the proper format.
        /// </summary>
        public async Task<User> EnsureUserAsync(string loginNameOrEmail)
        {
            string formDigest = await GetFormDigestAsync();
            var requestUrl = $"{_siteUrl}/_api/web/ensureuser";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            request.Headers.Add("X-RequestDigest", formDigest);

            var payload = new { logonName = loginNameOrEmail };
            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            var dElement = doc.RootElement.GetProperty("d");
            User user = MapUserProperties(dElement);

            //var user = new User
            //{
            //    Id = dElement.GetProperty("Id").GetInt32(),
            //    Title = dElement.GetProperty("Title").GetString(),
            //    Email = dElement.TryGetProperty("Email", out JsonElement emailElem) ? emailElem.GetString() : string.Empty,
            //    LoginName = dElement.GetProperty("LoginName").GetString()
            //};

            return user;
        }

        /// <summary>
        /// Adds a user to a SharePoint group.
        /// Inputs: GroupName, User (login name, e.g. "i:0#.f|membership|user@domain.com")
        /// </summary>
        public async Task AddUserToGroupAsync(string groupName, string userIdentifier)
        {
            // Ensure the user exists and retrieve the proper login name.
            User user = await EnsureUserAsync(userIdentifier);
            string userLoginName = user.LoginName;

            string formDigest = await GetFormDigestAsync();
            var requestUrl = $"{_siteUrl}/_api/web/sitegroups/GetByName('{groupName}')/users";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            request.Headers.Add("X-RequestDigest", formDigest);

            var payload = new
            {
                __metadata = new { type = "SP.User" },
                LoginName = userLoginName
            };
            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        /// <summary>
        /// Creates a SharePoint user group.
        /// Inputs: GroupName, GroupDescription
        /// </summary>
        public async Task CreateUserGroupAsync(string groupName, string groupDescription)
        {
            string formDigest = await GetFormDigestAsync();
            var requestUrl = $"{_siteUrl}/_api/web/sitegroups";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            request.Headers.Add("X-RequestDigest", formDigest);

            var payload = new
            {
                __metadata = new { type = "SP.Group" },
                Title = groupName,
                Description = groupDescription
            };
            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        /// <summary>
        /// Gets all users from a specific SharePoint group.
        /// Inputs: GroupName
        /// Output: List of SharePointUser objects
        /// </summary>
        public async Task<List<User>> GetAllUsersFromGroupAsync(string groupName)
        {
            var requestUrl = $"{_siteUrl}/_api/web/sitegroups/GetByName('{groupName}')/users";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            SetJsonHeaders(request);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string responseJson = await response.Content.ReadAsStringAsync();

            var users = new List<User>();
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("d", out JsonElement dElement) &&
                dElement.TryGetProperty("results", out JsonElement results))
            {
                foreach (var element in results.EnumerateArray())
                {
                    User userObj = MapUserProperties(element);
                    //var userObj = new User
                    //{
                    //    Id = element.GetProperty("Id").GetInt32(),
                    //    Title = element.GetProperty("Title").GetString(),
                    //    Email = element.TryGetProperty("Email", out JsonElement emailElem) ? emailElem.GetString() : string.Empty,
                    //    LoginName = element.GetProperty("LoginName").GetString()
                    //};
                    users.Add(userObj);
                }
            }
            return users;
        }

        /// <summary>
        /// Gets a user by email.
        /// Inputs: User (email address)
        /// Outputs: Tuple containing (SharePointUser as string, UserID as string)
        /// </summary>
        public async Task<User> GetUserByEmailAsync(string email)
        {

            try
            {
                User user = await EnsureUserAsync(email);
                return user;
            }
            catch (Exception exByUser)
            {
                try
                {
                    var requestUrl = $"{_siteUrl}/_api/web/siteusers/getByEmail('{Uri.EscapeDataString(email)}')";
                    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    SetJsonHeaders(request);

                    var response = await _httpClient.SendAsync(request);
                    await response.EnsureSuccessOrThrowAsync();
                    string responseJson = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(responseJson);
                    var d = doc.RootElement.GetProperty("d");
                    User userObj = MapUserProperties(d);

                    return userObj;
                }
                catch (Exception exByEmail)
                {
                    throw exByUser;
                }
            }

        }

        public static User MapUserProperties(JsonElement d)
        {
            return new User
            {
                Id = d.GetProperty("Id").GetInt32(),
                Title = d.GetProperty("Title").GetString(),
                Email = d.TryGetProperty("Email", out JsonElement emailElem) ? emailElem.GetString() : string.Empty,
                LoginName = d.GetProperty("LoginName").GetString(),
                IsSiteAdmin = d.TryGetProperty("IsSiteAdmin", out JsonElement isSiteAdminElem) && isSiteAdminElem.GetBoolean(),
                PrincipalType = d.TryGetProperty("PrincipalType", out JsonElement principalTypeElem)
                                    ? (PrincipalType)principalTypeElem.GetInt32()
                                    : PrincipalType.None,
                UserId = d.TryGetProperty("UserId", out JsonElement userIdElem)
                                                ? new UserIdInfo
                                                {
                                                    NameId = userIdElem.TryGetProperty("NameId", out JsonElement nameIdElem)
                                                                    ? nameIdElem.GetString()
                                                                    : string.Empty,
                                                    NameIdIssuer = userIdElem.TryGetProperty("NameIdIssuer", out JsonElement nameIdIssuerElem)
                                                                    ? nameIdIssuerElem.GetString()
                                                                    : string.Empty
                                                }
                                                : null

            };
        }

        public async Task RemoveGroupAsync(string groupName)
        {

            // Retrieve the form digest.
            string formDigest = await GetFormDigestAsync();

            // Get the group's ID using its name.
            var requestUrlGet = $"{_siteUrl}/_api/web/sitegroups/GetByName('{groupName}')";
            using var requestGet = new HttpRequestMessage(HttpMethod.Get, requestUrlGet);
            SetJsonHeaders(requestGet);
            var responseGet = await _httpClient.SendAsync(requestGet);
            await responseGet.EnsureSuccessOrThrowAsync();

            string responseJson = await responseGet.Content.ReadAsStringAsync();
            using var docGet = JsonDocument.Parse(responseJson);
            int groupId = docGet.RootElement.GetProperty("d").GetProperty("Id").GetInt32();

            // Now delete the group by calling removeById.
            var requestUrlDelete = $"{_siteUrl}/_api/web/sitegroups/removeById({groupId})";
            using var requestDelete = new HttpRequestMessage(HttpMethod.Post, requestUrlDelete);
            SetJsonHeaders(requestDelete);
            requestDelete.Headers.Add("X-RequestDigest", formDigest);
            requestDelete.Content = new StringContent(string.Empty);

            var responseDelete = await _httpClient.SendAsync(requestDelete);
            await responseDelete.EnsureSuccessOrThrowAsync();
            //await EnsureSuccessOrThrowAsync(responseDelete);


        }

        /// <summary>
        /// Removes a user from a SharePoint group.
        /// Inputs: GroupName, User (login name)
        /// </summary>
        public async Task RemoveUserFromGroupAsync(string groupName, string userIdentifier)
        {
            // Ensure the user exists and retrieve the proper login name.
            User user = await EnsureUserAsync(userIdentifier);
            //string userLoginName = user.LoginName;


            string formDigest = await GetFormDigestAsync();

            //var requestUrl = $"{_siteUrl}/_api/web/sitegroups/GetByName('{groupName}')/users/removeByLoginName('{userLoginName}')";
            //var requestUrl = $"{_siteUrl}/_api/web/sitegroups/GetByName('{groupName}')/users/removeByLoginName('{Uri.EscapeDataString(userLoginName)}')";
            var requestUrl = $"{_siteUrl}/_api/web/sitegroups/GetByName('{groupName}')/users/removeById({user.Id})";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            request.Headers.Add("X-HTTP-Method", "DELETE");
            request.Headers.Add("IF-MATCH", "*");
            request.Headers.Add("X-RequestDigest", formDigest);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Sets the Accept header to "application/json;odata=verbose".
        /// </summary>
        private void SetJsonHeaders(HttpRequestMessage request)
        {
            request.Headers.Accept.Clear();
            var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");
            acceptHeader.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            request.Headers.Accept.Add(acceptHeader);
        }

        /// <summary>
        /// Retrieves the form digest value from the contextinfo endpoint.
        /// </summary>
        private async Task<string> GetFormDigestAsync()
        {
            var requestUrl = $"{_siteUrl}/_api/contextinfo";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            request.Content = new StringContent(string.Empty);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string responseJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            string formDigest = doc.RootElement
                                   .GetProperty("d")
                                   .GetProperty("GetContextWebInformation")
                                   .GetProperty("FormDigestValue")
                                   .GetString();
            return formDigest;
        }

        #endregion
    }

    /// <summary>
    /// Represents a SharePoint user.
    /// </summary>
    //public class SharePointUser
    //{
    //    public int Id { get; set; }
    //    public string Title { get; set; }
    //    public string Email { get; set; }
    //    public string LoginName { get; set; }
    //}
}

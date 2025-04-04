using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Service;
using UiPathTeam.SharePoint.Service.Helpers;

namespace UiPathTeam.SharePoint.RestAPI.Services
{
    public class SharePointPermissionService : SharePointBaseService
    {
        public SharePointPermissionService(HttpClient httpClient, string siteUrl) : base(httpClient, siteUrl)
        {
        }

        #region Public Permission Methods

        /// <summary>
        /// 1) AddPermission  
        /// Inputs: ListName, Receiver, IsUser, PermissionToGive, FolderPath, ListType  
        /// This function ensures the folder’s permissions are breaked from inheritance,
        /// then retrieves the principal ID for the receiver and the role definition ID
        /// corresponding to the desired permission, and finally adds the role assignment.
        /// </summary>
        public async Task AddPermissionAsync(string listName, string receiver, bool isUser, RoleType permissionToGive, string folderPath, ListType listType)
        {
            // Determine the target endpoint based on inputs.
            string targetEndpoint = null;
            if (string.IsNullOrEmpty(listName))
            {
                // Site-level permissions
                targetEndpoint = $"{_siteUrl}/_api/web";
            }
            else if (string.IsNullOrEmpty(folderPath))
            {
                // List-level permissions
                targetEndpoint = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')";
                // Break role inheritance on the list so that permissions can be uniquely assigned.
                await BreakListInheritanceAsync(listName);
            }
            else
            {
                // Folder-level permissions (permissions are assigned to the underlying list item).
                targetEndpoint = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{folderPath}')/ListItemAllFields";
                // Break inheritance on the folder if necessary.
                await BreakRoleInheritanceAsync(folderPath);
            }

            // Resolve the principal (user or group) and get its ID.
            int principalId = await GetPrincipalIdAsync(receiver, isUser);

            // Map the RoleType to a role definition ID.
            int roleDefId = await GetRoleDefinitionIdAsync(permissionToGive);

            // Build the endpoint to add the role assignment.
            var requestUrl = $"{targetEndpoint}/roleassignments/addroleassignment(principalid={principalId}, roledefid={roleDefId})";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        /// <summary>
        /// 2) GetAllPermissions  
        /// Inputs: ListName, FolderPath, ListType  
        /// Output: List of Tuple&lt;string, string&gt; where the first element is the full login name (or group name)
        /// and the second element is the permission level.
        /// </summary>
        public async Task<List<Tuple<string, string>>> GetAllPermissionsAsync(string listName, string folderPath, ListType listType)
        {
            // Determine the target endpoint based on the inputs.
            string targetEndpoint;
            if (string.IsNullOrEmpty(listName))
            {
                // Site-level permissions.
                targetEndpoint = $"{_siteUrl}/_api/web";
            }
            else if (string.IsNullOrEmpty(folderPath))
            {
                // List-level permissions.
                targetEndpoint = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')";
            }
            else
            {
                // Folder-level permissions (using the folder's underlying list item).
                targetEndpoint = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{folderPath}')/ListItemAllFields";
            }

            // Query the role assignments on the target endpoint.
            var requestUrl = $"{targetEndpoint}/roleassignments?$expand=Member,RoleDefinitionBindings";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            SetJsonHeaders(request);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string json = await response.Content.ReadAsStringAsync();

            var result = new List<Tuple<string, string>>();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("d", out JsonElement dElement) &&
                dElement.TryGetProperty("results", out JsonElement results))
            {
                // If results is an array, enumerate through it.
                if (results.ValueKind == JsonValueKind.Array)
                {
                    foreach (var assignment in results.EnumerateArray())
                    {
                        string principal = assignment.GetProperty("Member").GetProperty("LoginName").GetString();
                        string permissionLevel = ExtractPermissionLevel(assignment);
                        result.Add(new Tuple<string, string>(principal, permissionLevel));
                    }
                }
                // If results is a single object, process it as one assignment.
                else if (results.ValueKind == JsonValueKind.Object)
                {
                    string principal = results.GetProperty("Member").GetProperty("LoginName").GetString();
                    string permissionLevel = ExtractPermissionLevel(results);
                    result.Add(new Tuple<string, string>(principal, permissionLevel));
                }
            }
            return result;
        }

        /// <summary>
        /// 3) RemovePermission  
        /// Inputs: ListName, FolderPath, ListType, Receiver, IsUser  
        /// Removes a role assignment from the folder.
        /// </summary>
        public async Task RemovePermissionAsync(string listName, string folderPath, ListType listType, string receiver, bool isUser)
        {
            // First, get the principal id.
            int principalId = await GetPrincipalIdAsync(receiver, isUser);

            // Determine the target endpoint based on the inputs.
            string targetEndpoint;
            if (string.IsNullOrEmpty(listName))
            {
                // Remove permission at site level.
                targetEndpoint = $"{_siteUrl}/_api/web";
            }
            else if (string.IsNullOrEmpty(folderPath))
            {
                // Remove permission at list level.
                targetEndpoint = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')";
            }
            else
            {
                // Remove permission at folder level (using the folder's underlying list item).
                targetEndpoint = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{folderPath}')/ListItemAllFields";
            }

            // Build the URL to remove the role assignment.
            var requestUrl = $"{targetEndpoint}/roleassignments/removeroleassignment(principalid={principalId})";

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            // Provide an empty content body.
            request.Content = new StringContent(string.Empty);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Constructs the endpoint URL for a folder's underlying list item.
        /// </summary>
        /// <param name="folderPath">The server-relative URL of the folder.</param>
        private string GetFolderItemEndpoint(string folderPath)
        {
            // e.g. "/sites/YourSite/Shared Documents/FolderName"
            return $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{folderPath}')/ListItemAllFields";
        }

        /// <summary>
        /// Retrieves the principal ID for a user or group.
        /// For a user, it calls the siteusers endpoint; for a group, the sitegroups endpoint.
        /// </summary>
        private async Task<int> GetPrincipalIdAsync(string receiver, bool isUser)
        {
            if (isUser)
            {
                // Use EnsureUserAsync from SharePointUserService to resolve the user.
                // You can either instantiate a new SharePointUserService (since it uses the same HttpClient and _siteUrl)
                // or inject it as a dependency. Here, we instantiate a new one for simplicity.
                var userService = new SharePointUserService(_httpClient, _siteUrl);
                var user = await userService.EnsureUserAsync(receiver);
                return user.Id;
            }
            else
            {
                // For groups, retrieve the group by its name.
                var requestUrl = $"{_siteUrl}/_api/web/sitegroups/GetByName('{receiver}')";
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                SetJsonHeaders(request);
                var response = await _httpClient.SendAsync(request);
                await response.EnsureSuccessOrThrowAsync();
                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                int id = doc.RootElement.GetProperty("d").GetProperty("Id").GetInt32();
                return id;
            }
        }

        /// <summary>
        /// Maps a RoleType to a role definition name and retrieves its ID.
        /// </summary>
        private async Task<int> GetRoleDefinitionIdAsync(RoleType role)
        {
            // Map RoleType to a SharePoint role definition name.
            string roleName;
            switch (role)
            {
                case RoleType.Reader:
                    roleName = "Read";
                    break;
                case RoleType.Contributor:
                    roleName = "Contribute";
                    break;
                case RoleType.WebDesigner:
                    roleName = "Design";
                    break;
                case RoleType.Administrator:
                    roleName = "Full Control";
                    break;
                case RoleType.Editor:
                    roleName = "Edit";
                    break;
                case RoleType.Guest:
                    roleName = "Limited Access"; // Adjust if necessary
                    break;
                default:
                    throw new ArgumentException("Invalid role type");
            }

            var requestUrl = $"{_siteUrl}/_api/web/roledefinitions?$filter=Name eq '{roleName}'";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            SetJsonHeaders(request);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            JsonElement results = doc.RootElement.GetProperty("d").GetProperty("results");
            if (results.GetArrayLength() > 0)
            {
                int roleDefId = results[0].GetProperty("Id").GetInt32();
                return roleDefId;
            }
            else
            {
                throw new Exception("Role definition not found");
            }
            }

        /// <summary>
        /// Breaks the role inheritance on a folder's list item so that permissions can be assigned uniquely.
        /// </summary>
        private async Task BreakRoleInheritanceAsync(string folderPath)
        {
            var endpoint = GetFolderItemEndpoint(folderPath);
            var requestUrl = $"{endpoint}/breakroleinheritance(copyRoleAssignments=false, clearSubscopes=true)";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        /// <summary>
        /// Retrieves the form digest value needed for POST/DELETE operations.
        /// </summary>
        private async Task<string> GetFormDigestAsync()
        {
            var requestUrl = $"{_siteUrl}/_api/contextinfo";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            request.Content = new StringContent(string.Empty);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            string formDigest = doc.RootElement
                                   .GetProperty("d")
                                   .GetProperty("GetContextWebInformation")
                                   .GetProperty("FormDigestValue")
                                   .GetString();
            return formDigest;
        }

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
        /// Breaks role inheritance on a list so that permissions can be uniquely assigned.
        /// </summary>
        private async Task BreakListInheritanceAsync(string listName)
        {
            var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/breakroleinheritance(copyRoleAssignments=false, clearSubscopes=true)";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        /// <summary>
        /// Helper method to extract the permission level name from a role assignment.
        /// It handles the case where RoleDefinitionBindings is an object with a "results" property.
        /// </summary>
        private string ExtractPermissionLevel(JsonElement assignment)
        {
            string permissionLevel = "";
            if (assignment.TryGetProperty("RoleDefinitionBindings", out JsonElement roleBindings))
            {
                // Check if roleBindings is an object that contains a "results" array.
                if (roleBindings.ValueKind == JsonValueKind.Object &&
                    roleBindings.TryGetProperty("results", out JsonElement rbResults) &&
                    rbResults.ValueKind == JsonValueKind.Array &&
                    rbResults.GetArrayLength() > 0)
                {
                    permissionLevel = rbResults[0].GetProperty("Name").GetString();
                }
                // Alternatively, if it's already an array.
                else if (roleBindings.ValueKind == JsonValueKind.Array && roleBindings.GetArrayLength() > 0)
                {
                    permissionLevel = roleBindings[0].GetProperty("Name").GetString();
                }
            }
            return permissionLevel;
        }
        #endregion
    }
    public enum RoleType
    {
        None,
        Guest,
        Reader,
        Contributor,
        WebDesigner,
        Administrator,
        Editor
    }

    public enum ListType
    {
        List,
        Library
    }
}

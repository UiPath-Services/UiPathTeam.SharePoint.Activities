
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Service.Helpers;

namespace UiPathTeam.SharePoint.RestAPI.Services
{
    public class SharePointLibraryService : SharePointBaseService
    {
        //private readonly HttpClient _httpClient;
        //private readonly string _siteUrl;

        //public SharePointLibraryService(HttpClient httpClient, string siteUrl)
        //{
        //    _httpClient = httpClient;
        //    _siteUrl = siteUrl.TrimEnd('/');
        //}
        public SharePointLibraryService(HttpClient httpClient, string siteUrl) : base(httpClient, siteUrl)
        {
        }
        #region File CheckIn/CheckOut

        /// <summary>
        /// 1) CheckInFile: Checks in a file at the specified relative URL.
        /// Example URL: "/sites/YourSite/Shared Documents/YourFile.docx"
        /// </summary>
        public async Task CheckInFileAsync(string relativeUrl, string comment = "Checked in via API", int checkInType = 1)
        {
            
            var requestUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')/CheckIn(comment='{Uri.EscapeDataString(comment)}', checkintype={checkInType})";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);

            try
            {
                var response = await _httpClient.SendAsync(request);
                await response.EnsureSuccessOrThrowAsync();
            }
            catch (Exception ex)
            {
                // If the error indicates the file isn't checked out, then ignore it.
                if (ex.Message.IndexOf("is not checked out", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine("CheckInFileAsync: File is not checked out; ignoring check-in call.");
                }
                else
                {
                    throw;
                }
            }


        }


        /// <summary>
        /// 2) CheckOutFile: Checks out a file at the specified relative URL.
        /// </summary>
        public async Task CheckOutFileAsync(string relativeUrl)
        {
            try
            {
                var requestUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')/CheckOut()";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                SetJsonHeaders(request);
                string formDigest = await GetFormDigestAsync();
                request.Headers.Add("X-RequestDigest", formDigest);
                request.Content = new StringContent(string.Empty);
                var response = await _httpClient.SendAsync(request);
                await response.EnsureSuccessOrThrowAsync();
            }
            catch (Exception ex)
            {
                // If the error message indicates the file is not checked out,
                // this may be due to the library settings (e.g. check-out is not enabled).
                // In that case, log and ignore the error.
                if (ex.Message.Contains("is not checked out", StringComparison.OrdinalIgnoreCase))
                {
                    // Log the error if needed, then ignore.
                    Console.WriteLine($"CheckOutFileAsync: Ignoring error as check-out is not supported: {ex.Message}");

                }
                else if (ex.Message.Contains("is checked out for editing by", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"CheckOutFileAsync: File already checked out; ignoring error: {ex.Message}");
                }
                else
                {
                    throw;
                }
            }
        }


        /// <summary>
        /// 6) DiscardCheckout: Discards the checkout on a file at the specified relative URL.
        /// </summary>
        public async Task DiscardCheckoutAsync(string relativeUrl)
        {
            var requestUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')/UndoCheckOut()";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        #endregion

        #region Create/Upload/Delete/Rename/Move Files and Folders



        /// <summary>
        /// 4) CreateFolder: Creates a new folder in the specified library.
        /// The RelativeUrl should be the server-relative path of the new folder.
        /// Example: LibraryName: "Shared Documents", RelativeUrl: "/Shared Documents/NewFolder"
        /// </summary>
        public async Task CreateFolderAsync(string libraryName, string relativeUrl)
        {
            string libraryRelativeUrl = await GetLibraryRootFolderRelativeUrl(libraryName);

            // Step 2: Construct the new folder's ServerRelativeUrl using the retrieved library URL.
            // For example: "/sites/YourSite/Documents/NewFolder"
            var newFolderServerRelativeUrl = $"{libraryRelativeUrl.TrimEnd("/".ToCharArray())}/{relativeUrl.TrimStart("/".ToCharArray())}";

            // For folder creation, call the /folders endpoint.
            var requestUrl = $"{_siteUrl}/_api/web/folders";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);

            // Include __metadata with the type SP.Folder in the payload.
            var payload = new
            {
                __metadata = new { type = "SP.Folder" },
                ServerRelativeUrl = newFolderServerRelativeUrl
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }

        public async Task<string> GetLibraryRootFolderRelativeUrl(string libraryName)
        {

            var listUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{libraryName}')/RootFolder";
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
            SetJsonHeaders(getRequest);

            var getResponse = await _httpClient.SendAsync(getRequest);
            if (getResponse.IsSuccessStatusCode)
            {
                string jsonResult = await getResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResult);
                string libraryRelativeUrl = doc.RootElement
                                                .GetProperty("d")
                                                .GetProperty("ServerRelativeUrl")
                                                .GetString();
                return libraryRelativeUrl;
            }
            else
            {
                // If the library by title wasn't found, try to retrieve it as a folder.
                var folderUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{libraryName}')?$select=ServerRelativeUrl";
                using var folderRequest = new HttpRequestMessage(HttpMethod.Get, folderUrl);
                SetJsonHeaders(folderRequest);

                var folderResponse = await _httpClient.SendAsync(folderRequest);
                if (!folderResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Library or Folder '{libraryName}' not found or could not be retrieved.");
                }

                string folderJsonResult = await folderResponse.Content.ReadAsStringAsync();
                using var folderDoc = JsonDocument.Parse(folderJsonResult);
                string folderRelativeUrl = folderDoc.RootElement
                                                .GetProperty("d")
                                                .GetProperty("ServerRelativeUrl")
                                                .GetString();
                return folderRelativeUrl;
            }

            //var listUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{libraryName}')/RootFolder";
            //using var getRequest = new HttpRequestMessage(HttpMethod.Get, listUrl);
            //SetJsonHeaders(getRequest);

            //var getResponse = await _httpClient.SendAsync(getRequest);
            //if (!getResponse.IsSuccessStatusCode)
            //{
            //    // The library might not exist. Throw an exception or handle as needed.
            //    throw new Exception($"Library '{libraryName}' not found or could not be retrieved.");
            //}

            //string jsonResult = await getResponse.Content.ReadAsStringAsync();
            //using var doc = JsonDocument.Parse(jsonResult);
            //// Assuming the response JSON is in the form:
            //// { "d": { "ServerRelativeUrl": "/sites/YourSite/Documents", ... } }
            //string libraryRelativeUrl = doc.RootElement
            //                                .GetProperty("d")
            //                                .GetProperty("ServerRelativeUrl")
            //                                .GetString();
            //return libraryRelativeUrl;

        }

        /// <summary>
        /// 5) Delete: Deletes a file or folder at the specified relative URL (relative to the library).
        /// If the file/folder is an ASPX file and AllowOperationsOnASPXFiles is false, an exception is thrown.
        /// </summary>
        public async Task DeleteAsync(string libraryName, string relativeUrl, bool allowOperationsOnASPXFiles)
        {
            string libraryRelativeUrl = await GetLibraryRootFolderRelativeUrl(libraryName);
            var aRelativeUrl = $"{libraryRelativeUrl.TrimEnd("/".ToCharArray())}/{relativeUrl.TrimStart("/".ToCharArray())}";
            // If not allowed and file appears to be an ASPX file, throw.
            if (!allowOperationsOnASPXFiles && aRelativeUrl.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Operations on ASPX files are not allowed.");
            }

            // Try deleting as a file.
            string formDigest = await GetFormDigestAsync();
            var fileUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{aRelativeUrl}')";
            //var fileUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')";
            using var request = new HttpRequestMessage(HttpMethod.Post, fileUrl);
            SetJsonHeaders(request);
            request.Headers.Add("X-HTTP-Method", "DELETE");
            request.Headers.Add("IF-MATCH", "*");
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                // If file deletion fails, try deleting as folder.
                var folderUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{aRelativeUrl}')";
                using var folderRequest = new HttpRequestMessage(HttpMethod.Post, folderUrl);
                SetJsonHeaders(folderRequest);
                folderRequest.Headers.Add("X-HTTP-Method", "DELETE");
                folderRequest.Headers.Add("IF-MATCH", "*");
                folderRequest.Headers.Add("X-RequestDigest", formDigest);
                folderRequest.Content = new StringContent(string.Empty);
                var folderResponse = await _httpClient.SendAsync(folderRequest);
                await folderResponse.EnsureSuccessOrThrowAsync();
            }
            else
            {
                await response.EnsureSuccessOrThrowAsync();
            }
        }

        /// <summary>
        /// 7) GetChildrenNames: Returns an array of the direct children names (both folders and files) of the specified folder.
        /// </summary>
        public async Task<string[]> GetChildrenNamesAsync(string relativeUrl)
        {
            // Query the folder and expand Folders and Files.
            var requestUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{relativeUrl}')?$expand=Folders,Files";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            SetJsonHeaders(request);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string json = await response.Content.ReadAsStringAsync();
            var children = new List<string>();
            using var doc = JsonDocument.Parse(json);
            var dElement = doc.RootElement.GetProperty("d");

            if (dElement.TryGetProperty("Folders", out JsonElement folders) &&
                folders.TryGetProperty("results", out JsonElement folderResults))
            {
                foreach (var folder in folderResults.EnumerateArray())
                {
                    children.Add(folder.GetProperty("Name").GetString());
                }
            }
            if (dElement.TryGetProperty("Files", out JsonElement files) &&
                files.TryGetProperty("results", out JsonElement fileResults))
            {
                foreach (var file in fileResults.EnumerateArray())
                {
                    children.Add(file.GetProperty("Name").GetString());
                }
            }
            return children.ToArray();
        }

        /// <summary>
        /// 8) GetFile: Downloads a file from the specified relative URL and saves it to the given local path.
        /// If LocalPath is empty, the file is saved in the current project's root folder.
        /// </summary>
        public async Task GetFileAsync(string localPath, string relativeUrl)
        {
            var requestUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')/$value";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

            // If localPath is empty, use current directory.
            if (string.IsNullOrEmpty(localPath))
            {
                localPath = Directory.GetCurrentDirectory();
            }

            string fullPath;
            // If localPath has an extension or if the directory does not exist, assume it's a full file path.
            if (Path.HasExtension(localPath) || !Directory.Exists(localPath))
            {
                fullPath = localPath;
            }
            else
            {
                // Otherwise, combine localPath with the file name from the relative URL.
                string fileName = Path.GetFileName(relativeUrl);
                fullPath = Path.Combine(localPath, fileName);
            }

            // Ensure the directory exists.
            string directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(fullPath, fileBytes);
        }


        /// <summary>
        /// 9) MoveItem: Moves a file or folder from one relative URL to another.
        /// </summary>
        //public async Task MoveItemAsync(string relativeUrl, string destinationRelativeUrl, bool allowOverwrite, string PlatformType)
        public async Task MoveItemOnlineAsync(string relativeUrl, string destinationRelativeUrl, bool allowOverwrite)
        {
            bool isFile = await FileExistsAsync(relativeUrl);
            if (isFile)
            {


                // If destination is a folder, append the file name.
                if ((!Path.GetFileName(destinationRelativeUrl).Contains(".")))
                {
                    destinationRelativeUrl = $"{destinationRelativeUrl.TrimEnd('/')}/{Path.GetFileName(relativeUrl)}";
                }

                await MoveFileAsync(_siteUrl, relativeUrl, destinationRelativeUrl, allowOverwrite);
                
            }
            else
            {
                await MoveFolderAsync(_siteUrl, relativeUrl, destinationRelativeUrl);
            }


        }

        public async Task MoveFileAsync(string siteUrl, string sourceServerRelUrl, string destServerRelUrl, bool overwrite)
        {
            // Construct the REST endpoint URL
            string overwriteParam = overwrite ? "true" : "false";
            string endpoint = $"{siteUrl}/_api/SP.MoveCopyUtil.MoveFileByPath(overwrite=@o)?@o={overwriteParam}";

            Uri uri = new Uri(_siteUrl);
            string baseServerUrl = $"{uri.Scheme}://{uri.Host}";
            //sourceServerRelUrl = 
            // Prepare the JSON body
            var body = new
            {
                srcPath = new
                {
                    //__metadata = new { type = "SP.ResourcePath" },
                    DecodedUrl = $"{baseServerUrl}{sourceServerRelUrl}"
                },
                destPath = new
                {
                    DecodedUrl = $"{baseServerUrl}{destServerRelUrl}"
                }
                // No need for options here; overwrite is handled via the query param
            };
            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            // Create the request (assumes you've added Authorization and X-RequestDigest headers to HttpClient)
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            // (Handle the response as needed)
        }

        public async Task MoveFolderAsync(string siteUrl, string sourceFolderServerRelUrl, string destFolderServerRelUrl)
        {
            string endpoint = $"{siteUrl}/_api/SP.MoveCopyUtil.MoveFolderByPath()";

            // For folder moves, prepare MoveCopyOptions (e.g., KeepBoth = false by default)
            var body = new
            {
                srcPath = new
                {
                    __metadata = new { type = "SP.ResourcePath" },
                    DecodedUrl = $"{siteUrl}{sourceFolderServerRelUrl}"
                },
                destPath = new
                {
                    __metadata = new { type = "SP.ResourcePath" },
                    DecodedUrl = $"{siteUrl}{destFolderServerRelUrl}"
                },
                options = new
                {
                    __metadata = new { type = "SP.MoveCopyOptions" },
                    KeepBoth = false  // do not keep both; fail if conflict (you can adjust this as needed)
                }
            };
            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(body);

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            SetJsonHeaders(request);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            //response.EnsureSuccessStatusCode();
        }

        private string EscapeSpUrl(string url)
        {
            return url
                .Replace("'", "''")  // Escape single quotes
                .Replace(" ", "%20") // Escape spaces if needed
                .Replace("#", "%23"); // Escape special characters if needed
        }

        public async Task MoveItemAsync(string relativeUrl, string destinationRelativeUrl, bool allowOverwrite)
        {
            // Check if the relativeUrl points to a file.
            int flags = allowOverwrite ? 1 : 0;
            bool isFile = await FileExistsAsync(relativeUrl);
            string requestUrl;

            if (isFile)
            {
                // Handle file move
                if (!Path.HasExtension(destinationRelativeUrl))
                {
                    destinationRelativeUrl = $"{destinationRelativeUrl.TrimEnd('/')}/{Path.GetFileName(relativeUrl)}";
                }

                requestUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl(@src)/MoveTo(newurl=@dst,flags={flags})"
                    + $"?@src='{EscapeSpUrl(Uri.EscapeDataString(relativeUrl))}'"
                    + $"&@dst='{EscapeSpUrl(Uri.EscapeDataString(destinationRelativeUrl))}'";
            }
            else
            {
                destinationRelativeUrl = $"{destinationRelativeUrl.TrimEnd('/')}/{Path.GetFileName(relativeUrl)}";
                // Handle folder move
                //requestUrl = $"{_siteUrl}/_api/web/getfolderbyserverrelativeurl('{EscapeSpUrl(relativeUrl)}')/movetousingpath";
                requestUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl(@src)/MoveTo(newurl=@dst)"
                    + $"?@src='{EscapeSpUrl(Uri.EscapeDataString(relativeUrl))}'"
                    + $"&@dst='{EscapeSpUrl(Uri.EscapeDataString(destinationRelativeUrl))}'";

            }

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Accept.Clear();
            var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");
            acceptHeader.Parameters.Add(new NameValueHeaderValue("odata", "nometadata"));
            request.Headers.Accept.Add(acceptHeader);

            // Required headers
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();

        }

        public async Task MoveItemAsyncOld(string relativeUrl, string destinationRelativeUrl, bool allowOverwrite)
        {
            int flags = allowOverwrite ? 1 : 0;
            string requestUrl = string.Empty;

            // Check if the relativeUrl points to a file.
            bool isFile = await FileExistsAsync(relativeUrl);
            if (isFile)
            {


                // If destination is a folder, append the file name.
                if ((!Path.GetFileName(destinationRelativeUrl).Contains(".")))
                {
                    destinationRelativeUrl = $"{destinationRelativeUrl.TrimEnd('/')}/{Path.GetFileName(relativeUrl)}";
                }

                requestUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')/moveto(newurl='{destinationRelativeUrl}', flags={flags})";
            }
            else
            {
                // For a folder, omit the flags parameter.
                requestUrl = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{relativeUrl}')/moveto(newurl='{destinationRelativeUrl}')";
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new StringContent(string.Empty);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
        }


        /// <summary>
        /// 10) RenameItem: Renames a file or folder at the specified relative URL.
        /// This is implemented by moving the item within the same folder with a new name.
        /// </summary>
        public async Task RenameItemAsync(string relativeUrl, string newName)
        {
            // Extract the folder path from the relative URL.
            string folderPath = Path.GetDirectoryName(relativeUrl).Replace("\\", "/");
            string newUrl = $"{folderPath}/{newName}";
            // Use MoveItem with allowOverwrite true.
            await MoveItemAsync(relativeUrl, newUrl, true);
        }

        /// <summary>
        /// 11) UploadFile: Uploads a file from a local path to the specified relative URL.
        /// After uploading, it updates the file's properties if provided.
        /// </summary>
        public async Task UploadFileAsync(
    string relativeUrl,
    string localPath,
    Dictionary<string, object> propertiesToAdd,
    bool allowOperationsOnASPXFiles,
    bool allowOverwrite,
    bool checkOutFileBeforeOverwrite,
    bool checkInFileAfterCreation)
        {
            // Check if file is an ASPX file.
            if (!allowOperationsOnASPXFiles && relativeUrl.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Operations on ASPX files are not allowed.");
            }

            // Determine folder and file name.
            (string folderPath, string fileName) = SplitFilePath(relativeUrl);
            // Encode folder path to handle spaces/special characters.
            // Replace spaces with %20 in folder path and file name.
            // Properly encode folder path and file name.
            string encodedFolderPath = Uri.EscapeDataString(folderPath);
            string encodedFileName = Uri.EscapeDataString(fileName);

            //var folderEndpoint = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{encodedFolderPath}')";
            var folderEndpoint = $"{_siteUrl.TrimEnd('/')}/_api/web/GetFolderByServerRelativeUrl('{encodedFolderPath.TrimStart('/')}')";

            //var folderEndpoint = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{folderPath}')";
            string formDigest = await GetFormDigestAsync();

            // Check if file exists.
            bool fileExists = await FileExistsAsync(relativeUrl);
            if (fileExists)
            {
                if (!allowOverwrite)
                    throw new Exception("File already exists and overwrite is not allowed.");
                if (checkOutFileBeforeOverwrite)
                {
                    await CheckOutFileAsync(relativeUrl);
                }
                // Delete the file before uploading.
                await DeleteAsync("", relativeUrl, allowOperationsOnASPXFiles);
            }

            // Upload the file.
            byte[] fileBytes = await File.ReadAllBytesAsync(localPath);
            //var requestUrl = $"{folderEndpoint}/Files/add(overwrite={(allowOverwrite ? "true" : "false")}, url='{encodedFileName}')";
            var requestUrl = $"{folderEndpoint}/Files/add(overwrite={allowOverwrite.ToString().ToLower()}, url='{encodedFileName}')";
            //var requestUrl = $"{folderEndpoint}/Files/add(overwrite={(allowOverwrite ? "true" : "false")}, url='{fileName}')";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);
            request.Headers.Add("X-RequestDigest", formDigest);
            request.Content = new ByteArrayContent(fileBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();

            // Update file properties if provided.
            if (propertiesToAdd != null && propertiesToAdd.Count > 0)
            {
                var listItemUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')/ListItemAllFields";
                using var updateRequest = new HttpRequestMessage(HttpMethod.Post, listItemUrl);
                SetJsonHeaders(updateRequest);
                updateRequest.Headers.Add("X-HTTP-Method", "MERGE");
                updateRequest.Headers.Add("IF-MATCH", "*");
                updateRequest.Headers.Add("X-RequestDigest", formDigest);
                string jsonProps = JsonSerializer.Serialize(propertiesToAdd);
                var updateContent = new StringContent(jsonProps, Encoding.UTF8, "application/json");
                updateContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
                updateRequest.Content = updateContent;
                var updateResponse = await _httpClient.SendAsync(updateRequest);
                await updateResponse.EnsureSuccessOrThrowAsync();
            }

            // Check in file if requested.
            if (checkInFileAfterCreation)
            {
                await CheckInFileAsync(relativeUrl, "File uploaded via API", 1);
            }
        }


        /// <summary>
        /// 12) UploadLargeFile: Uploads a large file in chunks.
        /// </summary>
        public async Task UploadLargeFileAsync(
    string relativeUrl,
    string localPath,
    Dictionary<string, object> propertiesToAdd,
    bool allowOperationsOnASPXFiles,
    bool allowOverwrite,
    int fileChunkSizeInMB = 10,
    bool checkOutFileBeforeOverwrite = false,
    bool checkInFileAfterCreation = false)
        {
            if (!allowOperationsOnASPXFiles && relativeUrl.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Operations on ASPX files are not allowed.");
            }
            bool fileExists = await FileExistsAsync(relativeUrl);
            string formDigest = await GetFormDigestAsync();
            if (fileExists)
            {
                if (!allowOverwrite)
                    throw new Exception("File already exists and overwrite is not allowed.");
                if (checkOutFileBeforeOverwrite)
                {
                    await CheckOutFileAsync(relativeUrl);
                }
                await DeleteAsync("", relativeUrl, allowOperationsOnASPXFiles);
            }
            byte[] fileBytes = await File.ReadAllBytesAsync(localPath);
            int fileSize = fileBytes.Length;
            int chunkSize = fileChunkSizeInMB * 1024 * 1024;
            int offset = 0;
            (string folderPath, string fileName) = SplitFilePath(relativeUrl);

            // Generate a single upload ID for the entire session.
            Guid uploadId = Guid.NewGuid();

            // Start the upload session by creating the file if it doesn't exist.
            var folderEndpoint = $"{_siteUrl}/_api/web/GetFolderByServerRelativeUrl('{folderPath.Replace(" ", "%20")}')";
            var startUploadUrl = $"{folderEndpoint}/Files/add(overwrite={(allowOverwrite ? "true" : "false")}, url='{fileName.Replace(" ", "%20")}')";
            using (var startRequest = new HttpRequestMessage(HttpMethod.Post, startUploadUrl))
            {
                SetJsonHeaders(startRequest);
                startRequest.Headers.Add("X-RequestDigest", formDigest);
                // Create the file with an empty payload.
                startRequest.Content = new ByteArrayContent(new byte[0]);
                var startResponse = await _httpClient.SendAsync(startRequest);
                await startResponse.EnsureSuccessOrThrowAsync();
            }

            // Now upload the file in chunks using the same uploadId.
            var fileEndpoint = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl.Replace(" ", "%20")}')";
            while (offset < fileSize)
            {
                int bytesToSend = Math.Min(chunkSize, fileSize - offset);
                byte[] chunk = new byte[bytesToSend];
                Array.Copy(fileBytes, offset, chunk, 0, bytesToSend);
                HttpResponseMessage chunkResponse;
                if (offset == 0)
                {
                    // First block: StartUpload.
                    var firstBlockUrl = $"{fileEndpoint}/StartUpload(uploadId=guid'{uploadId}')";
                    using var firstRequest = new HttpRequestMessage(HttpMethod.Post, firstBlockUrl);
                    SetJsonHeaders(firstRequest);
                    firstRequest.Headers.Add("X-RequestDigest", formDigest);
                    firstRequest.Content = new ByteArrayContent(chunk);
                    chunkResponse = await _httpClient.SendAsync(firstRequest);
                }
                else if (offset + bytesToSend < fileSize)
                {
                    // Intermediate blocks: ContinueUpload.
                    var continueUrl = $"{fileEndpoint}/ContinueUpload(uploadId=guid'{uploadId}', fileOffset={offset})";
                    using var continueRequest = new HttpRequestMessage(HttpMethod.Post, continueUrl);
                    SetJsonHeaders(continueRequest);
                    continueRequest.Headers.Add("X-RequestDigest", formDigest);
                    continueRequest.Content = new ByteArrayContent(chunk);
                    chunkResponse = await _httpClient.SendAsync(continueRequest);
                }
                else
                {
                    // Final block: FinishUpload.
                    var finishUrl = $"{fileEndpoint}/FinishUpload(uploadId=guid'{uploadId}', fileOffset={offset})";
                    using var finishRequest = new HttpRequestMessage(HttpMethod.Post, finishUrl);
                    SetJsonHeaders(finishRequest);
                    finishRequest.Headers.Add("X-RequestDigest", formDigest);
                    finishRequest.Content = new ByteArrayContent(chunk);
                    chunkResponse = await _httpClient.SendAsync(finishRequest);
                }
                await chunkResponse.EnsureSuccessOrThrowAsync();
                offset += bytesToSend;
            }

            // After upload, update file properties if provided.
            if (propertiesToAdd != null && propertiesToAdd.Count > 0)
            {
                var listItemUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl.Replace(" ", "%20")}')/ListItemAllFields";
                using var updateRequest = new HttpRequestMessage(HttpMethod.Post, listItemUrl);
                SetJsonHeaders(updateRequest);
                updateRequest.Headers.Add("X-HTTP-Method", "MERGE");
                updateRequest.Headers.Add("IF-MATCH", "*");
                updateRequest.Headers.Add("X-RequestDigest", formDigest);
                string jsonProps = JsonSerializer.Serialize(propertiesToAdd);
                var updateContent = new StringContent(jsonProps, Encoding.UTF8, "application/json");
                updateContent.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
                updateRequest.Content = updateContent;
                var updateResponse = await _httpClient.SendAsync(updateRequest);
                await updateResponse.EnsureSuccessOrThrowAsync();
            }

            if (checkInFileAfterCreation)
            {
                await CheckInFileAsync(relativeUrl, "Large file uploaded via API", 1);
            }
        }



        #endregion

        #region Helper Methods

        /// <summary>
        /// Splits a server-relative file URL into folder and file name.
        /// Example: "/sites/MySite/Shared Documents/NewFile.docx" 
        /// returns ("/sites/MySite/Shared Documents", "NewFile.docx")
        /// </summary>
        private (string folderPath, string fileName) SplitFilePath(string relativeUrl)
        {
            // Ensure forward slashes.
            relativeUrl = relativeUrl.Replace("\\", "/");
            int lastSlash = relativeUrl.LastIndexOf('/');
            if (lastSlash < 0)
                throw new Exception("Invalid file URL.");
            string folderPath = relativeUrl.Substring(0, lastSlash);
            string fileName = relativeUrl.Substring(lastSlash + 1);
            return (folderPath, fileName);
        }

        /// <summary>
        /// Checks if a file exists at the specified relative URL.
        /// </summary>
        public async Task<bool> FileExistsAsync(string relativeUrl)
        {
            var requestUrl = $"{_siteUrl}/_api/web/GetFileByServerRelativeUrl('{relativeUrl}')";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            SetJsonHeaders(request);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
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
            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            string formDigest = doc.RootElement
                                   .GetProperty("d")
                                   .GetProperty("GetContextWebInformation")
                                   .GetProperty("FormDigestValue")
                                   .GetString();
            return formDigest;
        }

        

        #endregion
    }
}

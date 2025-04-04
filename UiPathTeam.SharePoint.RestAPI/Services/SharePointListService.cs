using System.Data;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using UiPathTeam.SharePoint.Service.Helpers;
using System.Net.Mail;
using System;

namespace UiPathTeam.SharePoint.RestAPI.Services
{
    public class SharePointListService : SharePointBaseService
    {
        public SharePointListService(HttpClient httpClient, string siteUrl) : base(httpClient, siteUrl)
        {
        }

        
        #region Public List Methods

            /// <summary>
            /// 1) AddListItem  
            /// Inputs: ListName, PropertiesToAdd  
            /// Output: AddedItemID
            /// </summary>
        public async Task<int> AddListItemAsync(string listName, Dictionary<string, object> propertiesToAdd)
        {
            string formDigest = await GetFormDigestAsync();
            string entityType = await GetListItemEntityTypeAsync(listName);

            var payload = new Dictionary<string, object>(propertiesToAdd)
            {
                { "__metadata", new { type = entityType } }
            };
            string jsonPayload = JsonSerializer.Serialize(payload);
            // URL encode the list name to handle spaces and special characters.
            string encodedListName = Uri.EscapeDataString(listName);
            var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{encodedListName}')/items";
            //var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);

            request.Headers.Add("X-RequestDigest", formDigest);

            // Updated content creation:
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string responseJson = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(responseJson);
            int addedItemId = doc.RootElement
                                 .GetProperty("d")
                                 .GetProperty("Id")
                                 .GetInt32();
            return addedItemId;
        }

        

        /// <summary>
        /// 2) AddListItemAttachments  
        /// Inputs: ListName, ListItemID, Attachments (a collection of Attachment objects)
        /// </summary>
        public async Task AddListItemAttachmentsAsync(string listName, int listItemId, List<String> attachments)
        {
            List<Attachment> atts = new List<Attachment>();
            foreach (var attachment in attachments)
            {
                atts.Add(new Attachment
                {
                    FileName = Path.GetFileName(attachment),
                    FileContent = File.ReadAllBytes(attachment)
                });
            }
            await AddListItemAttachmentsAsync(listName, listItemId, atts); 
        }
        /// <summary>
        /// 2) AddListItemAttachments  
        /// Inputs: ListName, ListItemID, Attachments (a collection of Attachment objects)
        /// </summary>
        public async Task AddListItemAttachmentsAsync(string listName, int listItemId, List<Attachment> attachments)
        {
            string formDigest = await GetFormDigestAsync();

            foreach (var attachment in attachments)
            {

                var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({listItemId})/AttachmentFiles/add(FileName='{Uri.EscapeDataString(attachment.FileName)}')";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                SetJsonHeaders(request);
                request.Headers.Add("X-RequestDigest", formDigest);
                request.Content = new ByteArrayContent(attachment.FileContent);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var response = await _httpClient.SendAsync(request);
                await response.EnsureSuccessOrThrowAsync();
            }
        }
        /// <summary>
        /// 3) DeleteListItemAttachments  
        /// Inputs: ListName, ListItemID, Attachments (collection of attachment file names)  
        /// Output: DeletedAttachmentsNr
        /// </summary>
        public async Task<int> DeleteListItemAttachmentsAsync(string listName, int listItemId, IEnumerable<string> attachmentFileNames)
        {
            int deletedCount = 0;
            string formDigest = await GetFormDigestAsync();

            foreach (var fileName in attachmentFileNames)
            {
                var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({listItemId})/AttachmentFiles('{Uri.EscapeDataString(fileName)}')";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                SetJsonHeaders(request);
                request.Headers.Add("X-HTTP-Method", "DELETE");
                request.Headers.Add("IF-MATCH", "*");
                request.Headers.Add("X-RequestDigest", formDigest);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    deletedCount++;
                }
            }
            return deletedCount;
        }

        /// <summary>
        /// 4) DeleteListItems  
        /// Inputs: ListName  
        /// Output: NumberOfRowsAffected
        /// (Deletes all items in the list)
        /// </summary>
        public async Task<int> DeleteListItemsAsync(string listName, int batchSize, string camlQueryText)
        {
            var itemsResult = await ReadListItemsAsync(listName, camlQueryText);
            //var itemsResult = await ReadListItemsAsync(listName, "<View><Query></Query></View>");
            var items = itemsResult.ItemsDictArray;
            int totalDeleted = 0;
            string formDigest = await GetFormDigestAsync();

            if (batchSize == 0) batchSize = items.Count;

            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batchItems = items.Skip(i).Take(batchSize).ToList();
                var batchGuid = Guid.NewGuid().ToString();
                var changesetGuid = Guid.NewGuid().ToString();

                var batchBody = new StringBuilder();
                batchBody.AppendLine($"--batch_{batchGuid}");

                foreach (var item in batchItems)
                {
                    int itemId = Convert.ToInt32(item["Id"]);

                    batchBody.AppendLine($"Content-Type: multipart/mixed; boundary=changeset_{changesetGuid}");
                    batchBody.AppendLine();
                    batchBody.AppendLine($"--changeset_{changesetGuid}");
                    batchBody.AppendLine("Content-Type: application/http");
                    batchBody.AppendLine("Content-Transfer-Encoding: binary");
                    batchBody.AppendLine();
                    batchBody.AppendLine($"DELETE {_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({itemId}) HTTP/1.1");
                    batchBody.AppendLine("IF-MATCH: *");
                    batchBody.AppendLine();
                    //batchBody.AppendLine($"--changeset_{changesetGuid}--");
                    batchBody.AppendLine();
                }
                batchBody.AppendLine($"--changeset_{changesetGuid}--");
                batchBody.AppendLine($"--batch_{batchGuid}--");

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_siteUrl}/_api/$batch")
                {
                    Content = new StringContent(batchBody.ToString())
                };
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("multipart/mixed")
                {
                    Parameters = { new NameValueHeaderValue("boundary", $"batch_{batchGuid}") }
                };
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("X-RequestDigest", formDigest);

                var reqStr = await FormatHttpRequestAsync(request);
                var response = await _httpClient.SendAsync(request);
                string responseString = await response.Content.ReadAsStringAsync();
                await response.EnsureSuccessOrThrowAsync(true);

                totalDeleted += batchItems.Count;
            }

            return totalDeleted;
            /*
            //var itemsResult = await ReadListItemsAsync(listName, "<View><Query></Query></View>");
            //int count = 0;
            //string formDigest = await GetFormDigestAsync();

            //foreach (var item in itemsResult.ItemsDictArray)
            //{
            //    int itemId = Convert.ToInt32(item["Id"]);
            //    var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({itemId})";
            //    using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            //    SetJsonHeaders(request);
            //    request.Headers.Add("X-HTTP-Method", "DELETE");
            //    request.Headers.Add("IF-MATCH", "*");
            //    request.Headers.Add("X-RequestDigest", formDigest);

            //    var response = await _httpClient.SendAsync(request);
            //    await response.EnsureSuccessOrThrowAsync();
            //    count++;
            //}
            //return count;
            */
        }

        /// <summary>
        /// 5) GetListItemAttachments  
        /// Inputs: ListName, ListItemID  
        /// Output: AttachmentNames (list of file names)
        /// </summary>
        public async Task<IEnumerable<string>> GetListItemAttachmentsAsync(string listName, int listItemId)
        {
            var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({listItemId})/AttachmentFiles";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            SetJsonHeaders(request);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string responseJson = await response.Content.ReadAsStringAsync();

            var attachments = new List<string>();
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("d", out JsonElement dElement) &&
                dElement.TryGetProperty("results", out JsonElement results))
            {
                foreach (var element in results.EnumerateArray())
                {
                    string fileName = element.GetProperty("FileName").GetString();
                    attachments.Add(fileName);
                }
            }
            return attachments;
        }

        /// <summary>
        /// 6) ReadListItems  
        /// Inputs: ListName, CAMLQuery (as XML string)  
        /// Outputs: ItemsDictArray (list of dictionaries) and ItemsTable (DataTable)
        /// </summary>
        public async Task<ReadListItemsResult> ReadListItemsAsync(string listName, string camlQuery)
        {
            var payload = new
            {
                query = new
                {
                    __metadata = new { type = "SP.CamlQuery" },
                    ViewXml = camlQuery
                }
            };
            string jsonPayload = JsonSerializer.Serialize(payload);

            var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/getitems";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            SetJsonHeaders(request);

            // Updated content creation:
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            request.Content = content;

            string formDigest = await GetFormDigestAsync();
            request.Headers.Add("X-RequestDigest", formDigest);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string responseJson = await response.Content.ReadAsStringAsync();

            var result = new ReadListItemsResult();
            var itemsDict = new List<Dictionary<string, object>>();
            var dt = new DataTable();

            using (JsonDocument doc = JsonDocument.Parse(responseJson))
            {
                if (doc.RootElement.TryGetProperty("d", out JsonElement dElement) &&
                    dElement.TryGetProperty("results", out JsonElement results))
                {
                    foreach (var item in results.EnumerateArray())
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            object value = prop.Value.ValueKind switch
                            {
                                JsonValueKind.String => prop.Value.GetString(),
                                JsonValueKind.Number => prop.Value.GetDecimal(),
                                _ => prop.Value.ToString()
                            };
                            dict[prop.Name] = value;

                            if (!dt.Columns.Contains(prop.Name))
                                dt.Columns.Add(prop.Name);
                        }
                        DataRow row = dt.NewRow();
                        foreach (var kvp in dict)
                        {
                            row[kvp.Key] = kvp.Value;
                        }
                        dt.Rows.Add(row);
                        itemsDict.Add(dict);
                    }
                }
            }
            result.ItemsDictArray = itemsDict;
            result.ItemsTable = dt;
            return result;
        }


        /*
        /// <summary>
        /// 7) UpdateListItems  
        /// Inputs: ListName, PropertiesToUpdate  
        /// Output: NumberOfRowsAffected
        /// (Updates all items in the list with the provided properties)
        /// </summary>
        //public async Task<int> UpdateListItems4Async(string listName, Dictionary<string, object> propertiesToUpdate, string camlFilter, int batchSize)
        //{
        //    int updatedCount = 0;
        //    var itemsResult = await ReadListItemsAsync(listName, camlFilter);
        //    var items = itemsResult.ItemsDictArray;
        //    if (items.Count == 0) return 0;

        //    if (batchSize == 0) batchSize = items.Count;

        //    string entityType = await GetListItemEntityTypeAsync(listName);
        //    string formDigest = await GetFormDigestAsync();

        //    for (int i = 0; i < items.Count; i += batchSize)
        //    {
        //        var batchItems = items.Skip(i).Take(batchSize).ToList();
        //        var batchGuid = Guid.NewGuid().ToString();
        //        var changesetGuid = Guid.NewGuid().ToString();

        //        var batchBody = new StringBuilder();
        //        batchBody.AppendLine($"--batch_{batchGuid}");
        //        batchBody.AppendLine($"Content-Type: multipart/mixed; boundary=changeset_{changesetGuid}");
        //        batchBody.AppendLine();

        //        foreach (var item in batchItems)
        //        {
        //            int itemId = Convert.ToInt32(item["Id"]);
        //            //var itemUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({itemId})";
        //            var itemUrl = $"/_api/web/lists/getByTitle('{listName}')/items({itemId})";

        //            //var payload = new Dictionary<string, object>(propertiesToUpdate)
        //            //{
        //            //    { "__metadata", new Dictionary<string, string> { { "type", entityType } } }
        //            //};
        //            var payload = new Dictionary<string, object>(propertiesToUpdate)
        //            {
        //                { "__metadata", new { type = entityType } }
        //            };
        //            string jsonPayload = JsonSerializer.Serialize(payload);

        //            batchBody.AppendLine($"--changeset_{changesetGuid}");
        //            batchBody.AppendLine("Content-Type: application/http");
        //            batchBody.AppendLine("Content-Transfer-Encoding: binary");
        //            batchBody.AppendLine();
        //            batchBody.AppendLine($"POST {itemUrl} HTTP/1.1");
        //            batchBody.AppendLine("Content-Type: application/json;odata=verbose");
        //            batchBody.AppendLine("IF-MATCH: *");
        //            batchBody.AppendLine("X-HTTP-Method: MERGE");
        //            batchBody.AppendLine();
        //            batchBody.AppendLine(jsonPayload);
        //            batchBody.AppendLine(); // <== THIS LINE IS REQUIRED: blank line after payload
        //        }

        //        batchBody.AppendLine($"--changeset_{changesetGuid}--");
        //        batchBody.AppendLine($"--batch_{batchGuid}--");

        //        var request = new HttpRequestMessage(HttpMethod.Post, $"{_siteUrl}/_api/$batch")
        //        {
        //            Content = new StringContent(batchBody.ToString())
        //        };
        //        Console.WriteLine("==== BATCH REQUEST START ====");
        //        Console.WriteLine(batchBody.ToString());
        //        Console.WriteLine("==== BATCH REQUEST END ====");
        //        request.Content.Headers.ContentType = new MediaTypeHeaderValue("multipart/mixed")
        //        {
        //            Parameters = { new NameValueHeaderValue("boundary", $"batch_{batchGuid}") }
        //        };
        //        request.Headers.Add("Accept", "application/json");
        //        request.Headers.Add("X-RequestDigest", formDigest);

        //        var reqStr = await FormatHttpRequestAsync(request);
        //        var response = await _httpClient.SendAsync(request);
        //        var responseStr = await LogBatchResponse(response);
                
        //        await response.EnsureSuccessOrThrowAsync();

        //        updatedCount += batchItems.Count;
        //    }

        //    return updatedCount;

        //    /*
        //    int updatedCount = 0;
        //    string formDigest = await GetFormDigestAsync();
        //    string entityType = await GetListItemEntityTypeAsync(listName);

        //    // Prepare the payload once, since it's the same for all items
        //    var payload = new Dictionary<string, object>(propertiesToUpdate)
        //    {
        //        { "__metadata", new { type = entityType } }
        //    };
        //    string jsonPayload = JsonSerializer.Serialize(payload);

        //    // Read all items that match the CAML filter
        //    var itemsResult = await ReadListItemsAsync(listName, camlFilter);
        //    var itemIds = itemsResult.ItemsDictArray.Select(item => Convert.ToInt32(item["Id"])).ToList();

        //    // Process items in batches of 100
        //    if (batchSize == 0) batchSize = itemIds.Count;

        //    for (int i = 0; i < itemIds.Count; i += batchSize)
        //    {
        //        var batchIds = itemIds.Skip(i).Take(batchSize).ToList();
        //        var boundary = "batch_" + Guid.NewGuid().ToString();
        //        var batchBody = new StringBuilder();

        //        // Construct the batch request body with update operations
        //        foreach (var id in batchIds)
        //        {
        //            batchBody.AppendLine($"--{boundary}");
        //            batchBody.AppendLine("Content-Type: application/http");
        //            batchBody.AppendLine("Content-Transfer-Encoding: binary");
        //            batchBody.AppendLine();
        //            batchBody.AppendLine($"POST /_api/web/lists/getByTitle('{listName}')/items({id}) HTTP/1.1");
        //            batchBody.AppendLine("Content-Type: application/json;odata=verbose");
        //            batchBody.AppendLine("X-HTTP-Method: MERGE");
        //            batchBody.AppendLine("IF-MATCH: *");
        //            batchBody.AppendLine($"X-RequestDigest: {formDigest}");
        //            batchBody.AppendLine();
        //            batchBody.AppendLine(jsonPayload);
        //            batchBody.AppendLine();
        //        }
        //        batchBody.AppendLine($"--{boundary}--");

        //        // Send the batch request
        //        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_siteUrl}/_api/$batch");
        //        request.Content = new StringContent(batchBody.ToString(), Encoding.UTF8, $"multipart/mixed; boundary={boundary}");


        //        var response = await _httpClient.SendAsync(request);
        //        await response.EnsureSuccessOrThrowAsync();
        //        updatedCount += batchIds.Count;
        //    }

        //    return updatedCount;
        //    */

        /*
        //    //int updatedCount = 0;
        //    ////var itemsResult = await ReadListItemsAsync(listName, "<View><Query></Query></View>");
        //    //var itemsResult = await ReadListItemsAsync(listName, camlFilter);
        //    //string formDigest = await GetFormDigestAsync();

        //    //foreach (var item in itemsResult.ItemsDictArray)
        //    //{
        //    //    int itemId = Convert.ToInt32(item["Id"]);
        //    //    string entityType = await GetListItemEntityTypeAsync(listName);

        //    //    var payload = new Dictionary<string, object>(propertiesToUpdate)
        //    //    {
        //    //        { "__metadata", new { type = entityType } }
        //    //    };
        //    //    string jsonPayload = JsonSerializer.Serialize(payload);
        //    //    var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({itemId})";
        //    //    using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        //    //    SetJsonHeaders(request);
        //    //    request.Headers.Add("X-HTTP-Method", "MERGE");
        //    //    request.Headers.Add("IF-MATCH", "*");
        //    //    request.Headers.Add("X-RequestDigest", formDigest);

        //    //    // Updated content creation:
        //    //    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        //    //    content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
        //    //    request.Content = content;

        //    //    var response = await _httpClient.SendAsync(request);
        //    //    await response.EnsureSuccessOrThrowAsync();
        //    //    updatedCount++;
        //    //}
        //    //return updatedCount;
        */


        /*
        //public async Task<int> UpdateListItems2Async(string listName, Dictionary<string, object> propertiesToUpdate, string camlFilter, int batchSize)
        //{
            

        //    int updatedCount = 0;
        //    //var itemsResult = await ReadListItemsAsync(listName, "<View><Query></Query></View>");
        //    var itemsResult = await ReadListItemsAsync(listName, camlFilter);
        //    string formDigest = await GetFormDigestAsync();

        //    foreach (var item in itemsResult.ItemsDictArray)
        //    {
        //        int itemId = Convert.ToInt32(item["Id"]);
        //        string entityType = await GetListItemEntityTypeAsync(listName);

        //        var payload = new Dictionary<string, object>(propertiesToUpdate)
        //        {
        //            { "__metadata", new { type = entityType } }
        //        };
        //        string jsonPayload = JsonSerializer.Serialize(payload);
        //        var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({itemId})";
        //        using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        //        request.Headers.Accept.Clear();
        //        var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");
        //        acceptHeader.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
        //        request.Headers.Accept.Add(acceptHeader);
        //        request.Headers.Add("X-HTTP-Method", "MERGE");
        //        request.Headers.Add("IF-MATCH", "*");
        //        request.Headers.Add("X-RequestDigest", formDigest);

        //        // Updated content creation:
        //        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        //        content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
        //        request.Content = content;

        //        var reqStr = await FormatHttpRequestAsync(request);
        //        var response = await _httpClient.SendAsync(request);
        //        var reqResponse = await LogBatchResponse(response);
        //        await response.EnsureSuccessOrThrowAsync();
        //        updatedCount++;
        //    }
        //    return updatedCount;


        //}
        //public async Task<int> UpdateListItems3Async(string listName, Dictionary<string, object> propertiesToUpdate, string camlFilter, int batchSize)
        //{
        //    int updatedCount = 0;
        //    var itemsResult = await ReadListItemsAsync(listName, camlFilter);
            
        //    string formDigest = await GetFormDigestAsync();
        //    string entityType = await GetListItemEntityTypeAsync(listName);

        //    if (batchSize <= 0)
        //        batchSize = itemsResult.ItemsDictArray.Count;

        //    var items = itemsResult.ItemsDictArray;
        //    for (int i = 0; i < items.Count; i += batchSize)
        //    {
        //        var batchItems = items.Skip(i).Take(batchSize).ToList();
        //        string boundary = "batch_" + Guid.NewGuid().ToString();
        //        var multipartContent = new MultipartContent("mixed", boundary);

        //        foreach (var item in batchItems)
        //        {
        //            int itemId = Convert.ToInt32(item["Id"]);
        //            var payload = new Dictionary<string, object>(propertiesToUpdate)
        //            {
        //                { "__metadata", new { type = entityType } }
        //            };
        //            string jsonPayload = JsonSerializer.Serialize(payload);
        //            string requestUrl = $"/_api/web/lists/getByTitle('{listName}')/items({itemId})";

        //            string requestString = $"POST {requestUrl} HTTP/1.1\r\n" +
        //                                  $"Host: {new Uri(_siteUrl).Host}\r\n" +
        //                                  "Accept: application/json;odata=verbose\r\n" +
        //                                  "Content-Type: application/json;odata=verbose\r\n" +
        //                                  "X-HTTP-Method: MERGE\r\n" +
        //                                  "IF-MATCH: *\r\n" +
        //                                  "\r\n" +
        //                                  jsonPayload;

        //            var partContent = new StringContent(requestString, Encoding.UTF8);
        //            partContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
        //            partContent.Headers.Add("Content-Transfer-Encoding", "binary");
        //            multipartContent.Add(partContent);
        //        }

        //        var batchRequest = new HttpRequestMessage(HttpMethod.Post, $"{_siteUrl}/_api/$batch");
        //        batchRequest.Headers.Add("X-RequestDigest", formDigest);
        //        batchRequest.Content = multipartContent;

        //        var reqStr = await FormatHttpRequestAsync(batchRequest);
        //        var batchResponse = await _httpClient.SendAsync(batchRequest);
        //        var reqResponse = await LogBatchResponse(batchResponse);
                
        //        if (!batchResponse.IsSuccessStatusCode)
        //        {
        //            throw new Exception($"Batch request failed: {await batchResponse.Content.ReadAsStringAsync()}");
        //        }

        //        var multipartResponse = await batchResponse.Content.ReadAsMultipartAsync();
        //        foreach (var part in multipartResponse.Contents)
        //        {
        //            var partContent = await part.ReadAsStringAsync();
        //            var lines = partContent.Split(new[] { "\r\n" }, StringSplitOptions.None);
        //            if (lines.Length > 0)
        //            {
        //                var statusLine = lines[0];
        //                var statusParts = statusLine.Split(' ');
        //                if (statusParts.Length >= 2 && statusParts[1] != "204")
        //                {
        //                    throw new Exception($"Update failed for one or more items: {partContent}");
        //                }
        //            }
        //            else
        //            {
        //                throw new Exception($"Invalid response part: {partContent}");
        //            }
        //        }

        //        updatedCount += batchItems.Count;
        //    }

        //    return updatedCount;
        

        //}
        */

        /// <summary>
        /// 7) UpdateListItems  
        /// Inputs: ListName, PropertiesToUpdate  
        /// Output: NumberOfRowsAffected
        /// (Updates all items in the list with the provided properties)
        /// </summary>
        public async Task<int> UpdateListItemsAsync(string listName, Dictionary<string, object> propertiesToUpdate, string camlFilter, int batchSize)
        {
            int updatedCount = 0;
            var itemsResult = await ReadListItemsAsync(listName, camlFilter);

            string formDigest = await GetFormDigestAsync();
            string entityType = await GetListItemEntityTypeAsync(listName);

            if (batchSize <= 0)
                batchSize = itemsResult.ItemsDictArray.Count;

            var items = itemsResult.ItemsDictArray;
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var payload = new Dictionary<string, object>(propertiesToUpdate)
                    {
                        { "__metadata", new { type = entityType } }
                    };
                string jsonPayload = JsonSerializer.Serialize(payload);

                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string batchBoundary = "batch_" + Guid.NewGuid().ToString();
                string changesetBoundary = "changeset_" + Guid.NewGuid().ToString();
                var batchContent = new StringBuilder();

                var batchItems = items.Skip(i).Take(batchSize).ToList();
                batchContent.AppendLine($"--{batchBoundary}");
                batchContent.AppendLine($"Content-Type: multipart/mixed; boundary={changesetBoundary}");
                batchContent.AppendLine();
                foreach (var item in batchItems)
                {
                    int itemId = Convert.ToInt32(item["Id"]);
                    batchContent.AppendLine($"--{changesetBoundary}");
                    batchContent.AppendLine("Content-Type: application/http");
                    batchContent.AppendLine("Content-Transfer-Encoding: binary");
                    batchContent.AppendLine();
                    //batchContent.AppendLine($"POST /_api/web/lists/getByTitle('{listName}')/items({itemId}) HTTP/1.1");
                    batchContent.AppendLine($"PATCH {_siteUrl}/_api/web/lists/getByTitle('{listName}')/items({itemId}) HTTP/1.1");
                    batchContent.AppendLine("Accept: application/json;odata=verbose");
                    batchContent.AppendLine("Content-Type: application/json;odata=verbose");
                    //batchContent.AppendLine($"X-RequestDigest: {formDigest}");
                    batchContent.AppendLine("X-HTTP-Method: MERGE");
                    batchContent.AppendLine("IF-MATCH: *");
                    batchContent.AppendLine();
                    //batchContent.AppendLine("{\"Title\":\"mass updatess\",\"__metadata\":{\"type\":\"SP.Data.Test_x005f_listListItem\"}}");
                    batchContent.AppendLine(jsonPayload);
                    batchContent.AppendLine();
                }

                // End the batch with the closing boundary
                batchContent.AppendLine($"--{changesetBoundary}--");
                batchContent.AppendLine($"--{batchBoundary}--");

                // Create the StringContent with the proper content type and boundary
                var content = new StringContent(batchContent.ToString(), Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("multipart/mixed");
                content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", batchBoundary));

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_siteUrl}/_api/$batch")
                {
                    Content = content
                };
                request.Headers.Add("X-RequestDigest", formDigest);
                request.Headers.Add("Accept", "application/json");

                var reqStr = await FormatHttpRequestAsync(request);
                HttpResponseMessage batchResponse = await _httpClient.SendAsync(request);
                //HttpResponseMessage batchResponse = await _httpClient.PostAsync($"{_siteUrl}/_api/$batch", content);

                string responseString = await batchResponse.Content.ReadAsStringAsync();
                await batchResponse.EnsureSuccessOrThrowAsync(true);
                //var multipartResponse = await batchResponse.Content.ReadAsMultipartAsync();
                //foreach (var part in multipartResponse.Contents)
                //{
                //    var partContent = await part.ReadAsStringAsync();
                //    var lines = partContent.Split(new[] { "\r\n" }, StringSplitOptions.None);
                //    if (lines.Length > 0)
                //    {
                //        var statusLine = lines[0];
                //        var statusParts = statusLine.Split(' ');
                //        if (statusParts.Length >= 2 && statusParts[1] != "204")
                //        {
                //            throw new Exception($"Update failed for one or more items: {partContent}");
                //        }
                //    }
                //    else
                //    {
                //        throw new Exception($"Invalid response part: {partContent}");
                //    }
                //}

                updatedCount += batchItems.Count;
            }

            return updatedCount;


        }
        
        private async Task<string> LogBatchResponse(HttpResponseMessage response)
        {
            string content = await response.Content.ReadAsStringAsync();
            Console.WriteLine("==== BATCH RESPONSE START ====");
            Console.WriteLine(content);
            Console.WriteLine("==== BATCH RESPONSE END ====");

            // Very simple parsing - just find status lines
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("HTTP/1.1"))
                {
                    Console.WriteLine("Batch operation status: " + line);
                }
            }
            return content;
        }

        public static async Task<string> FormatHttpRequestAsync(HttpRequestMessage request)
        {
            var sb = new StringBuilder();

            // Append request line (method, URI, HTTP version)
            sb.AppendLine($"{request.Method} {request.RequestUri} HTTP/{request.Version}");

            // Append headers
            foreach (var header in request.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            // If the request has content, append content headers and the body
            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                // Read and append the content body
                string contentBody = await request.Content.ReadAsStringAsync();
                sb.AppendLine();
                sb.AppendLine(contentBody);
            }
            //Console.WriteLine(sb.ToString());
            return sb.ToString();
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
            request.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
            request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            // For AppOnly mode, try sending no content or an empty string.
            //if (loginMode == SharePointLoginMode.AppOnly)
            //{
            //    // Option 1: Set content to an empty string with no additional JSON formatting.
            //    request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            //}
            //else
            //{
            //    // For other modes, send a minimal valid JSON payload.
            //    var content = new StringContent("{}", Encoding.UTF8, "application/json");
            //    content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
            //    request.Content = content;
            //}

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
        /// Retrieves the ListItemEntityTypeFullName for the given list.
        /// </summary>
        private async Task<string> GetListItemEntityTypeAsync(string listName)
        {
            var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')?$select=ListItemEntityTypeFullName";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            SetJsonHeaders(request);
            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string responseJson = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(responseJson);
            string entityType = doc.RootElement
                                   .GetProperty("d")
                                   .GetProperty("ListItemEntityTypeFullName")
                                   .GetString();
            return entityType;
        }

        
        override public string ToString()
        {
            return $"SharePointListService: {_siteUrl}";
        }
        

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Represents an attachment file.
    /// </summary>
    public class Attachment
    {
        public string FileName { get; set; }
        public byte[] FileContent { get; set; }
    }

    /// <summary>
    /// Result for the ReadListItems method.
    /// Contains a list of dictionaries (one per item) and a DataTable.
    /// </summary>
    public class ReadListItemsResult
    {
        public List<Dictionary<string, object>> ItemsDictArray { get; set; }
        public DataTable ItemsTable { get; set; }
    }

        
    #endregion
}

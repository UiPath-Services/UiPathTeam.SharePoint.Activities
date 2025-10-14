using System;
using System.Data;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UiPathTeam.SharePoint.Service.Helpers;

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

        //public async Task<ReadListItemsResult> ReadListItemsRender2Async(string listName, string camlQuery)
        //{
        //    bool callerHasViewFields = !string.IsNullOrWhiteSpace(camlQuery) &&
        //                       camlQuery.IndexOf("<ViewFields", StringComparison.OrdinalIgnoreCase) >= 0;

        //    string viewFieldsXml = string.Empty;
        //    List<string> ensuredFields = new List<string>();
        //    if (!callerHasViewFields)
        //    {
        //        var viewFields = await GetSchemaFieldsAsync(listName, camlQuery);
        //        ensuredFields = EnsureCoreFields(viewFields);

        //        var fieldRefs = string.Join("",
        //            ensuredFields.Select(f => $"<FieldRef Name='{System.Security.SecurityElement.Escape(f)}' />"));
        //        viewFieldsXml = $"<ViewFields>{fieldRefs}</ViewFields>";
        //    }
        //    else
        //    {
        //        // Regex to capture Name="..."
        //        var matches = System.Text.RegularExpressions.Regex.Matches(
        //            camlQuery,
        //            "Name\\s*=\\s*\"([^\"]+)\"",
        //            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        //        var viewFields = new List<string>();
        //        foreach (System.Text.RegularExpressions.Match m in matches)
        //        {
        //            if (m.Groups.Count > 1)
        //                viewFields.Add(m.Groups[1].Value);
        //        }
        //        ensuredFields = EnsureCoreFields(viewFields);
        //    }
        //    string queryXml = string.IsNullOrWhiteSpace(camlQuery) ? "<Query/>" : camlQuery;

        //    string viewXml = $@"
        //                    <View Scope='RecursiveAll'>
        //                      {queryXml}
        //                      {viewFieldsXml}
        //                    </View>".Trim();
        //    //// 1) Ensure we have a minimal <View/> if none supplied
        //    //string viewXml = string.IsNullOrWhiteSpace(camlQuery)
        //    //? "<View Scope='RecursiveAll'><Query/></View>"
        //    //: $"<View Scope='RecursiveAll'>{camlQuery}</View>";

        //    // 2) Build the payload *without* __metadata
        //    var payload = new
        //    {
        //        parameters = new
        //        {
        //            ViewXml = viewXml,
        //            RenderOptions = 4    // 2 = IncludeListData (raw + lookupValue)
        //        }
        //    };
        //    string body = JsonSerializer.Serialize(payload);

        //    // 3) Prepare the RenderListDataAsStream request
        //    var url = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/RenderListDataAsStream";
        //    using var req = new HttpRequestMessage(HttpMethod.Post, url);

        //    // Accept: application/json;odata=nometadata
        //    req.Headers.Accept.Clear();
        //    req.Headers.Accept.ParseAdd("application/json;odata=nometadata");

        //    // X-RequestDigest header
        //    req.Headers.Add("X-RequestDigest", await GetFormDigestAsync());

        //    // Content-Type: application/json;odata=nometadata
        //    var content = new StringContent(body, Encoding.UTF8);
        //    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;odata=nometadata");
        //    req.Content = content;

        //    // 4) Send and ensure success
        //    var resp = await _httpClient.SendAsync(req);
        //    await resp.EnsureSuccessOrThrowAsync();
        //    var json = await resp.Content.ReadAsStringAsync();

        //    // 5) Parse the nometadata response
        //    using var doc = JsonDocument.Parse(json);
        //    // Top‐level has "RenderListData" with a "Row" array
        //    //var rows = doc.RootElement
        //    //              .GetProperty("RenderListData")
        //    //              .GetProperty("Row")
        //    //              .EnumerateArray();
        //    var rows = GetRows(doc.RootElement);   // <- robust to both shapes

        //    // 6) Build DataTable and dictionary list
        //    var dt = new DataTable();
        //    var items = new List<Dictionary<string, object>>();


        //    // helper to detect user objects
        //    bool IsUserObject(JsonElement e)
        //    {
        //        return e.ValueKind == JsonValueKind.Object &&
        //               (e.TryGetProperty("id", out _) || e.TryGetProperty("title", out _) || e.TryGetProperty("email", out _));
        //    }
        //    bool IsUserArray(JsonElement v)
        //    {
        //        if (v.ValueKind != JsonValueKind.Array) return false;
        //        using var en = v.EnumerateArray();
        //        if (!en.MoveNext()) return false;
        //        return IsUserObject(en.Current);
        //    }

        //    foreach (var row in rows)
        //    {
        //        // Collect raw values and lookupValues
        //        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        //        //var lookupMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        //        foreach (var prop in row.EnumerateObject())
        //        {
        //            string name = prop.Name;
        //            var val = prop.Value;

        //            // Ignore noisy display-only helpers
        //            if (name.EndsWith(".span", StringComparison.OrdinalIgnoreCase))
        //                continue;

        //            if (name.EndsWith(".lookupValue", StringComparison.OrdinalIgnoreCase))
        //            {
        //                //// e.g. "Author.lookupValue" → field "Author"
        //                //var field = name[..name.IndexOf('.')];
        //                //lookupMap[field] = val.GetString();
        //                continue;
        //            }
        //            if (name.EndsWith(".term", StringComparison.OrdinalIgnoreCase))
        //            {
        //                // Managed metadata GUID or value
        //                dict[name] = val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
        //                continue;
        //            }
        //            if (name.Contains('.') && row.TryGetProperty(name.Split('.')[0], out var baseVal) && baseVal.ValueKind == JsonValueKind.Array)
        //            {
        //                // Skip redundant e15p.id / e15p.title if "e15p" array is already there
        //                continue;
        //            }
        //            // --- User field handling ---
        //            if (IsUserArray(val))
        //            {
        //                var users = val.EnumerateArray().Select(u => new ListSPUser
        //                {
        //                    Id = u.TryGetProperty("id", out var pId) ? pId.ToString() : null,
        //                    Title = u.TryGetProperty("title", out var pTitle) ? pTitle.GetString() : null,
        //                    Email = u.TryGetProperty("email", out var pEmail) ? pEmail.GetString() : null,
        //                    Sip = u.TryGetProperty("sip", out var pSip) ? pSip.GetString() : null,
        //                    Picture = u.TryGetProperty("picture", out var pPic) ? pPic.GetString() : null
        //                }).ToList();

        //                // single vs multi-value
        //                if (users.Count == 1)
        //                    dict[name] = users.First();
        //                else
        //                    dict[name] = users;

        //                continue;
        //            }
        //            // arrays (non-user) → raw JSON string
        //            if (val.ValueKind == JsonValueKind.Array)
        //            {
        //                dict[name] = val.ToString();
        //                continue;
        //            }
        //            if (ensuredFields.Contains(name))
        //            {
        //                // primitives
        //                dict[name] = val.ValueKind switch
        //                {
        //                    JsonValueKind.String => val.GetString(),
        //                    JsonValueKind.Number => val.TryGetInt64(out var i) ? i : (object)val.GetDecimal(),
        //                    JsonValueKind.True => true,
        //                    JsonValueKind.False => false,
        //                    JsonValueKind.Null => null,
        //                    _ => val.ToString()
        //                };
        //            }





        //        }

        //        //// 7) Overlay each lookupValue onto the raw field
        //        //foreach (var kv in lookupMap)
        //        //{
        //        //    dict[kv.Key] = kv.Value;
        //        //}

        //        // 8) Add to DataTable
        //        foreach (var col in dict.Keys)
        //        {
        //            if (!dt.Columns.Contains(col))
        //                dt.Columns.Add(col);
        //        }

        //        var dr = dt.NewRow();
        //        foreach (var kv in dict)
        //            dr[kv.Key] = kv.Value ?? DBNull.Value;
        //        dt.Rows.Add(dr);

        //        items.Add(dict);
        //    }

        //    // 9) Return the result
        //    return new ReadListItemsResult
        //    {
        //        ItemsTable = dt,
        //        ItemsDictArray = items
        //    };
        //}

        public async Task<ReadListItemsResult> ReadListItemsAsync(string listName, string camlQuery)
        {
            bool callerHasViewFields = !string.IsNullOrWhiteSpace(camlQuery) &&
                               camlQuery.IndexOf("<ViewFields", StringComparison.OrdinalIgnoreCase) >= 0;
            
            string viewFieldsXml = string.Empty;
            List<string> ensuredFields = new List<string>();
            if (!callerHasViewFields)
            {
                var viewFields = await GetDefaultViewFieldsAsync(listName);
                ensuredFields = EnsureCoreFields(viewFields);

                var fieldRefs = string.Join("",
                    ensuredFields.Select(f => $"<FieldRef Name='{System.Security.SecurityElement.Escape(f)}' />"));
                viewFieldsXml = $"<ViewFields>{fieldRefs}</ViewFields>";
            }else{
                // Regex to capture Name="..."
                //var matches = System.Text.RegularExpressions.Regex.Matches(
                //    camlQuery,
                //    "Name\\s*=\\s*\"([^\"]+)\"",
                //    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var matches = Regex.Matches(
                    camlQuery,
                    @"Name\s*=\s*['""]([^'""]+)['""]",
                    RegexOptions.IgnoreCase);
                var viewFields = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    if (m.Groups.Count > 1)
                        viewFields.Add(m.Groups[1].Value);
                }
                ensuredFields = EnsureCoreFields(viewFields);
            }
            string queryXml = string.Empty;
            if (string.IsNullOrWhiteSpace(camlQuery))
            {
                queryXml = "<Query/>";
            }
            else
            {
                var match = Regex.Match(camlQuery,
                                        @"<View[^>]*>(?<inner>[\s\S]*?)<\/View>",
                                        RegexOptions.IgnoreCase);

                queryXml = match.Success ? match.Groups["inner"].Value : camlQuery;
            }
                //string queryXml = string.IsNullOrWhiteSpace(camlQuery) ? "<Query/>" : camlQuery;

            string viewXml = $@"
                            <View Scope='RecursiveAll'>
                              {queryXml}
                              {viewFieldsXml}
                            </View>".Trim();
            //// 1) Ensure we have a minimal <View/> if none supplied
            //string viewXml = string.IsNullOrWhiteSpace(camlQuery)
            //? "<View Scope='RecursiveAll'><Query/></View>"
            //: $"<View Scope='RecursiveAll'>{camlQuery}</View>";

            // 2) Build the payload *without* __metadata
            var payload = new
            {
                parameters = new
                {
                    ViewXml = viewXml,
                    RenderOptions = 2    // 2 = IncludeListData (raw + lookupValue)
                }
            };
            string body = JsonSerializer.Serialize(payload);

            // 3) Prepare the RenderListDataAsStream request
            var url = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/RenderListDataAsStream";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);

            // Accept: application/json;odata=nometadata
            req.Headers.Accept.Clear();
            req.Headers.Accept.ParseAdd("application/json;odata=nometadata");

            // X-RequestDigest header
            req.Headers.Add("X-RequestDigest", await GetFormDigestAsync());

            // Content-Type: application/json;odata=nometadata
            var content = new StringContent(body, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;odata=nometadata");
            req.Content = content;

            // 4) Send and ensure success
            var resp = await _httpClient.SendAsync(req);
            await resp.EnsureSuccessOrThrowAsync();
            var json = await resp.Content.ReadAsStringAsync();

            // 5) Parse the nometadata response
            using var doc = JsonDocument.Parse(json);
            // Top‐level has "RenderListData" with a "Row" array
            //var rows = doc.RootElement
            //              .GetProperty("RenderListData")
            //              .GetProperty("Row")
            //              .EnumerateArray();
            var rows = GetRows(doc.RootElement);   // <- robust to both shapes

            // 6) Build DataTable and dictionary list
            var dt = new DataTable();
            var items = new List<Dictionary<string, object>>();


            // helper to detect user objects
            bool IsUserObject(JsonElement e)
            {
                return e.ValueKind == JsonValueKind.Object &&
                       (e.TryGetProperty("id", out _) || e.TryGetProperty("title", out _) || e.TryGetProperty("email", out _));
            }
            bool IsUserArray(JsonElement v)
            {
                if (v.ValueKind != JsonValueKind.Array) return false;
                using var en = v.EnumerateArray();
                if (!en.MoveNext()) return false;
                return IsUserObject(en.Current);
            }

            foreach (var row in rows)
            {
                // Collect raw values and lookupValues
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                //var lookupMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var prop in row.EnumerateObject())
                {
                    string name = prop.Name;
                    var val = prop.Value;
                    
                    // Ignore noisy display-only helpers
                    if (name.EndsWith(".span", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (name.EndsWith(".lookupValue", StringComparison.OrdinalIgnoreCase))
                    {
                        //// e.g. "Author.lookupValue" → field "Author"
                        //var field = name[..name.IndexOf('.')];
                        //lookupMap[field] = val.GetString();
                        continue;
                    }
                    if (name.EndsWith(".term", StringComparison.OrdinalIgnoreCase))
                    {
                        // Managed metadata GUID or value
                        dict[name] = val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString();
                        continue;
                    }
                    if (name.Contains('.') && row.TryGetProperty(name.Split('.')[0], out var baseVal) && baseVal.ValueKind == JsonValueKind.Array)
                    {
                        // Skip redundant e15p.id / e15p.title if "e15p" array is already there
                        continue;
                    }
                    // --- User field handling ---
                    if (IsUserArray(val))
                    {
                        var users = val.EnumerateArray().Select(u => new ListSPUser
                        {
                            Id = u.TryGetProperty("id", out var pId) ? pId.ToString() : null,
                            Title = u.TryGetProperty("title", out var pTitle) ? pTitle.GetString() : null,
                            Email = u.TryGetProperty("email", out var pEmail) ? pEmail.GetString() : null,
                            Sip = u.TryGetProperty("sip", out var pSip) ? pSip.GetString() : null,
                            Picture = u.TryGetProperty("picture", out var pPic) ? pPic.GetString() : null
                        }).ToList();

                        // single vs multi-value
                        if (users.Count == 1)
                            dict[name] = users.First();
                        else
                            dict[name] = users;

                        continue;
                    }
                    // arrays (non-user) → raw JSON string
                    if (val.ValueKind == JsonValueKind.Array)
                    {
                        dict[name] = val.ToString();
                        continue;
                    }
                    if (ensuredFields.Contains(name))
                    {
                        // primitives
                        dict[name] = val.ValueKind switch
                        {
                            JsonValueKind.String => val.GetString(),
                            JsonValueKind.Number => val.TryGetInt64(out var i) ? i : (object)val.GetDecimal(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => val.ToString()
                        };
                    }
                    




                }

                //// 7) Overlay each lookupValue onto the raw field
                //foreach (var kv in lookupMap)
                //{
                //    dict[kv.Key] = kv.Value;
                //}

                // 8) Add to DataTable
                foreach (var col in dict.Keys)
                {
                    if (!dt.Columns.Contains(col))
                        dt.Columns.Add(col);
                }

                var dr = dt.NewRow();
                foreach (var kv in dict)
                    dr[kv.Key] = kv.Value ?? DBNull.Value;
                dt.Rows.Add(dr);

                items.Add(dict);

            }

            // 9) Return the result
            return new ReadListItemsResult
            {
                ItemsTable = dt,
                ItemsDictArray = items
            };
        }
        // Helper: get /DefaultView/ViewFields for the list
        //private async Task<IEnumerable<string>> GetSchemaFieldsAsync(string listName, string camlQuery)
        //{
        //    string viewXml = string.IsNullOrWhiteSpace(camlQuery)
        //    ? "<View Scope='RecursiveAll'><Query/></View>"
        //    : $"<View Scope='RecursiveAll'>{camlQuery}</View>";

        //    var payload = new
        //    {
        //        parameters = new
        //        {
        //            ViewXml = "<View/>",
        //            RenderOptions = 4    // 2 = IncludeListData (raw + lookupValue)
        //        }
        //    };
        //    string body = JsonSerializer.Serialize(payload);


        //    var url = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/RenderListDataAsStream";
        //    using var req = new HttpRequestMessage(HttpMethod.Post, url);

        //    // Accept: application/json;odata=nometadata
        //    req.Headers.Accept.Clear();
        //    req.Headers.Accept.ParseAdd("application/json;odata=nometadata");

        //    // X-RequestDigest header
        //    req.Headers.Add("X-RequestDigest", await GetFormDigestAsync());

        //    // Content-Type: application/json;odata=nometadata
        //    var content = new StringContent(body, Encoding.UTF8);
        //    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;odata=nometadata");
        //    req.Content = content;

        //    // 4) Send and ensure success
        //    var resp = await _httpClient.SendAsync(req);
        //    await resp.EnsureSuccessOrThrowAsync();
        //    var json = await resp.Content.ReadAsStringAsync();

        //    // 5) Parse the nometadata response
        //    using var doc = JsonDocument.Parse(json);
        //    var root = doc.RootElement;

        //    var fieldNames = new List<string>();

        //    if (root.TryGetProperty("Field", out var fieldsElem) &&
        //        fieldsElem.ValueKind == JsonValueKind.Array)
        //    {
        //        foreach (var field in fieldsElem.EnumerateArray())
        //        {
        //            if (field.TryGetProperty("RealFieldName", out var nameProp))
        //            {
        //                var name = nameProp.GetString();
        //                if (!string.IsNullOrWhiteSpace(name))
        //                    fieldNames.Add(name);
        //            }
        //        }
        //    }

        //    return fieldNames;
        //}
        // Helper: get /DefaultView/ViewFields for the list
        private async Task<IEnumerable<string>> GetDefaultViewFieldsAsync(string listName)
        {
            var url = $"{_siteUrl}/_api/web/lists/getbytitle('{listName}')/DefaultView/ViewFields";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.ParseAdd("application/json;odata=nometadata");

            using var res = await _httpClient.SendAsync(req).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
            return ParseDefaultViewFieldsFromResponse(json);
        }
        private static List<string> ParseDefaultViewFieldsFromResponse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Unwrap "d" if present (odata=verbose)
            if (root.TryGetProperty("d", out var d)) root = d;

            if (root.TryGetProperty("Items", out var items2) && items2.ValueKind == JsonValueKind.Array)
            {
                return items2.EnumerateArray()
                              .Select(x => x.GetString())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToList();
            }
            // 1) Preferred: Items.results
            if (root.TryGetProperty("Items", out var items) &&
                items.TryGetProperty("results", out var results))
            {
                return results.EnumerateArray()
                              .Select(x => x.GetString())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToList();
            }
            

            // 2) Fallback: parse SchemaXml: <FieldRef Name="..." />
            if (root.TryGetProperty("SchemaXml", out var schemaXmlElem))
            {
                var schema = schemaXmlElem.GetString() ?? string.Empty;
                var names = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in
                         System.Text.RegularExpressions.Regex.Matches(schema, "Name=\\\"([^\\\"]+)\\\""))
                {
                    names.Add(m.Groups[1].Value);
                }
                return names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            // 3) Last resort: if response is directly an array
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray()
                           .Select(x => x.GetString())
                           .Where(s => !string.IsNullOrWhiteSpace(s))
                           .Distinct(StringComparer.OrdinalIgnoreCase)
                           .ToList();
            }

            return new List<string>(); // nothing found
        }
        // Helper: add system/core fields
        private static List<string> EnsureCoreFields(IEnumerable<string> fields)
        {
            var set = new HashSet<string>(fields ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            string[] core = { "ID", "ContentTypeId", "Modified", "Created", "Author", "Editor", "FileRef", "FSObjType" };
            foreach (var f in core) set.Add(f);
            return set.ToList();
        }
        private static IEnumerable<JsonElement> GetRows(JsonElement root)
        {
            // Shape A (your case):
            // { "Row": [ ... ], "FirstRow": 1, "LastRow": 2, ... }
            if (root.TryGetProperty("Row", out var rowA) && rowA.ValueKind == JsonValueKind.Array)
                return rowA.EnumerateArray();

            // Shape B:
            // { "RenderListData": { "Row": [ ... ], ... } }
            if (root.TryGetProperty("RenderListData", out var rld) &&
                rld.ValueKind == JsonValueKind.Object &&
                rld.TryGetProperty("Row", out var rowB) &&
                rowB.ValueKind == JsonValueKind.Array)
                return rowB.EnumerateArray();

            // Shape C (rare): { "d": { "RenderListData": { "Row": [ ... ] } } }
            if (root.TryGetProperty("d", out var d) &&
                d.ValueKind == JsonValueKind.Object &&
                d.TryGetProperty("RenderListData", out var rld2) &&
                rld2.ValueKind == JsonValueKind.Object &&
                rld2.TryGetProperty("Row", out var rowC) &&
                rowC.ValueKind == JsonValueKind.Array)
                return rowC.EnumerateArray();

            // Nothing matched
            return Array.Empty<JsonElement>();
        }
        

        //public async Task<ReadListItemsResult> ReadListItemsAsync(string listName, string camlQuery)
        //{
        //    // 1) If no CAML supplied, use a bare <View/> so SP falls back to default view
        //    if (string.IsNullOrWhiteSpace(camlQuery))
        //    {
        //        camlQuery = "<View/>";
        //    }

        //    // 2) Build CAML POST payload
        //    var payload = new
        //    {
        //        query = new
        //        {
        //            __metadata = new { type = "SP.CamlQuery" },
        //            ViewXml = camlQuery
        //        }
        //    };
        //    var body = JsonSerializer.Serialize(payload);

        //    // 3) Compose the REST URL: expand only FieldValuesAsText
        //    var url = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/GetItems" +
        //              "?$expand=FieldValuesAsText" +
        //              "&$select=*,FieldValuesAsText";

        //    using var req = new HttpRequestMessage(HttpMethod.Post, url);
        //    SetJsonHeaders(req);
        //    req.Headers.Add("X-RequestDigest", await GetFormDigestAsync());
        //    req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        //    req.Content.Headers.ContentType.Parameters.Add(
        //        new NameValueHeaderValue("odata", "verbose")
        //    );

        //    // 4) Send & ensure success
        //    var resp = await _httpClient.SendAsync(req);
        //    await resp.EnsureSuccessOrThrowAsync();
        //    var json = await resp.Content.ReadAsStringAsync();

        //    // 5) Parse out items, pulling each person-field name from FieldValuesAsText
        //    using var doc = JsonDocument.Parse(json);
        //    var results = doc.RootElement
        //                     .GetProperty("d")
        //                     .GetProperty("results")
        //                     .EnumerateArray();

        //    var dt = new DataTable();
        //    var items = new List<Dictionary<string, object>>();

        //    foreach (var item in results)
        //    {
        //        // grab the flattened text values
        //        var fv = item.GetProperty("FieldValuesAsText");

        //        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        //        foreach (var prop in item.EnumerateObject())
        //        {
        //            if (prop.Name == "FieldValuesAsText")
        //                continue;   // skip the nested object itself

        //            object val;
        //            if (fv.TryGetProperty(prop.Name, out var textVal))
        //            {
        //                // if FieldValuesAsText has this name, use it (for person, choice, etc.)
        //                switch (textVal.ValueKind)
        //                {
        //                    case JsonValueKind.String:
        //                        val = textVal.GetString();
        //                        break;
        //                    case JsonValueKind.Null:
        //                        val = null;
        //                        break;
        //                    default:
        //                        // could be object or array; fall back to raw JSON
        //                        val = textVal.ToString();
        //                        break;
        //                }
        //            }
        //            else if (prop.Value.ValueKind == JsonValueKind.String)
        //            {
        //                val = prop.Value.GetString();
        //            }
        //            else if (prop.Value.ValueKind == JsonValueKind.Number)
        //            {
        //                val = prop.Value.GetDecimal();
        //            }
        //            else
        //            {
        //                val = prop.Value.ToString();
        //            }

        //            dict[prop.Name] = val;
        //            if (!dt.Columns.Contains(prop.Name))
        //                dt.Columns.Add(prop.Name);
        //        }

        //        var row = dt.NewRow();
        //        foreach (var kv in dict)
        //            row[kv.Key] = kv.Value ?? DBNull.Value;
        //        dt.Rows.Add(row);

        //        items.Add(dict);
        //    }

        //    return new ReadListItemsResult
        //    {
        //        ItemsTable = dt,
        //        ItemsDictArray = items
        //    };
        //}

        //public async Task<ReadListItemsResult> ReadListItems2Async(string listName, string camlQuery)
        //{

        //    string viewXml = string.IsNullOrWhiteSpace(camlQuery)
        //    ? "<View Scope='RecursiveAll'><Query/></View>"
        //    : $"<View Scope='RecursiveAll'>{camlQuery}</View>";


            
        //    //string[] personFields = new[] { "Author", "Editor", "e15p" };
        //    string[] personFields = new[] { "e15p" };
        //    var expand = string.Join(",", personFields);
        //    var selectParts = new List<string> { "Id", "Title", "FileSystemObjectType", "ContentTypeId", "Created", "Modified", "Attachments", "GUID" };
        //    foreach (var f in personFields)
        //        selectParts.AddRange(new[] { $"{f}/Id", $"{f}/Title", $"{f}/EMail" });
        //    // If you also need the raw numeric IDs (AuthorId etc.), include them too:
        //    selectParts.AddRange(personFields.Select(f => f + "Id"));

        //    var select = string.Join(",", selectParts);
        //    var url = $"{_siteUrl}/_api/web/lists/getbytitle('{Uri.EscapeDataString(listName)}')/GetItems" +
        //              $"?$select={Uri.EscapeDataString(select)}&$expand={Uri.EscapeDataString(expand)}";


        //    var payload = new { query = new { ViewXml = viewXml } };
        //    var json = JsonSerializer.Serialize(payload);

        //    using var req = new HttpRequestMessage(HttpMethod.Post, url);
        //    var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");
        //    acceptHeader.Parameters.Add(new NameValueHeaderValue("odata", "nometadata"));
        //    req.Headers.Accept.Add(acceptHeader);
        //    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        //    req.Content.Headers.ContentType.Parameters.Add(
        //        new NameValueHeaderValue("odata", "nometadata")
        //    );
        //    req.Headers.Add("X-RequestDigest", await GetFormDigestAsync());

           
        //    //req.Content.Headers.ContentType.Parameters.Add(
        //    //    new NameValueHeaderValue("odata", "verbose")
        //    //);

        //    // 4) Send & ensure success
        //    var res = await _httpClient.SendAsync(req);
        //    await res.EnsureSuccessOrThrowAsync();
        //    var txt = await res.Content.ReadAsStringAsync();

        //    // 5) Parse out items, pulling each person-field name from FieldValuesAsText
        //    using var doc = JsonDocument.Parse(txt);

        //    var items = doc.RootElement.GetProperty("value");
        //    var list = new List<Dictionary<string, object>>(items.GetArrayLength());

        //    foreach (var item in items.EnumerateArray())
        //    {
        //        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        //        foreach (var p in item.EnumerateObject())
        //        {
        //            if (p.Name.StartsWith("@")) continue; // ignore metadata if any
        //            dict[p.Name] = p.Value.ValueKind switch
        //            {
        //                JsonValueKind.String => p.Value.GetString(),
        //                JsonValueKind.Number => p.Value.TryGetInt64(out var l) ? l : p.Value.GetDouble(),
        //                JsonValueKind.True => true,
        //                JsonValueKind.False => false,
        //                JsonValueKind.Null => null,
        //                _ => p.Value.ToString()
        //            };
        //        }
        //        list.Add(dict);
        //    }

        //    // match your original return type: object[]
        //    //return list.Cast<object>().ToArray();

        //    return new ReadListItemsResult
        //    {
        //        ItemsTable = null,
        //        ItemsDictArray = list
        //    };
        //}




        ///// <summary>
        ///// 6) ReadListItems  
        ///// Inputs: ListName, CAMLQuery (as XML string)  
        ///// Outputs: ItemsDictArray (list of dictionaries) and ItemsTable (DataTable)
        ///// </summary>
        //public async Task<ReadListItemsResult> ReadListItemsBackupAsync(string listName, string camlQuery)
        //{
        //    var payload = new
        //    {
        //        query = new
        //        {
        //            __metadata = new { type = "SP.CamlQuery" },
        //            ViewXml = camlQuery
        //        }
        //    };
        //    string jsonPayload = JsonSerializer.Serialize(payload);

        //    var requestUrl = $"{_siteUrl}/_api/web/lists/getByTitle('{listName}')/getitems";
        //    using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        //    SetJsonHeaders(request);

        //    // Updated content creation:
        //    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        //    content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata", "verbose"));
        //    request.Content = content;

        //    string formDigest = await GetFormDigestAsync();
        //    request.Headers.Add("X-RequestDigest", formDigest);

        //    var response = await _httpClient.SendAsync(request);
        //    await response.EnsureSuccessOrThrowAsync();
        //    string responseJson = await response.Content.ReadAsStringAsync();

        //    var result = new ReadListItemsResult();
        //    var itemsDict = new List<Dictionary<string, object>>();
        //    var dt = new DataTable();

        //    using (JsonDocument doc = JsonDocument.Parse(responseJson))
        //    {
        //        if (doc.RootElement.TryGetProperty("d", out JsonElement dElement) &&
        //            dElement.TryGetProperty("results", out JsonElement results))
        //        {
        //            foreach (var item in results.EnumerateArray())
        //            {
        //                var dict = new Dictionary<string, object>();
        //                foreach (var prop in item.EnumerateObject())
        //                {
        //                    object value = prop.Value.ValueKind switch
        //                    {
        //                        JsonValueKind.String => prop.Value.GetString(),
        //                        JsonValueKind.Number => prop.Value.GetDecimal(),
        //                        _ => prop.Value.ToString()
        //                    };
        //                    dict[prop.Name] = value;

        //                    if (!dt.Columns.Contains(prop.Name))
        //                        dt.Columns.Add(prop.Name);
        //                }
        //                DataRow row = dt.NewRow();
        //                foreach (var kvp in dict)
        //                {
        //                    row[kvp.Key] = kvp.Value;
        //                }
        //                dt.Rows.Add(row);
        //                itemsDict.Add(dict);
        //            }
        //        }
        //    }
        //    result.ItemsDictArray = itemsDict;
        //    result.ItemsTable = dt;
        //    return result;
        //}


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

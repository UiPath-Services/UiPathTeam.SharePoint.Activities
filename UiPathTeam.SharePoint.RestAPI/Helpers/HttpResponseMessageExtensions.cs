using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;

namespace UiPathTeam.SharePoint.Service.Helpers
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task EnsureSuccessOrThrowAsync(this HttpResponseMessage response, bool MultiPartsSuccessCheck = false)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    JsonElement errorElement;

                    // Try "error" property first, then "odata.error"
                    if (doc.RootElement.TryGetProperty("error", out errorElement) ||
                        doc.RootElement.TryGetProperty("odata.error", out errorElement))
                    {
                        string errorCode = errorElement.GetProperty("code").GetString();
                        string errorMessage = errorElement.GetProperty("message").GetProperty("value").GetString();
                        throw new Exception($"SharePoint API error (HTTP {response.StatusCode}): Code: {errorCode}, Message: {errorMessage}");
                    }
                    else
                    {
                        throw new Exception($"SharePoint API error (HTTP {response.StatusCode}): {errorContent}");
                    }
                }
                catch (JsonException)
                {
                    throw new Exception($"SharePoint API error (HTTP {response.StatusCode}): {errorContent}");
                }
            }else if (MultiPartsSuccessCheck)
            {
                var multipartResponse = await response.Content.ReadAsMultipartAsync();
                foreach (var part in multipartResponse.Contents)
                {
                    var partContent = await part.ReadAsStringAsync();
                    var lines = partContent.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    if (lines.Length > 0)
                    {
                        var statusLine = lines[0];
                        var statusParts = statusLine.Split(' ');
                        int statusCode = 0;
                        if (statusParts.Length >= 2 && int.TryParse(statusParts[1], out statusCode) && !(statusCode >= 200 && statusCode <= 299))
                        {
                            throw new Exception($"Update failed for one or more items: {statusParts[1]} - {partContent}");
                        }
                    }
                    else
                    {
                        throw new Exception($"Invalid response part: {partContent}");
                    }
                }
            }
        }
        public static void EnsureSuccessOrThrow(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    JsonElement errorElement;

                    // Try "error" property first, then "odata.error"
                    if (doc.RootElement.TryGetProperty("error", out errorElement) ||
                        doc.RootElement.TryGetProperty("odata.error", out errorElement))
                    {
                        string errorCode = errorElement.GetProperty("code").GetString();
                        string errorMessage = errorElement.GetProperty("message").GetProperty("value").GetString();
                        throw new Exception($"SharePoint API error (HTTP {response.StatusCode}): Code: {errorCode}, Message: {errorMessage}");
                    }
                    else
                    {
                        throw new Exception($"SharePoint API error (HTTP {response.StatusCode}): {errorContent}");
                    }
                }
                catch (JsonException)
                {
                    throw new Exception($"SharePoint API error (HTTP {response.StatusCode}): {errorContent}");
                }
            }
        }
    }
}

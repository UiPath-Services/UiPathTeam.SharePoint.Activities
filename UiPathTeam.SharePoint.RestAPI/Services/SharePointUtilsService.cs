using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Service.Helpers;

namespace UiPathTeam.SharePoint.RestAPI.Services
{
    public class SharePointUtilsService : SharePointBaseService
    {
        public SharePointUtilsService(HttpClient httpClient, string siteUrl) : base(httpClient, siteUrl)
        {
        }

        
        public async Task<TimeZoneInfo> GetSPTimeZoneAsync(string siteUrl)
        {
            string endpoint = $"{siteUrl}/_api/web/RegionalSettings/TimeZone";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            SetJsonHeaders(request);

            var response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();

            string json = await response.Content.ReadAsStringAsync();

            // Deserialize into TimeZoneResponse
            var timeZoneResponse = JsonSerializer.Deserialize<TimeZoneResponse>(json);
            // Return the TimeZoneInfo object
            if (timeZoneResponse == null || timeZoneResponse.d == null)
            {
                throw new Exception("Failed to get time zone");
            }
            return timeZoneResponse.d;

            
        }

        public async Task<User> GetCurrentUserAsync(string siteUrl)
        {
            string endpoint = $"{siteUrl}/_api/web/currentuser";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            SetJsonHeaders(request);

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            await response.EnsureSuccessOrThrowAsync();
            string jsonResult = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(jsonResult);
            var d = doc.RootElement.GetProperty("d");
            User userObj = SharePointUserService.MapUserProperties(d);
            return userObj;

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
        public class TimeZoneResponse
        {
            public TimeZoneInfo d { get; set; }
        }
    }

}

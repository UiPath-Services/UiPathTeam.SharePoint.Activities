using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharePoint.RestAPI.Services
{
    public abstract class SharePointBaseService
    {
        protected readonly HttpClient _httpClient;
        public readonly string _siteUrl;
        protected SharePointBaseService(HttpClient httpClient, string siteUrl)
        {
            _httpClient = httpClient;
            _siteUrl = siteUrl.TrimEnd('/');
        }

        override public string ToString()
        {
            return "Service site URL: " + _siteUrl;
        }

    }
}

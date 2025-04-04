using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharePoint.RestAPI.Tests
{
    public static class TestsHelpers
    {
        public static IEnumerable<Enum> GetFlags(this Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }
        public static string ResolveRelativePath(string siteUrl, string relativePath)
        {
            Uri uri = new Uri(siteUrl);
            string sitePath = uri.LocalPath;

            if (!relativePath.StartsWith("/")) relativePath = "/" + relativePath;
            if (!sitePath.StartsWith("/")) sitePath = "/" + sitePath;
            if (relativePath.StartsWith(sitePath))
            {
                return relativePath;
            }
            return string.Format("{0}/{1}", sitePath.TrimEnd('/'), relativePath.TrimStart('/'));
        }

        public static string[] GetAzureAppScopes(string siteURL, AzureAppPermissions permissionsEnum)
        {
            string[] Scopes = new String[] { "{0}/AllSites.Read", "{0}/AllSites.Write",
            "{0}/AllSites.Manage",  "{0}/AllSites.FullControl"};
        //extract tenant from site URL
        string spoTenant = ExtractTenantFromSiteURL(siteURL);
            //our scopes are represented by powers of 2: 1,2,4,....; we need the log function to convert our enum to an index for the Scopes Array
            int[] scopeIndexes = permissionsEnum.GetFlags().Where(x => (int)(AzureAppPermissions)x > 0).
                Select(x => (int)Math.Log((int)(AzureAppPermissions)x, 2)).ToArray();
            //get the raw scopes and substitute {0} with the tenant
            string[] scopes = scopeIndexes.Select(x => String.Format(Scopes[x], spoTenant)).ToArray();
            return scopes;
        }

        public static string ExtractTenantFromSiteURL(string siteURL)
        {
            var spoTenant = siteURL.Substring(0, siteURL.IndexOf('/', 8));
            return spoTenant;
        }

        
    }

    public enum AzureAppPermissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Manage = 4,
        FullControl = 8
    }
}

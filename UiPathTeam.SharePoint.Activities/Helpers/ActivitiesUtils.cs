using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharePoint.Activities.Helpers
{
    public static class ActivitiesUtils
    {
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }
        //add the relative site url to the begining of a relative URL

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
    }
}

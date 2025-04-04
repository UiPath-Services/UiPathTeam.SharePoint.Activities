using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharePoint.Activities.Helpers
{

    public static class WebBrowserHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct INTERNET_CACHE_ENTRY_INFOA
        {
            public uint dwStructSize;
            public IntPtr lpszSourceUrlName;
            public IntPtr lpszLocalFileName;
            public uint CacheEntryType;
            public uint dwUseCount;
            public uint dwHitRate;
            public uint dwSizeLow;
            public uint dwSizeHigh;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastModifiedTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ExpireTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastSyncTime;
            public IntPtr lpHeaderInfo;
            public uint dwHeaderInfoSize;
            public IntPtr lpszFileExtension;
            public uint dwReserved;
            public uint dwExemptDelta;
        }

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindFirstUrlCacheEntry(
            string lpszUrlSearchPattern,
            IntPtr lpFirstCacheEntryInfo,
            ref int lpdwFirstCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool FindNextUrlCacheEntry(
            IntPtr hEnumHandle,
            IntPtr lpNextCacheEntryInfo,
            ref int lpdwNextCacheEntryInfoBufferSize);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeleteUrlCacheEntry(string lpszUrlName);

        public static List<string> SharepointCookies(string url, bool delete = false)
        {
            List<string> result = new();
            string hostEntry = new Uri(url).Host;

            const int ERROR_NO_MORE_ITEMS = 259;

            int size = 0;
            IntPtr enumHandle = FindFirstUrlCacheEntry(null, IntPtr.Zero, ref size);
            if (enumHandle == IntPtr.Zero && Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
                return result;

            IntPtr buffer = Marshal.AllocHGlobal(size);
            enumHandle = FindFirstUrlCacheEntry(null, buffer, ref size);

            while (true)
            {
                var entry = Marshal.PtrToStructure<INTERNET_CACHE_ENTRY_INFOA>(buffer);
                string sourceUrl = Marshal.PtrToStringAuto(entry.lpszSourceUrlName);

                if (!string.IsNullOrEmpty(sourceUrl) &&
                    (sourceUrl.Contains(hostEntry, StringComparison.OrdinalIgnoreCase) ||
                    (sourceUrl.Contains("cookie", StringComparison.OrdinalIgnoreCase) &&
                     sourceUrl.Contains("sharepoint", StringComparison.OrdinalIgnoreCase))))
                {
                    result.Add(sourceUrl);
                    if (delete)
                        DeleteUrlCacheEntry(sourceUrl);
                }

                int nextSize = size;
                bool hasNext = FindNextUrlCacheEntry(enumHandle, buffer, ref nextSize);

                if (!hasNext)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err == ERROR_NO_MORE_ITEMS)
                        break;

                    if (nextSize > size)
                    {
                        buffer = Marshal.ReAllocHGlobal(buffer, (IntPtr)nextSize);
                        size = nextSize;
                        hasNext = FindNextUrlCacheEntry(enumHandle, buffer, ref nextSize);
                    }

                    if (!hasNext)
                        break;
                }
            }

            Marshal.FreeHGlobal(buffer);
            return result;
        }

        public static async Task SharePointSignOutAsync(string Url)
        {

            /*Steps: 
             * 1. Delete any cookies and cache that might have been created by a previous login (only those for our sharepoint site)
             * to make sure that the previous signed in user is forgotten, thus forcing the need to introduce the credentials for a new user
             * 2. Navigate to the official URL that logs out the current user (this step is not enough for the sign out, as after several logins,
             * the cache created locally will impede the proper and complete logout, so we have to delete the sharepoint cache.
             * 3. Delete the cookies created by the logout URL
             */


            /* 
           * Changes internet options!! No cache is stored for IE
           * var ptr = Marshal.AllocHGlobal(4);
           Marshal.WriteInt32(ptr, 3);
           InternetSetOption(IntPtr.Zero, 81, ptr, 4);
           Marshal.Release(ptr);
           */

            if (!string.IsNullOrEmpty(Url))
            {
                //delete parameter set to true for deletion of the cookies
                WebBrowserHelper.SharepointCookies(Url, true);
                var siteUri = new Uri("https://login.microsoftonline.com/common/oauth2/logout");
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://login.microsoftonline.com/common/oauth2/logout");
                    response.EnsureSuccessStatusCode();
                }
                //delete parameter set to true for deletion of the cookies
                WebBrowserHelper.SharepointCookies("https://login.microsoftonline.com/common/oauth2/logout", true);
            }
            else
                throw new Exception("The URL of the SP site was not provided!");

        }

    }

}



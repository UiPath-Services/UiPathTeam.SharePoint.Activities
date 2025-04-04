using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;


namespace UiPathTeam.SharePoint
{
    public delegate bool SPActivityTypeValidator(Activity activity);

    public enum SharePointType { AppOnly, OnPremises, Online, WebLogin, AzureApp };

    public enum SharePointPlatformType { Online, Server };

    //public enum ListType { List, Library };

    [Flags]
    public enum AzureAppPermissions
    {
        None = 0,
        Read = 1,
        Write = 2,
        Manage = 4,
        FullControl = 8
    }
    
    public static class Utils
    {
        public static SharePointContextInfo GetSPContextInfo(CodeActivityContext Context)
        {
            var property = Context.DataContext.GetProperties()[SharePointContextInfo.Tag];
            var ctx = property.GetValue(Context.DataContext) as SharePointContextInfo;
            return ctx;
        }

        public static string[] Scopes = new String[] { "{0}/AllSites.Read", "{0}/AllSites.Write",
            "{0}/AllSites.Manage",  "{0}/AllSites.FullControl"};
        public static string[] GetAzureAppScopes(string siteURL, AzureAppPermissions permissionsEnum)
        {
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
        public static IEnumerable<Enum> GetFlags(this Enum input)
        {
            foreach (Enum value in Enum.GetValues(input.GetType()))
                if (input.HasFlag(value))
                    yield return value;
        }

        public static bool IsLiteral(object inputObject)
        {
            if (inputObject == null || !(inputObject is InArgument<string> inArgument))
            {
                return false;
            }

            Activity<string> expression = inArgument.Expression;
            VisualBasicValue<string> visualBasicValue = expression as VisualBasicValue<string>;
            return expression is Literal<string> && visualBasicValue == null;
        }
        public static string TrimFilePath(string initialPath, string absolutePath)
        {
            if (initialPath.StartsWith(absolutePath))
            {
                return initialPath.Remove(0, absolutePath.Length).TrimStart('\\');
            }

            return initialPath;
        }

        public static void ValidateLibraryResourcePath(string path, bool allowASPXFileOperations)
        {
            //we will throw an exception if our file is an ASPX file but allowASPXFileOperations is false
            if (path.ToLower().EndsWith(".aspx") && !allowASPXFileOperations)
                throw new Exception("You cannot operate on files ending with '.aspx' if the Allow ASPX File Operation checkbox is unchecked.");
        }
    }
}

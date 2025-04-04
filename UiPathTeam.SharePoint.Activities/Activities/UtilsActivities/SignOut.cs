using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;

namespace UiPathTeam.SharePoint.Activities
{
    [Description("Use it only in case you are signed in to a WebLogin SharePoint Instance! Signs out the current user in order to enter new credentials (in case of MFA, the user might remain logged in for some time; if we need to log in a different user, we have to sign out the current one")]
    public class SignOut : SPUrlOnlyCodeActivity
    {
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string siteURL = URL.Get(context);

            var task = WebBrowserHelper.SharePointSignOutAsync(siteURL);
            
            return task.ToAsyncResult(callback, state); 

        }

        

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }

        //protected override void Execute(CodeActivityContext context)
        //{

        //    WorkflowDataContext dc = context.DataContext;
        //    string siteURL = URL.Get(context);

        //    if (string.IsNullOrEmpty(siteURL))
        //    {
        //        SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //        siteURL = customContext.Url;
        //        //reset the current client context
        //        customContext.currentClientContext = null;

        //    }
        //    Utils.SharePointSignOut(siteURL);

        //}
    }
}

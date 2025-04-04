using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities
{
    [Description("Gets the current user only in case you are signed in to a WebLogin SharePoint Instance")]
    public class GetWebLoginUser : SPUrlOnlyCodeActivity
    {
        [Category("Output")]
        [Description("Full user information")]
        //public OutArgument<User> SharePointUser { get; set; }
        public OutArgument<User> SharePointUser { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }
        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;
        //    string siteURL = URL.Get(context);
        //    SharePointContextInfo customContext;

        //    try

        //    {
        //        //we presume the activty is inside a SP App Scope, so we don't need to login again to get the current user
        //        customContext = Utils.GetSPContextInfo(context);

        //    }
        //    catch (Exception)
        //    {
        //        //in case the activity is not inside a SP App Scope 
        //        customContext = new SharePointContextInfo()
        //        {
        //            Url = siteURL,
        //            SharePointInstanceType = SharePointType.WebLogin
        //        };
        //    }
        //    SharePointUser.Set(context, Utils.GetCurrentWebLoginUser(customContext));
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string siteURL = URL.Get(context);
            SharePointContextInfo spContext;
            
            try
            {
                spContext = Utils.GetSPContextInfo(context);
                
            }
            catch (Exception)
            {
                spContext = new SharePointContextInfo()
                {
                    Url = siteURL,
                    SharePointInstanceType = SharePointType.WebLogin
                };
            }
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointUtilsService(httpClient, spContext.Url);
            //UiPathTeam.SharePoint.RestAPI.Services.RoleType _permissionsToGive  = (UiPathTeam.SharePoint.RestAPI.Services.RoleType)Enum.Parse(typeof(UiPathTeam.SharePoint.RestAPI.Services.RoleType), PermissionToGive.ToString());
            var task = service.GetCurrentUserAsync(siteURL);

            return task.ToAsyncResult(callback, state);
        }

        

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<User>)result;
            var resultUser = task.Result;

            //var spContext = Utils.GetSPContextInfo(context);

            //if (!spContext.groupQueries)
            //{
            SharePointUser.Set(context, resultUser);
        }

        
    }
}

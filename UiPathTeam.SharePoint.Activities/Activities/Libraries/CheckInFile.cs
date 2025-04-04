using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Libraries
{
    [Description("An activity that checks in a file located at a specified url, the check in will happen under the account specified in the SP Application Scope")]
    public class CheckInFile : SharePointCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("The complete url of the File")]
        public InArgument<string> RelativeUrl { get; set; }

        public CheckInFile() : base(false)
        {
            ShowRelativeUrl = true;
        }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext clientContext = customContext.GetSharePointContext();

        //    //initialize input arguments
        //    string relativeUrl = Utils.ResolveRelativePath(clientContext, RelativeUrl.Get(context));

        //    Microsoft.SharePoint.Client.File file = Utils.GetFileByUrl(clientContext, relativeUrl);

        //    //if checkin is enabled, we will check in the file
        //    Utils.CheckInFile(clientContext, file);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var relativeUrl = RelativeUrl.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);
            
            relativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, relativeUrl);
            var task = service.CheckInFileAsync(relativeUrl);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

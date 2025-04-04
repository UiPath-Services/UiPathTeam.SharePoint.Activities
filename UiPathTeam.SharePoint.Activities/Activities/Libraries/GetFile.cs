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
    [Description("An activity that downloads a file from a specified url, into the mentioned local path")]
    public class GetFile : SharePointCodeActivity
    {

        [Category("Input")]
        [Description("Local path where the file will be saved. If empty, the file will be saved in the projects Root Folder")]
        public InArgument<string> LocalPath { get; set; }


        [Category("Input")]
        [RequiredArgument]
        [Description("The complete url of the file which will be saved")]
        public InArgument<string> RelativeUrl { get; set; }

        public GetFile()
        {
            ShowRelativeUrl = true;
            ShowLocalPath = true;
            LocalPathHintText = "Local path where the file will be saved";
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext clientContext = customContext.GetSharePointContext();

        //    //initialize input arguments
        //    string relativeUrl = Utils.ResolveRelativePath(clientContext, RelativeUrl.Get(context));
        //    string localPath = LocalPath.Get(context);

        //    //download file from the server to a local Path
        //    Utils.GetFileFromSharePoint(clientContext, localPath, relativeUrl);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var relativeUrl = RelativeUrl.Get(context);
            string localPath = LocalPath.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);

            relativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, relativeUrl);
            var task = service.GetFileAsync(localPath, relativeUrl);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

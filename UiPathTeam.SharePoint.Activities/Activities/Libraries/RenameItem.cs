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
    [Description("An activity that changes the name of a file of folder from a specified url")]
    public class RenameItem : SharePointCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("The complete url of the file/folder that will be renamed")]
        public InArgument<string> RelativeUrl { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("The new name of the file/folder")]
        public InArgument<string> NewName { get; set; }

        public RenameItem()
        {
            ShowRelativeUrl = true;
            ShowRename = true;

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
        //    string newName = NewName.Get(context);

        //    //rename the file/folder from the server
        //    Utils.RenameItemInSharePoint(clientContext, relativeUrl, newName);

        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var relativeUrl = RelativeUrl.Get(context);
            string newName = NewName.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);

            relativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, relativeUrl);
            var task = service.RenameItemAsync(relativeUrl, newName);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
            //throw new NotImplementedException();
        }
    }
}

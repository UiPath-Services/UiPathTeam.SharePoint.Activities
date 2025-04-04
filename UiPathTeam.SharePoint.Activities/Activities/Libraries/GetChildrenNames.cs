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
    [Description("An activity that returns an array with the direct children names (folders and files) of a specified folder")]
    public class GetChildrenNames : SharePointCodeActivity
    {
        [Category("Input")]
        [Description("The complete url of the parent folder")]
        [RequiredArgument]
        public InArgument<string> RelativeUrl { get; set; }

        [Category("Output")]
        public OutArgument<String[]> ChildrenNames { get; set; }


        public GetChildrenNames()
        {
            ShowRelativeUrl = true;
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

        //    //get the names of all the direct subfolders and files inside the folder
        //    ChildrenNames.Set(context, Utils.GetFolderChildrenNamesInSharePoint(clientContext, relativeUrl));
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var relativeUrl = RelativeUrl.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);
            
            relativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, relativeUrl);
            var task = service.GetChildrenNamesAsync(relativeUrl);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<string[]>)result;
            string[] childrenNames = task.Result;

            //var spContext = Utils.GetSPContextInfo(context);

            //if (!spContext.groupQueries)
            //{
            ChildrenNames.Set(context, childrenNames);
        }
    }
}

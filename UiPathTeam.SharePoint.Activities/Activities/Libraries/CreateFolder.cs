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
    [Description("An activity that adds a new folder in a Library, at the specified path")]
    public class CreateFolder : SharePointCodeActivity
    {

        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the library where the folder is created")]
        public InArgument<string> LibraryName { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("The complete url (relative to the Library) of the new folder")]
        public InArgument<string> RelativeUrl { get; set; }


        public CreateFolder()
        {
            ShowLibraryName = true;
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
        //    string relativeUrl = RelativeUrl.Get(context);
        //    string listTitle = LibraryName.Get(context);

        //    if (String.IsNullOrWhiteSpace(listTitle) || String.IsNullOrWhiteSpace(relativeUrl)) throw new Exception("Relative URL or Library Name cannot be empty");

        //    //create the folder to the server
        //    Utils.CreateFolderInSharePoint(clientContext, listTitle, relativeUrl);

        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string relativeUrl = RelativeUrl.Get(context);
            string listTitle = LibraryName.Get(context);

            if (String.IsNullOrWhiteSpace(listTitle) || String.IsNullOrWhiteSpace(relativeUrl)) throw new Exception("Relative URL or Library Name cannot be empty");

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);
            var task = service.CreateFolderAsync(listTitle, relativeUrl);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any

        }
    }
}

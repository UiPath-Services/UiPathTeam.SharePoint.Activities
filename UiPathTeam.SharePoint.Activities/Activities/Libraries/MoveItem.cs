using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.Activities.Libraries;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Libraries
{
    [Description("An activity that moves a file or folder from a specified url to another")]
    public class MoveItem : CreateFile
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("The complete url of the file/folder that will be moved")]
        public InArgument<string> RelativeUrl { get; set; }

        [Category("Input")]
        [Description("The complete url where the file/folder will be moved")]
        [RequiredArgument]
        public InArgument<string> DestinationRelativeUrl { get; set; }


        private new bool CheckOutFileBeforeOverwrite { get; set; }
        private new bool CheckInFileAfterCreation { get; set; }

        public MoveItem()
        {

            ShowRelativeUrl = true;
            ShowMove = true;

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
        //    string sourceRelativeUrl = Utils.ResolveRelativePath(clientContext, RelativeUrl.Get(context));
        //    string destinationRelativeUrl = Utils.ResolveRelativePath(clientContext, DestinationRelativeUrl.Get(context));

        //    if (String.IsNullOrWhiteSpace(destinationRelativeUrl) || String.IsNullOrWhiteSpace(sourceRelativeUrl)) throw new Exception("The source and destination file paths cannot be empty");

        //    //rename the file/folder from the server
        //    Utils.MoveFileInSharePoint(clientContext, sourceRelativeUrl, destinationRelativeUrl, AllowOverwrite);

        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var sourceRelativeUrl = RelativeUrl.Get(context);
            var destinationRelativeUrl = DestinationRelativeUrl.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);

            sourceRelativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, sourceRelativeUrl);
            destinationRelativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, destinationRelativeUrl);

            if (String.IsNullOrWhiteSpace(destinationRelativeUrl) || String.IsNullOrWhiteSpace(sourceRelativeUrl)) throw new Exception("The source and destination file paths cannot be empty");


            var task = service.MoveItemAsync(sourceRelativeUrl, destinationRelativeUrl, AllowOverwrite);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

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
    [Description("An activity that can be used to delete any file or folder in the site")]
    public class Delete : SharePointCodeActivity
    {

        [Category("Input")]
        [RequiredArgument]
        [Description("The complete url of the file/folder that will be deleted(relative to the library)")]
        public InArgument<string> RelativeUrl { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the library where the folder/file is located")]
        public InArgument<string> LibraryName { get; set; }

        [Category("Input")]
        [Description("The activities will throw an exception when deleting/overwriting aspx files unless this option is selected")]
        public bool AllowOperationsOnASPXFiles { get; set; }

        public Delete()
        {
            ShowRelativeUrl = true;
            ShowLibraryName = true;
        }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

        }


        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            //initialize input arguments
            string libraryName = LibraryName.Get(context);
            string relativeUrl = RelativeUrl.Get(context);

            if (String.IsNullOrWhiteSpace(libraryName) || String.IsNullOrWhiteSpace(relativeUrl)) throw new Exception("Relative URL or Library Name cannot be empty");
            
            //relativeUrl = String.Format("{0}/{1}", libraryName.TrimEnd("/".ToCharArray()), relativeUrl.TrimStart("/".ToCharArray()));

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);
            
            relativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, relativeUrl);
            Utils.ValidateLibraryResourcePath(relativeUrl, AllowOperationsOnASPXFiles);

            var task = service.DeleteAsync(libraryName, relativeUrl, AllowOperationsOnASPXFiles);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

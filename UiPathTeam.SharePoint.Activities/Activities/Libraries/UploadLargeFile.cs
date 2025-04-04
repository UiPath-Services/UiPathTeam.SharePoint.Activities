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
    [Description("An activity that uploads a file to a specified url, from the mentioned local path")]
    public sealed class UploadLargeFile : CreateFile
    {

        [Category("Input")]
        [RequiredArgument]
        [Description("The complete url where the file will be uploaded and its name")]
        public InArgument<string> RelativeUrl { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("The current local path and name of the file to upload")]
        public InArgument<string> LocalPath { get; set; }

        [Category("Input")]
        [Description("A dictionary containing the properties used to add to the file. For each KeyValuePair, the string will be the name of the field to update and the object its value")]
        public InArgument<Dictionary<string, object>> PropertiesToAdd { get; set; }

        [Category("Input")]
        [Description("The activities will throw an exception when deleting/overwriting aspx files unless this option is selected")]
        public bool AllowOperationsOnASPXFiles { get; set; }

        public UploadLargeFile() : base(false)
        {
            ShowRelativeUrl = true;
            ShowLocalPath = true;
            ChooseFile = true;
            LocalPathHintText = "The current local path of the file";

            //only checking if there is an ancestor who is a SPScope
            base.Constraints.Add(CheckParent(SPScopeTypeCompatibleWithLogin, "Upload large files is not possible with this combination of platform type and authentication type"));
        }

        public static bool SPScopeTypeCompatibleWithLogin(Activity activity)
        {
            //in addition to checking the parent type, also check the 
            return CheckIfActivityIsSPScope(activity)
               && !(((SharepointApplicationScope)activity).SharePointInstanceType == SharePointType.AppOnly &&
               ((SharepointApplicationScope)activity).PlatformType == SharePointPlatformType.Server);
        }

        //// If your activity returns a value, derive from CodeActivity<TResult>
        //// and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext clientContext = customContext.GetSharePointContext();


        //    //initialize input arguments
        //    string relativeUrl = Utils.ResolveRelativePath(clientContext, RelativeUrl.Get(context));
        //    string localpath = LocalPath.Get(context);

        //    Dictionary<string, object> propertiesDictionary = PropertiesToAdd.Get(context);

        //    //if the relative url is a folder, we must append the file name to it
        //    if (!Path.GetFileName(relativeUrl).Contains("."))
        //    {
        //        relativeUrl = String.Format("{0}/{1}", relativeUrl.TrimEnd('/'), Path.GetFileName(localpath));

        //    }

        //    //this throws exceptions if the robot is trying by mistake to upload/work with ASPX files
        //    Utils.ValidateLibraryResourcePath(relativeUrl, AllowOperationsOnASPXFiles);

        //    if (CheckOutFileBeforeOverwrite)
        //    {
        //        try
        //        {
        //            //we check out the old file before we overwrite it
        //            Utils.CheckOutFile(clientContext, relativeUrl);
        //        }
        //        catch (ServerException) { }
        //        catch (Exception)
        //        {
        //            throw;
        //        }

        //    }

        //    //Upload a file to the server from a local Path
        //    Microsoft.SharePoint.Client.File uploadedFile = Utils.UploadLargeFile(clientContext, relativeUrl, localpath, propertiesDictionary, customContext.SharePointPlatformType, AllowOverwrite, customContext.groupQueries);


        //    if (CheckInFileAfterCreation && uploadedFile.CheckOutType != CheckOutType.None)
        //    {
        //        //if checkin is enabled, we will check in the file
        //        Utils.CheckInFile(clientContext, uploadedFile);
        //    }
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var relativeUrl = RelativeUrl.Get(context);
            string localpath = LocalPath.Get(context);
            Dictionary<string, object> propertiesDictionary = PropertiesToAdd.Get(context);


            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointLibraryService(httpClient, spContext.Url);

            relativeUrl = ActivitiesUtils.ResolveRelativePath(spContext.Url, relativeUrl);

            //if the relative url is a folder, we must append the file name to it
            if (!Path.GetFileName(relativeUrl).Contains("."))
            {
                relativeUrl = String.Format("{0}/{1}", relativeUrl.TrimEnd('/'), Path.GetFileName(localpath));

            }
            Utils.ValidateLibraryResourcePath(relativeUrl, AllowOperationsOnASPXFiles);

            var task = service.UploadLargeFileAsync(relativeUrl, localpath, propertiesDictionary, AllowOperationsOnASPXFiles, AllowOverwrite, checkOutFileBeforeOverwrite: CheckOutFileBeforeOverwrite, checkInFileAfterCreation: CheckInFileAfterCreation);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

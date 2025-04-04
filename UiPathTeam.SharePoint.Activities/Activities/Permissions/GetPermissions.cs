using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.Activities.Permissions;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Permissions
{
    /* an activity that gets all the existing permissions a list/library, folder or even the site has */
    [Description("Gets all existing permissions a list/library, folder or even the SharePoint site has")]
    public sealed class GetPermissions : PermissionsCodeActivity
    {
        [Category("Input")]
        [Description("The name of the List/Library for which we'll read the permissions")]
        public override InArgument<string> ListName { get; set; }

        [Category("Input")]
        [Description("The folder inside the list/library for which we will read the permissions. If empty, the permission will be applied to the list itself/")]
        public override InArgument<string> FolderPath { get; set; }

        [Category("Output")]
        [Description("The first value of each item contains the full Login Name or Group name. The second value contains the actual permission level.")]
        public OutArgument<List<Tuple<string, string>>> Result { get; set; }

        public GetPermissions() : base(false)
        {
            ShowListName = false;
            ShowPropertiesDictionary = false;
            ShowCAMLQuery = false;
        }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;

        //    string folderPath = context.GetValue(FolderPath);
        //    string listName = context.GetValue(ListName);



        //    //get  context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();

        //    List<Tuple<string, string>> permissionsList = Utils.GetAllPermissionsFromSPObject(spContext, ListType, listName, folderPath);
        //    Result.Set(context, permissionsList);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string folderPath = context.GetValue(FolderPath);
            string listName = context.GetValue(ListName);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointPermissionService(httpClient, spContext.Url);
            UiPathTeam.SharePoint.RestAPI.Services.ListType _listType = (UiPathTeam.SharePoint.RestAPI.Services.ListType)Enum.Parse(typeof(UiPathTeam.SharePoint.RestAPI.Services.ListType), ListType.ToString());

            var task = service.GetAllPermissionsAsync(listName, folderPath, _listType);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<List<Tuple<string, string>>>)result;
            var resultsPermissions = task.Result;

            //var spContext = Utils.GetSPContextInfo(context);

            //if (!spContext.groupQueries)
            //{
            Result.Set(context, resultsPermissions);
        }
    }
}

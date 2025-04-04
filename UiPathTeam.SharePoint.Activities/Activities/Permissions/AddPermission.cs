using Microsoft.SharePoint.Client;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Permissions
{
    [Description("Gives a group an additional permission level on a list/library or children folder")]
    public sealed class AddPermission : PermissionsCodeActivity
    {

        [Category("Input")]
        [Description("the name of the list/library we want to assign permissions to. If this is left empty, the permissions will be assigned to the SharePoint Site instead")]
        public override InArgument<string> ListName { get; set; }

        [Category("Assignee Info")]
        [RequiredArgument]
        [Description("The name of the user/group that'll receive the permission")]
        [DisplayName("User/Group")]
        public InArgument<string> Receiver { get; set; }

        [Category("Assignee Info")]
        [Description("Select it if the assignee is a User")]
        [DisplayName("Is User")]
        public bool IsUser { get; set; }

        [Category("Input")]
        [RequiredArgument]
        public RestAPI.Services.RoleType PermissionToGive { get; set; }

        [Category("Input")]
        [Description("The folder inside the list/library which should receive the permission.If empty, the permission will be applied to the list itself/")]
        public override InArgument<string> FolderPath { get; set; }

        public AddPermission() : base(true)
        {
            ShowListName = true;
            ShowPermissionDropdown = true;
        }



        //// If your activity returns a value, derive from CodeActivity<TResult>
        //// and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;

        //    //initialize input arguments
        //    string listname = ListName.Get(context);
        //    string receiver = Receiver.Get(context);
        //    string folderPath = FolderPath.Get(context);

        //    //get  context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();

        //    //add the permission
        //    Utils.AddPermission(spContext, receiver, IsUser, ListType, PermissionToGive, listname, folderPath, customContext.groupQueries);

        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            //Debugger.Launch();
            string listname = ListName.Get(context);
            string receiver = Receiver.Get(context);
            string folderPath = FolderPath.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointPermissionService(httpClient, spContext.Url);
            UiPathTeam.SharePoint.RestAPI.Services.ListType _listType = (UiPathTeam.SharePoint.RestAPI.Services.ListType)Enum.Parse(typeof(UiPathTeam.SharePoint.RestAPI.Services.ListType), ListType.ToString());
            //UiPathTeam.SharePoint.RestAPI.Services.RoleType _permissionsToGive  = (UiPathTeam.SharePoint.RestAPI.Services.RoleType)Enum.Parse(typeof(UiPathTeam.SharePoint.RestAPI.Services.RoleType), PermissionToGive.ToString());
            var task = service.AddPermissionAsync(listname, receiver, IsUser, PermissionToGive, folderPath, _listType);

            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any

        }
    }
}

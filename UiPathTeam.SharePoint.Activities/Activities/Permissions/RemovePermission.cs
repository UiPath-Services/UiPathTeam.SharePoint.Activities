using System;
using System.Activities.Validation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Permissions;
using UiPathTeam.SharePoint.RestAPI.Services;
using UiPathTeam.SharePoint.Activities.Helpers;

namespace UiPathTeam.SharePoint.Activities.Permissions
{
    [Description("Remove all the permissions a group has on the given list/library")]
    public sealed class RemovePermission : PermissionsCodeActivity
    {
        [Category("Input")]
        [Description("the name of the list/library we want to delete permissions from. If this is left empty, the permissions will be removed from the SharePoint Site instead")]
        public override InArgument<string> ListName { get; set; }

        [Category("Input")]
        [Description("The folder inside the list/library which should lose the permission. If empty, the permission will be applied to the list itself")]
        public override InArgument<string> FolderPath { get; set; }

        [Category("Deposed Info")]
        [RequiredArgument]
        [Description("The name of the user/group that'll lose the permission.")]
        [DisplayName("User/Group")]
        public InArgument<string> Receiver { get; set; }

        [Category("Deposed Info")]
        [Description("Select it if the deposed entity is a User")]
        [DisplayName("Is User")]
        public bool IsUser { get; set; }

        public RemovePermission() : base(true)
        {
            ShowListName = true;
            ShowPermissionDropdown = false;
        }
        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {


            //performing validation checks
            if (ListName == null && FolderPath != null)
            {
                ValidationError error = new ValidationError("The FolderPath argument can be used only if the ListName is not null");
                metadata.AddValidationError(error);
            }

            base.CacheMetadata(metadata);

        }
        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
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
        //    Utils.RemovePermission(spContext, receiver, IsUser, ListType, listname, folderPath, customContext.groupQueries);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string listname = ListName.Get(context);
            string receiver = Receiver.Get(context);
            string folderPath = FolderPath.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointPermissionService(httpClient, spContext.Url);
            UiPathTeam.SharePoint.RestAPI.Services.ListType _listType = (UiPathTeam.SharePoint.RestAPI.Services.ListType)Enum.Parse(typeof(UiPathTeam.SharePoint.RestAPI.Services.ListType), ListType.ToString());
            //UiPathTeam.SharePoint.RestAPI.Services.RoleType _permissionsToGive  = (UiPathTeam.SharePoint.RestAPI.Services.RoleType)Enum.Parse(typeof(UiPathTeam.SharePoint.RestAPI.Services.RoleType), PermissionToGive.ToString());
            var task = service.RemovePermissionAsync(listname, folderPath, _listType, receiver, IsUser);

            return task.ToAsyncResult(callback, state);

        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any

        }
    }
}

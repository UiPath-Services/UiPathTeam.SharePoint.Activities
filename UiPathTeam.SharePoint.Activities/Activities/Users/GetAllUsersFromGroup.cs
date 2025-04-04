using Microsoft.SharePoint.Client;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Users
{
    [Description("Gets all the users inside a group as a List of Microsoft.SharePoint.User objects  ")]
    public sealed class GetAllUsersFromGroup : SharePointCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> GroupName { get; set; }

        [Category("Output")]
        public OutArgument<List<User>> Result { get; set; }

        public GetAllUsersFromGroup() : base(false)
        {
            ShowUserName = false;
            ShowGroupName = true;
            ShowGroupDescription = false;
        }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;

        //    // Obtain the runtime value of the Text input argument
        //    string groupName = context.GetValue(GroupName);

        //    //get  context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();
        //    List<User> users = Utils.GetUsersFromGroup(spContext, groupName);
        //    Result.Set(context, users);

        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string groupName = context.GetValue(GroupName);
            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointUserService(httpClient, spContext.Url);

            var task = service.GetAllUsersFromGroupAsync(groupName);
            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<List<User>>)result;
            var ListOfUsers = task.Result;

            var spContext = Utils.GetSPContextInfo(context);

            
            Result.Set(context, ListOfUsers);
        }
    }
}

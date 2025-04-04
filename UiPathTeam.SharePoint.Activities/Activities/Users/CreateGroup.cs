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
    public class CreateGroup : SharePointCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> GroupName { get; set; }

        [Category("Input")]
        [Description("The description of the newly created group")]
        public InArgument<string> GroupDescription { get; set; }

        public CreateGroup() : base(true)
        {

            ShowGroupName = true;
            ShowUserName = false;
            ShowGroupDescription = false;
        }

        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;

        //    //initialize input arguments
        //    string groupName = GroupName.Get(context);
        //    string groupDescription = GroupDescription.Get(context);

        //    //get  context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();

        //    //remove the user from the group
        //    Utils.CreateNewGroup(spContext, groupName, groupDescription, customContext.groupQueries);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string groupName = GroupName.Get(context);
            string groupDescription = GroupDescription.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointUserService(httpClient, spContext.Url);

            var task = service.CreateUserGroupAsync(groupName, groupDescription);
            return task.ToAsyncResult(callback, state);
            //return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

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
    public class RemoveUserFromGroup : SharePointCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the group where the user should be removed from")]
        public InArgument<string> GroupName { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("String representing either the domain/username or the email of the user that needs to be removed from the group")]
        public InArgument<string> User { get; set; }

        public RemoveUserFromGroup() : base(true)
        {
            ShowUserName = true;
            ShowGroupName = true;
        }

        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;

        //    //initialize input arguments
        //    string groupName = GroupName.Get(context);
        //    string userIdentification = User.Get(context);

        //    //get  context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();

        //    //remove the user from the group
        //    Utils.RemoveUserFromGroup(spContext, userIdentification, groupName, customContext.groupQueries);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string groupName = GroupName.Get(context);
            string userIdentification = User.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointUserService(httpClient, spContext.Url);

            var task = service.RemoveUserFromGroupAsync(groupName, userIdentification);
            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any

        }
    }
}

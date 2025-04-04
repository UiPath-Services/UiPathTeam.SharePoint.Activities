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
    public class DeleteGroup : SharePointCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        public InArgument<string> GroupName { get; set; }

        public DeleteGroup() : base(true)
        {
            ShowUserName = false;
            ShowGroupName = true;
        }

        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;

        //    //initialize input arguments
        //    string groupName = GroupName.Get(context);

        //    //get  context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();

        //    //execute query
        //    Utils.DeleteGroup(spContext, groupName, customContext.groupQueries);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string groupName = GroupName.Get(context);
            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointUserService(httpClient, spContext.Url);

            var task = service.RemoveGroupAsync(groupName);
            return task.ToAsyncResult(callback, state);
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

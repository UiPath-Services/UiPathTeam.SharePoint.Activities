using Microsoft.SharePoint.Client;
using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Users
{
    [Description("Helps you search for a user in SharePoint and returns the users ID and full details")]
    public sealed class GetUser : SharePointCodeActivity
    {
        // Define an activity input argument of type string
        [Category("Input")]
        [Description("The email/name the user will be searched by")]
        [DisplayName("SearchString")]
        [RequiredArgument]
        public InArgument<string> User { get; set; }

        [Category("Output")]
        [Description("Full user information")]
        public OutArgument<User> SharePointUser { get; set; }

        [Category("Output")]
        [Description("The users's ID ")]
        public OutArgument<int> UserID { get; set; }

        public GetUser() : base(false)
        {
            ShowGroupName = false;
            ShowUserName = true;
            ShowGroupDescription = false;
        }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;
        //    string email = User.Get(context);

        //    //get  context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();

        //    User user = Utils.GetUserByEmail(spContext, email);

        //    UserID.Set(context, user.Id);
        //    SharePointUser.Set(context, user);
        //}

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string email = User.Get(context);
            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointUserService(httpClient, spContext.Url);

            var task = service.GetUserByEmailAsync(email);
            return task.ToAsyncResult(callback, state);

        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<User>)result;
            var auser = task.Result;

            var spContext = Utils.GetSPContextInfo(context);

            UserID.Set(context, auser.Id);
            SharePointUser.Set(context, auser);

        }
    }
}

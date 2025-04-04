using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Lists
{
    public sealed class GetListItemAttachments : SharePointCodeActivity
    {

        public GetListItemAttachments() : base(false)
        {
        }

        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the list containing our item")]
        public InArgument<string> ListName { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("The ID of the list item for which to retrieve the attachment names")]
        public InArgument<int> ListItemID { get; set; }

        [Category("Output")]
        [RequiredArgument]
        public OutArgument<string[]> AttachmentNames { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string listName = context.GetValue(ListName);
            int listItemID = context.GetValue(ListItemID);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointListService(httpClient, spContext.Url);

            var task = service.GetListItemAttachmentsAsync(listName, listItemID);


            // Wrap async task into IAsyncResult
            var tcs = new TaskCompletionSource<string[]>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.SetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.SetCanceled();
                else
                    tcs.SetResult(t.Result.ToArray());

                callback?.Invoke(tcs.Task);
            });

            return tcs.Task;
        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            string[] attachmentNames = ((Task<string[]>)result).GetAwaiter().GetResult();
            AttachmentNames.Set(context, attachmentNames);
        }

        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext clientContext = customContext.GetSharePointContext();

        //    // Obtain the runtime value of the Text input argument
        //    string listName = context.GetValue(ListName);
        //    int listItemID = context.GetValue(ListItemID);

        //    string[] attachmentNames = Utils.GetItemAttachments(clientContext, listName, listItemID);

        //    AttachmentNames.Set(context, attachmentNames);
        //}
    }
}

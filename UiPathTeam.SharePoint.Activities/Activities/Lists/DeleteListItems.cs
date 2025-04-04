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
    [Description("An activity that deletes all the items in a list matched by a CAML Query")]
    public class DeleteListItems : SharePointMultiQueryCodeActivity
    {

        public DeleteListItems() : base(false)
        {
            ShowListName = true;
            ShowAttachFiles = false;
            ShowPropertiesDictionary = false;
            ShowCAMLQuery = true;
            ShowCAMLWarning = true;
        }

        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the list where we will delete items from")]
        public InArgument<string> ListName { get; set; }

        [Category("Output")]
        public OutArgument<int> NumberOfRowsAffected { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string listname = ListName.Get(context);
            string camlFilter = CAMLQuery.Get(context);
            int querySize = NumberOfItemsProcessedAtOnce.Get(context);

            //throw an exception if the CAML Query is empty but the AllowOperationOnAllItems is not checked
            CheckIfEmptyQueriesAreAllowed(camlFilter);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();
            var service = new SharePointListService(httpClient, spContext.Url);

            var task = service.DeleteListItemsAsync(listname, querySize, camlFilter);

            // Wrap async task into IAsyncResult
            var tcs = new TaskCompletionSource<int>(state);
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.SetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.SetCanceled();
                else
                    tcs.SetResult(t.Result);

                callback?.Invoke(tcs.Task);
            });

            return tcs.Task;


        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<int>)result;
            int NoOFDeletedRows = task.Result;

            var spContext = Utils.GetSPContextInfo(context);

            if (!spContext.groupQueries)
            {
                NumberOfRowsAffected.Set(context, NoOFDeletedRows);
            }
        }

        
    }
}

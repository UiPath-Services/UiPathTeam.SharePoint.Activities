using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Lists
{
    [Description("An activity that returns the items inside a list matched by a CAML Query")]
    [DisplayName("Get List Items")]
    public class ReadListItems : SharePointCodeActivity
    {

        public ReadListItems() : base(false)
        {
            ShowListName = true;
            ShowAttachFiles = false;
            ShowPropertiesDictionary = false;
            ShowCAMLQuery = true;
        }


        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the list where we will read items from")]
        public InArgument<string> ListName { get; set; }

        [Category("Input")]
        [Description("Query that filters out the items that need to be returned")]
        public InArgument<string> CAMLQuery { get; set; }

        [OverloadGroup("ArrayOfDictionaries")]
        [Category("Output")]
        [DisplayName("Items(Dictionary Array)")]
        public OutArgument<Dictionary<string, object>[]> ItemsDictArray { get; set; }

        [OverloadGroup("DataTable")]
        [Category("Output")]
        [DisplayName("Items(DataTable)")]
        public OutArgument<DataTable> ItemsTable { get; set; }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            //Debugger.Launch();
            string listname = ListName.Get(context);
            string camlFilter = CAMLQuery.Get(context);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointListService(httpClient, spContext.Url);

            var task = service.ReadListItemsAsync(listname, camlFilter);

            // Wrap async task into IAsyncResult
            var tcs = new TaskCompletionSource<ReadListItemsResult>(state);
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

            ReadListItemsResult itemListResult = ((Task<ReadListItemsResult>)result).GetAwaiter().GetResult();
            
            if (ItemsDictArray.Expression != null)
                ItemsDictArray.Set(context, itemListResult.ItemsDictArray);
            else
                ItemsTable.Set(context, itemListResult.ItemsTable);

            //AttachmentNames.Set(context, attachmentNames);
        }

        

    }
}

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
    [Description("An activity that updates all the items in a list matched by a CAML Query")]
    public class UpdateListItems : SharePointMultiQueryCodeActivity
    {

        public UpdateListItems() : base(false)
        {
            ShowListName = true;
            ShowAttachFiles = false;
            ShowPropertiesDictionary = true;
            ShowCAMLQuery = true;
            ShowCAMLWarning = true;
        }

        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the list from which we will update items")]
        public InArgument<string> ListName { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("A dictionary containing the properties used to update the filtered items. For each KeyValuePair, the string will be the name of the field to update and the object its value")]
        public InArgument<Dictionary<string, object>> PropertiesToAdd { get; set; }

        [Category("Output")]
        public OutArgument<int> NumberOfRowsAffected { get; set; }

        

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string listname = ListName.Get(context);
            string camlFilter = CAMLQuery.Get(context);
            int querySize = NumberOfItemsProcessedAtOnce.Get(context);
            Dictionary<string, object> propertiesDictionary = PropertiesToAdd.Get(context);

            //throw an exception if the CAML Query is empty but the AllowOperationOnAllItems is not checked
            CheckIfEmptyQueriesAreAllowed(camlFilter);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();
            var service = new SharePointListService(httpClient, spContext.Url);

            var task = service.UpdateListItemsAsync(listname, propertiesDictionary, camlFilter, querySize);

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
            int NoOfRowsAffected = ((Task<int>)result).GetAwaiter().GetResult();
            NumberOfRowsAffected.Set(context, NoOfRowsAffected);
        }

        //protected override void Execute(CodeActivityContext context)
        //{

        //    WorkflowDataContext dc = context.DataContext;

        //    //initialize input arguments
        //    string listname = ListName.Get(context);
        //    string camlFilter = CAMLQuery.Get(context);
        //    int querySize = NumberOfItemsProcessedAtOnce.Get(context);
        //    Dictionary<string, object> propertiesDictionary = PropertiesToAdd.Get(context);

        //    //throw an exception if the CAML Query is empty but the AllowOperationOnAllItems is not checked
        //    CheckIfEmptyQueriesAreAllowed(camlFilter);

        //    //get SP context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();

        //    ListItemActivityCRUD updateItemOperaton = delegate (ListItem listItem) { Utils.UpdateItemProperties(listItem, propertiesDictionary); };

        //    //get items to update
        //    ListItemCollection listItemCollection = Utils.GetListItems(spContext, listname, camlFilter);

        //    //delete all found items in batches
        //    Utils.RunQueriesInBatch(spContext, listItemCollection, querySize, updateItemOperaton);

        //    //set result
        //    NumberOfRowsAffected.Set(context, listItemCollection.Count);
        //}
    }
}

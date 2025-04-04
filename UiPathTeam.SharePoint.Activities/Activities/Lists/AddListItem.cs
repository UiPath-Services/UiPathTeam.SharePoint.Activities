using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Lists
{
    public class AddListItem : SharePointCodeActivity
    {
        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the list where to add the Item")]
        public InArgument<string> ListName { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("A dictionary containing the properties for the new object. For each KeyValuePair, the string will be the field name and the object the value")]
        public InArgument<Dictionary<string, object>> PropertiesToAdd { get; set; }


        [Category("Output")]
        public OutArgument<int> AddedItemID { get; set; }

        public AddListItem() : base(true)
        {
            ShowListName = true;
            ShowAttachFiles = false;
            ShowPropertiesDictionary = true;
            ShowCAMLQuery = false;

        }






        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            //Debugger.Launch();

            string listName = ListName.Get(context);
            var properties = PropertiesToAdd.Get(context);
            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointListService(httpClient, spContext.Url);

            var task = service.AddListItemAsync(listName, properties);

            return task.ToAsyncResult(callback, state);
            // Wrap async task into IAsyncResult
            //var tcs = new TaskCompletionSource<int>(state);
            //task.ContinueWith(t =>
            //{
            //    if (t.IsFaulted)
            //        tcs.SetException(t.Exception.InnerExceptions);
            //    else if (t.IsCanceled)
            //        tcs.SetCanceled();
            //    else
            //        tcs.SetResult(t.Result);

            //    callback?.Invoke(tcs.Task);
            //});

            //return tcs.Task;

        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<int>)result;
            int itemId = task.Result;

            //var spContext = Utils.GetSPContextInfo(context);

            //if (!spContext.groupQueries)
            //{
                AddedItemID.Set(context, itemId);
            //}
        }



        //protected override async void Execute(CodeActivityContext context)
        //{
        //    Debugger.Launch();
        //    WorkflowDataContext dc = context.DataContext;

        //    //initialize input arguments
        //    string listname = ListName.Get(context);
        //    Dictionary<string, object> propertiesDictionary = PropertiesToAdd.Get(context);

        //    //get context
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    var httpClient = customContext.GetSharePointContext();

        //    SharePointListService _service = new SharePointListService(httpClient, customContext.Url);
        //    int addedListItemID = await _service.AddListItemAsync(listname, propertiesDictionary);

        //    //set result if the query was synchronous
        //    if (!customContext.groupQueries)
        //    {
        //        AddedItemID.Set(context, addedListItemID);
        //    }
        //}
        //protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        //{
        //    throw new NotImplementedException();
        //}

        //protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        //{
        //    throw new NotImplementedException();
        //}
    }





}

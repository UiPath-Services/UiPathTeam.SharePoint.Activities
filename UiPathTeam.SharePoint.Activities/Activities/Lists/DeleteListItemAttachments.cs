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
    public class DeleteListItemAttachments : SharePointCodeActivity
    {
        public DeleteListItemAttachments() : base(true)
        {
            ShowListName = true;
            ShowAttachFiles = true;
            AttachmentsAction = "AttachmentsToDelete";
            ShowPropertiesDictionary = false;
            ShowCAMLQuery = false;

            Attachments = new List<InArgument<string>>();
        }

        [Category("Input")]
        [RequiredArgument]
        [Description("The name of the list containing our item")]
        public InArgument<string> ListName { get; set; }

        [Category("Input")]
        [RequiredArgument]
        [Description("The ID of the list item for which we delete the attachment")]
        public InArgument<int> ListItemID { get; set; }

        [Category("Input")]
        [Description("Allows specifying a list of files to be deleted")]
        [DisplayName("AttachmentsCollection")]
        public InArgument<IEnumerable<string>> AttachmentsCollection { get; set; }

        [Category("Input")]
        [DisplayName("Attachments")]
        public List<InArgument<string>> Attachments { get; set; }

        [Category("Output")]
        public OutArgument<int> DeletedAttachmentsNr { get; set; }

        

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (Attachments == null)
            {
                Attachments = new List<InArgument<string>>();
            }
            base.CacheMetadata(metadata);
            int index = 1;
            foreach (var item in Attachments)
            {
                string name = "attachmentArg" + ++index;
                var runtimeArg = new RuntimeArgument(name, typeof(string), ArgumentDirection.In);
                metadata.Bind(item, runtimeArg);
                metadata.AddArgument(runtimeArg);
            }
        }
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            //Debugger.Launch();
            string listName = context.GetValue(ListName);
            int listItemID = context.GetValue(ListItemID);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();
            var service = new SharePointListService(httpClient, spContext.Url);

            List<string> attachmentNames = new List<string>();
            foreach (var at in Attachments)
            {
                attachmentNames.Add(at.Get(context));
            }

            attachmentNames.AddRange(AttachmentsCollection.Get(context).EmptyIfNull());

            var task = service.DeleteListItemAttachmentsAsync(listName, listItemID, attachmentNames);
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
            int itemId = task.Result;

            //var spContext = Utils.GetSPContextInfo(context);

            //if (!spContext.groupQueries)
            //{
            DeletedAttachmentsNr.Set(context, itemId);
            //}
        }

    }
}

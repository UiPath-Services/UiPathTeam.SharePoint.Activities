using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.Activities.Helpers;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Lists
{
    public class AddListItemAttachments : SharePointCodeActivity
    {
        public AddListItemAttachments() : base(true)
        {
            ShowListName = true;
            ShowAttachFiles = true;
            AttachmentsAction = "AttachFiles";
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
        [Description("The ID of the list item for which we add the attachment")]
        public InArgument<int> ListItemID { get; set; }

        [Category("Input")]
        [Description("Allows specifying a list of files to be attached")]
        [DisplayName("AttachmentsCollection")]
        public InArgument<IEnumerable<string>> AttachmentsCollection { get; set; }

        [Category("Input")]
        [DisplayName("Attachments")]
        public List<InArgument<string>> Attachments { get; set; }


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

        //protected override async void Execute(CodeActivityContext context)
        //{
        //    string listName = context.GetValue(ListName);
        //    int listItemID = context.GetValue(ListItemID);

        //    var spContext = Utils.GetSPContextInfo(context);
        //    var httpClient = spContext.GetSharePointContext();
        //    var service = new SharePointListService(httpClient, spContext.Url);

        //}
        

        private static void AddAttachments(List<string> attachments, string attPath)
        {
            //we need to verify that the attachments represent existing files
            try
            {
                string attFullPath = null;
                if (!Path.IsPathRooted(attPath))
                {
                    attFullPath = Path.Combine(Environment.CurrentDirectory, attPath);
                }
                if (System.IO.File.Exists(attFullPath))
                {
                    attachments.Add(attFullPath);
                }
                else
                {
                    attachments.Add(attPath);
                }

            }
            catch (System.Exception e)
            {
                Trace.TraceWarning(e.ToString());
            }
        }

        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            string listName = context.GetValue(ListName);
            int listItemID = context.GetValue(ListItemID);

            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();
            var service = new SharePointListService(httpClient, spContext.Url);



            //populate the list of attachments from both fields (AttachmentsCollection and Attachments)
            List<string> attachmentNames = new List<string>();
            foreach (var at in Attachments)
            {
                AddAttachments(attachmentNames, at.Get(context));
            }

            foreach (string att in AttachmentsCollection.Get(context).EmptyIfNull())
            {
                AddAttachments(attachmentNames, att);
            }

            var task = service.AddListItemAttachmentsAsync(listName, listItemID, attachmentNames);
            var taskCompletionSource = new TaskCompletionSource<object>(state);

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    taskCompletionSource.SetException(t.Exception.InnerExceptions);
                else if (t.IsCanceled)
                    taskCompletionSource.SetCanceled();
                else
                    taskCompletionSource.SetResult(null);

                callback?.Invoke(taskCompletionSource.Task);
            }, CancellationToken.None);

            return taskCompletionSource.Task;


        }

        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            ((Task)result).GetAwaiter().GetResult(); // re-throw exceptions if any
        }
    }
}

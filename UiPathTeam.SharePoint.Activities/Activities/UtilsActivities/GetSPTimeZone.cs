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

namespace UiPathTeam.SharePoint.Activities
{
    [Description("Retrieves the SharePoint Site TimeZone")]
    [DisplayName("Get TimeZone")]
    public class GetSPTimeZone : SharePointCodeActivity
    {
        [Category("Output")]
        [Description("SharePoint Site TimeZone")]
        public OutArgument<TimeZoneInfo> SharePointTimeZone { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }
        //protected override void Execute(CodeActivityContext context)
        //{
        //    WorkflowDataContext dc = context.DataContext;
        //    SharePointContextInfo customContext = Utils.GetSPContextInfo(context);
        //    ClientContext spContext = customContext.GetSharePointContext();
        //    SharePointTimeZone.Set(context, Utils.GetSPTimeZone(spContext));
        //}
        protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
        {
            var spContext = Utils.GetSPContextInfo(context);
            var httpClient = spContext.GetSharePointContext();

            var service = new SharePointUtilsService(httpClient, spContext.Url);

            var task = service.GetSPTimeZoneAsync(spContext.Url);

            return task.ToAsyncResult(callback, state);
        }

        
        protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
        {
            var task = (Task<TimeZoneInfo>)result;
            var resultTimeZone = task.Result;

            //var spContext = Utils.GetSPContextInfo(context);

            //if (!spContext.groupQueries)
            //{
            SharePointTimeZone.Set(context, resultTimeZone);
        }

        
    }
}

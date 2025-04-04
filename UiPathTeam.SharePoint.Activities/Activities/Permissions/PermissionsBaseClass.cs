using System;
using System.Activities.Validation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UiPathTeam.SharePoint.RestAPI.Services;

namespace UiPathTeam.SharePoint.Activities.Permissions
{
    public abstract class PermissionsCodeActivity : SharePointCodeActivity
    {
        public PermissionsCodeActivity(bool allowBatchQueries = false) : base(allowBatchQueries)
        { }

        public abstract InArgument<string> ListName { get; set; }
        public abstract InArgument<string> FolderPath { get; set; }

        [Category("Input")]
        [Description("The Type of list Used for this operation. The URL format for lists and libraries is different. The user needs to provide the list type in order for the operation to be performed correctly")]
        public ListType ListType { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {


            //performing validation checks
            if (ListName == null && FolderPath != null)
            {
                ValidationError error = new ValidationError("The FolderPath argument can be used only if the ListName is not null");
                metadata.AddValidationError(error);
            }

            base.CacheMetadata(metadata);

        }
    }
}

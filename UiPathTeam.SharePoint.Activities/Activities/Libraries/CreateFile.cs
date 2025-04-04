using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharePoint.Activities.Libraries
{
    public abstract class CreateFile : SharePointCodeActivity
    {
        [Category("File Creation Options")]
        [Description("Overwrite the file with the same name if this is checked")]
        public bool AllowOverwrite { get; set; }

        [Category("File Creation Options")]
        [Description("In the case of overwrite, check out the file before overwriting it. If file does not exist before upload, nothing will happen")]
        public bool CheckOutFileBeforeOverwrite { get; set; }

        [Category("File Creation Options")]
        [Description("After creation, check in the file.")]
        public bool CheckInFileAfterCreation { get; set; }


        public CreateFile(bool allowBatchQueries = false) : base(allowBatchQueries)
        {
            //make allow Overwrite enabled by default
            AllowOverwrite = true;
        }
    }
}

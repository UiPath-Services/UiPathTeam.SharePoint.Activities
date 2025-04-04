using System.Activities;
using System.Activities.DesignViewModels;
using System.ComponentModel;
using System.Security;

namespace UiPathTeam.SharePoint.Activities.ViewModels
{
    public class SharePointApplicationScopeViewModel : DesignPropertiesViewModel
    {
        ///*
        // * The result property comes from the activity's base class
        // */
        //[Browsable(false)]
        //public Designac ActivityAction<SharePointContextInfo> Body { get; set; }

        //public InArgument<string> URL { get; set; }

        //public InArgument<string> UserName { get; set; }

        //public InArgument<string> Password { get; set; }

        //public InArgument<SecureString> SecurePassword { get; set; }

        //public bool ResetCredentials { get; set; }

        //public InArgument<double> LoginTimeout { get; set; }

        //public InArgument<string> ClientId { get; set; }

        //public InArgument<string> ClientSecret { get; set; }

        //public InArgument<string> AzureApplicationID { get; set; }



        //[Category("Output")]
        //public OutArgument<ClientContext> ClientContext { get; set; }

        public SharePointApplicationScopeViewModel(IDesignServices services) : base(services)
        {
        }

        protected override void InitializeModel()
        {
            /*
             * The base call will initialize the properties of the view model with the values from the xaml or with the default values from the activity
             */
            base.InitializeModel();

            PersistValuesChangedDuringInit(); // mandatory call only when you change the values of properties during initialization
        }
    }
}

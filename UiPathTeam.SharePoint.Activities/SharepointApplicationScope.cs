using System.Activities;
using System.Activities.Statements;
using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using UiPathTeam.SharePoint.Activities.Helpers;

namespace UiPathTeam.SharePoint.Activities
{
    public class SharepointApplicationScope : NativeActivity // This base class exposes an OutArgument named Result
    {
        [Browsable(false)]
        public ActivityAction<SharePointContextInfo> Body { get; set; }

        [Category("SharePointConnection")]
        [RequiredArgument]
        public InArgument<string> URL { get; set; }

        [Category("SharePointConnection")]
        public InArgument<string> UserName { get; set; }

        [Category("SharePointConnection")]
        [OverloadGroup("PlainPassword")]
        public InArgument<string> Password { get; set; }

        [Category("SharePointConnection")]
        [OverloadGroup("SecuredPassword")]
        [PasswordPropertyText(true)]
        public InArgument<SecureString> SecurePassword { get; set; }

        [Category("SharePointConnection")]
        [DisplayName("Login Mode")]
        public SharePointType SharePointInstanceType { get; set; }

        [Category("SPWebLoginConnection")]
        [DisplayName("Reset Credentials")]
        [Description("For WebLogin: sign out the current user in order to enter new credentials (in case of MFA, the user might remain logged in for some time; if we need to log in a different user, we have to sign out the current one")]
        public bool ResetCredentials { get; set; }

        [Category("SPWebLoginConnection")]
        [DisplayName("LoginTimeout(milliseconds)")]
        [Description("For WebLogin: the amount of time to wait for the user to enter the credentials and finish the Multifactor Authentication. The default is 300000(5 minutes)")]
        public InArgument<double> LoginTimeout { get; set; }

        [Category("AppOnlyConnection ")]
        [Description("The Client Id generated for your site! Use it only when connecting using an app-only principal!")]
        public InArgument<string> ClientId { get; set; }

        [Category("AppOnlyConnection ")]
        [Description("The Client Secret generated for your site! Use it only when connecting using an app-only principal!")]
        public InArgument<string> ClientSecret { get; set; }

        [Category("AzureAppConnection")]
        [Description("The permission we can assign to our activity")]
        public AzureAppPermissions AzureAppPermissions { get; set; }

        [Category("AzureAppConnection")]
        [Description("The Client Id generated for your site! Use it only when connecting using an app-only principal!")]
        public InArgument<string> AzureApplicationID { get; set; }

        [Category("QueryGrouping")]
        public bool QueryGrouping { get; set; }

        internal static string SharePointContextInfoTag { get { return "SharePointContextInfoTag"; } }

        [Category("Output")]
        public OutArgument<Object> ClientContext { get; set; }

        [Category("SharePointConnection")]
        [Description("The SharePoint instance type")]
        public SharePointPlatformType PlatformType { get; set; }


        public SharepointApplicationScope()
        {
            AzureAppPermissions = AzureAppPermissions.None;
            SharePointInstanceType = SharePointType.Online;
            Body = new ActivityAction<SharePointContextInfo>
            {

                Argument = new DelegateInArgument<SharePointContextInfo>(SharePointContextInfoTag),
                Handler = new Sequence { DisplayName = "Do"}
            };
        }
        //private ClientContext ctx;

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            DelegateInArgument<SharePointContextInfo> delegateInArgument = new DelegateInArgument<SharePointContextInfo>() { Name = SharePointContextInfoTag };
            metadata.AddDelegate(Body);
            Body.Argument = delegateInArgument;
            metadata.AddDelegate(Body);

            if ((SharePointInstanceType.Equals(SharePointType.OnPremises) && PlatformType.Equals(SharePointPlatformType.Online)) ||
                ((SharePointInstanceType.Equals(SharePointType.Online) || SharePointInstanceType.Equals(SharePointType.AzureApp)) && PlatformType.Equals(SharePointPlatformType.Server)))
            {
                metadata.AddValidationError("This Login Mode is not compatible with this Platform Type");
            }

            // use the Password, SecurePassword and UserName fields ONLY for Online or OnPremises + force their use  when Online or OnPremises
            if (SharePointInstanceType.Equals(SharePointType.Online) || SharePointInstanceType.Equals(SharePointType.OnPremises) || SharePointInstanceType.Equals(SharePointType.AzureApp))
            {

                if ((this.Password == null && this.SecurePassword == null) || this.UserName == null)
                    metadata.AddValidationError("You have to set the credentials for this type of authentication!");

            }
            else
            {
                if (this.Password != null || this.SecurePassword != null || this.UserName != null)
                    metadata.AddValidationError("No Username/Passwowd should be set for this authentication type!");
            }


            //use the resetCredentials and LoginTimeout fields ONLY for WebLogin
            if (!SharePointInstanceType.Equals(SharePointType.WebLogin))
            {
                if (ResetCredentials == true)
                    metadata.AddValidationError("Reset Credentials should be checked only for WebLogin Authentication!");

                if (LoginTimeout != null)
                {
                    metadata.AddValidationError("LoginTimeout should be set only for WebLogin authentication!");
                }
            }

            //use the ClientId and ClientSecret fields ONLY for AppSecret + force their use  when AppSecret
            if (SharePointInstanceType.Equals(SharePointType.AppOnly))
            {
                if (this.ClientId == null || this.ClientSecret == null)
                    metadata.AddValidationError("You have to set the ClientId and ClientSecret for this login type!");
            }
            else
            {

                if (this.ClientId != null || this.ClientSecret != null)
                    metadata.AddValidationError("ClientId and ClientSecret should be set only for AppOnly SharePoint Instance Type!");
            }

            //use the AzureApplicationID and UserName and Password to 
            if (SharePointInstanceType.Equals(SharePointType.AzureApp))
            {
                if ((this.Password == null && this.SecurePassword == null) || this.UserName == null ||
                    this.AzureApplicationID == null || this.AzureAppPermissions == AzureAppPermissions.None)
                    metadata.AddValidationError("Azure Application ID, Username, Password and Azure App Permissions need to be configured for the Azure App type of login");
            }
            else
            {
                if (this.AzureApplicationID != null)
                    metadata.AddValidationError("AzureApplicationID should be set only for the Azure App authentication type!");

            }
            if(this.QueryGrouping)
            {
                metadata.AddValidationError("QueryGrouping is not supported");
            }


            base.CacheMetadata(metadata);
        }
        protected override void Execute(NativeActivityContext context)
        {
            //context.GetExecutorRuntime().LogMessage(new UiPath.Robot.Activities.Api.LogMessage()
            //{
            //    EventType = TraceEventType.Information,
            //    Message = "Executing Scope activity"
            //});
            string inputUrl = URL.Get(context);
            string inputUserName = UserName.Get(context);
            //If the password is not provided in plain, then it is provided in SecurePassword
            string inputPassword = Password.Get(context);
            if (string.IsNullOrEmpty(inputPassword))
                inputPassword = new System.Net.NetworkCredential(string.Empty, SecurePassword.Get(context)).Password;
            double loginTimeout = LoginTimeout.Get(context);
            string clientId = ClientId.Get(context);
            string clientSecret = ClientSecret.Get(context);
            string azureAppId = AzureApplicationID.Get(context);

            //conver the URL and Permissions Enum to and array permissions as strings
            var _scopes = Utils.GetAzureAppScopes(inputUrl, AzureAppPermissions);

            SharePointContextInfo customCtx = new SharePointContextInfo()
            {
                Url = inputUrl,
                Password = inputPassword,
                UserName = inputUserName,
                SharePointInstanceType = SharePointInstanceType,
                SharePointPlatformType = PlatformType,
                LoginTimeout = loginTimeout,
                ResetCredentials = ResetCredentials,
                ClientId = clientId,
                ClientSecret = clientSecret,
                AzureAppId = azureAppId,
                groupQueries = QueryGrouping,
                AzureAppPermissions = _scopes
            };

            if (Body != null)
            {
                //context.GetExecutorRuntime().LogMessage(new UiPath.Robot.Activities.Api.LogMessage()
                //{
                //    EventType = TraceEventType.Information,
                //    Message = "Body N1"
                //});
                context.Properties.Add(SharePointContextInfoTag, customCtx);
                //ClientContext.Set(context, ctx);
                context.ScheduleAction<SharePointContextInfo>(this.Body, customCtx, OnCompleted, OnFaulted);
            }
            //context.GetExecutorRuntime().LogMessage(new UiPath.Robot.Activities.Api.LogMessage()
            //{
            //    EventType = TraceEventType.Information,
            //    Message = "Body N2"
            //});
            //ExecuteInternal();
        }

        private void OnFaulted(NativeActivityFaultContext faultContext, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            //throw new NotImplementedException();
        }

        private void OnCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {
            //throw new NotImplementedException();
        }

        
    }
}

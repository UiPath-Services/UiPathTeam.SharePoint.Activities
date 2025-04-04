using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UiPathTeam.SharePoint;


namespace UiPathTeam.SharePoint.Activities.Design
{
    public partial class TestCredentials : Form
    {
        private string theUsername;
        private string thePassword;
        private string theURL;
        private string theInstanceType;
        private bool resetCredentials;
        private double loginTimeout;
        private bool testConnectionPushed;
        private bool validURL = true;
        private bool validTimeout = true;

        public string ThePassword { get => thePassword; set => thePassword = value; }
        public string TheUsername { get => theUsername; set => theUsername = value; }
        public string TheURL { get => theURL; set => theURL = value; }
        public string TheInstanceType { get => theInstanceType; set => theInstanceType = value; }
        public bool TheResetCredentials { get => resetCredentials; set => resetCredentials = value; }
        public double TheLoginTimeout { get => loginTimeout; set => loginTimeout = value; }
        public bool TestConnectionPushed { get => testConnectionPushed; set => testConnectionPushed = value; }

        public string[] AzureAppScopes { get; set; }
        public string AzureAppID { get; set; }

        public TestCredentials(string SPInstanceType,string URL,string Username, string AzureAppId, bool ResetCredentials,  double LoginTimeout)
        {
            
            TheInstanceType = SPInstanceType;
            TheURL = URL;
            TheUsername = Username;
            TheResetCredentials = ResetCredentials;
            TheLoginTimeout = LoginTimeout;
            TestConnectionPushed = false;
            AzureAppID = AzureAppId;
            InitializeComponent();
        }

        private void TestCredentials_Load(object sender, EventArgs e)
        {
            //set the default values of the fields
            InstanceType.Text = TheInstanceType;
            url.Text = TheURL;
            Username.Text = TheUsername;
            ResetCredentialsCheck.Checked = TheResetCredentials;
            AzureAppIDField.Text = AzureAppID;

            if (TheLoginTimeout == 0)
                TimeoutField.Text = "";
            else
                TimeoutField.Text = TheLoginTimeout.ToString();

            ValidateURL(sender,e);
            TimeoutField_Validated(sender, e);

        }

        private void InstanceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            TheInstanceType = InstanceType.Text;

            //no need to enter the username and password for weblogin
            if (TheInstanceType == "WebLogin")
            {
                LoginTimeoutLbl.Visible = true;
                TimeoutField.Visible = true;
                ResetCredentialsCheck.Visible = true;
                ResetCredentialsLbl.Visible = true;

            }
            else
            {
                LoginTimeoutLbl.Visible = false;
                TimeoutField.Visible = false;
                ResetCredentialsCheck.Visible = false;
                ResetCredentialsLbl.Visible = false;
            }

            if (TheInstanceType == "Online" || TheInstanceType == "OnPremises" || TheInstanceType == "AzureApp")
            {
                Username.Visible = true;
                UsernameLbl.Visible = true;
                Password.Visible = true;
                PasswordLbl.Visible = true;
            }
            else
            {
                Username.Visible = false;
                UsernameLbl.Visible = false;
                Password.Visible = false;
                PasswordLbl.Visible = false;
            }

            if (TheInstanceType == "AzureApp")
            {
                AzureAppIDField.Visible = true;
                AzureAppIDLbl.Visible = true;
                GetConsentBtn.Visible = true;
                PermissionsLbl.Visible = true;
                PermissionsField.Visible = true;
            }
            else
            {
                AzureAppIDField.Visible = false;
                AzureAppIDLbl.Visible = false;
                GetConsentBtn.Visible = false;
                PermissionsLbl.Visible = false;
                PermissionsField.Visible = false;
            }
        }

        private void ValidateURL(object sender, EventArgs e)
        {
            if (url == null || string.IsNullOrEmpty(url.Text))
            {
                ExceptionLbl.Visible = true;
                ExceptionLbl.Text = "You have to introduce a SharePoint URL!";
                TestConnection.Enabled = false;
                validURL = false;
            }
            else
            {
                validURL = true;
                if (validTimeout)
                {
                    ExceptionLbl.Visible = false;
                    TestConnection.Enabled = true;
                }
            }
        }
   

        private void TimeoutField_Validated(object sender, EventArgs e)
        {

            if (!string.IsNullOrEmpty(TimeoutField.Text) && (!double.TryParse(TimeoutField.Text, out double timeout)))
            {
                ExceptionLbl.Visible = true;
                ExceptionLbl.Text = "LoginTimeout is a number only field!";
                TestConnection.Enabled = false;
                validTimeout = false;
            }
            else
            {
                validTimeout = true;
                if (validURL)
                {
                    ExceptionLbl.Visible = false;
                    TestConnection.Enabled = true;
                }
            }

        }
        private async void TestConnection_Click(object sender, EventArgs e)
        {
            //Debugger.Launch();
            ExceptionLbl.Visible = false;

            //send back to the parent activity, the values set by the user for the fields
            TheInstanceType = InstanceType.Text;
            TheURL = url.Text;
            TheUsername = Username.Text;
            ThePassword = Password.Text;
            AzureAppID = AzureAppIDField.Text;
            TheResetCredentials = ResetCredentialsCheck.Checked;
            ExtractAzureAppScopes();

            double timeout;
            if (string.IsNullOrEmpty(TimeoutField.Text))
                timeout = 0;
            else
                double.TryParse(TimeoutField.Text, out timeout);


            TheLoginTimeout = timeout;
            TestConnectionPushed = true;

            //if connection to sharepoint was successful, automatically close the form
            if (TryConnectToSharePoint())
                this.Close();
        }

        private async void GetConsentBtn_Click(object sender, EventArgs e)
        {
            ExceptionLbl.Visible = false;
            ValidateURL(sender, e);
            if(validURL)
            {
                if (string.IsNullOrEmpty(AzureAppIDField.Text))
                {
                    ExceptionLbl.Visible = true;
                    ExceptionLbl.Text = "AzureAppID field must contain a valid Azure Application ID";
                }
                else if (PermissionsField.SelectedIndices == null || PermissionsField.SelectedIndices.Count == 0)
                {
                    ExceptionLbl.Visible = true;
                    ExceptionLbl.Text = "You need to select the permissions you want to grant to the App";
                }
                else
                {
                    try
                    {
                        ExtractAzureAppScopes();
                        //var clientContext = await SharePointContextInfo.GetClientConsent(AzureAppIDField.Text, url.Text, AzureAppScopes);
                        MessageBox.Show("Not implemented");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Exception encountered when trying to login to the SharePoint instance via Azure App and interactive token:" + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                ExceptionLbl.Visible = true;
                ExceptionLbl.Text = "Incorrect or null URL";
            }
            
            
        }

        private void ExtractAzureAppScopes()
        {
            if (PermissionsField.SelectedIndices == null || PermissionsField.SelectedIndices.Count == 0)
                return;

            //extract tenant from site URL
            string spoTenant = Utils.ExtractTenantFromSiteURL(url.Text);
            //extract the scopes from the form
            List<string> scopes = new List<string>();
            foreach (int i in PermissionsField.SelectedIndices)
            {
                scopes.Add(String.Format(Utils.Scopes[i], spoTenant));
            }
            AzureAppScopes = scopes.ToArray();
        }

        private bool TryConnectToSharePoint()
        {
            try
            {
                SharePointType spType = (SharePointType)Enum.Parse(typeof(SharePointType), TheInstanceType, true);

                //if (TheInstanceType == "OnPremises")
                //    spType = SharePointType.OnPremises;
                //else if (TheInstanceType == "Online")
                //    spType = SharePointType.Online;
                //else
                //    spType = SharePointType.WebLogin;
                SharePointContextInfo customCtx = new SharePointContextInfo()
                {
                    Url = TheURL,
                    Password = ThePassword,
                    UserName = TheUsername,
                    SharePointInstanceType = spType,
                    ResetCredentials = TheResetCredentials,
                    LoginTimeout = TheLoginTimeout,
                    AzureAppId = AzureAppID,
                    AzureAppPermissions = AzureAppScopes

                };
                var ctx = customCtx.GetSharePointContext();
 
                

                MessageBox.Show("Connected successfully to SharePoint");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception encountered when trying to login to the SharePoint instance:" + ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;   
        }


    }
}

//using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.VisualBasic.Activities;
using System;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.Presentation.Model;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace UiPathTeam.SharePoint.Activities.Design
{
    public partial class SharePointApplicationScopeDesigner
    {
        public SharePointApplicationScopeDesigner()
        {
            InitializeComponent();
        }

        private void Button_Click_TestTheSharePointConnection(object sender, System.Windows.RoutedEventArgs e)
        {
            /*Firstly, get the values inserted by the user in the SharePoint Application Scope activity for the connection parameters
            to use them as default values in our testing form*/
            
            ModelProperty InstanceTypeProperty = ModelItem.Properties["SharePointInstanceType"];
            string sharePointInstanceType = InstanceTypeProperty.ComputedValue.ToString();


            ModelProperty ResetCredentialsProperty = ModelItem.Properties["ResetCredentials"];
            bool resetCredentials = Convert.ToBoolean(ResetCredentialsProperty.ComputedValue);

            ModelProperty LoginTimeoutProperty = ModelItem.Properties["LoginTimeout"];
            double loginTimeout;
            if (LoginTimeoutProperty.ComputedValue != null)
                loginTimeout = Convert.ToDouble(LoginTimeoutProperty.Value.Content.Value.ToString());
            else
                loginTimeout = 0;

            string url = ExtractLiteralValueFromProperty("URL");
            string userName = ExtractLiteralValueFromProperty("UserName");
            string azureAppId = ExtractLiteralValueFromProperty("AzureApplicationID");

            TestCredentials testCredentialForm = new TestCredentials(sharePointInstanceType, url, userName, azureAppId, resetCredentials, loginTimeout);
            testCredentialForm.SuspendLayout();
            testCredentialForm.Width = 640;
            testCredentialForm.Height = 480;
            testCredentialForm.Text = "SharePoint Connection Test";
            testCredentialForm.ResumeLayout(false);
            testCredentialForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            testCredentialForm.MaximizeBox = false;
            testCredentialForm.MinimizeBox = false;
            testCredentialForm.StartPosition = FormStartPosition.CenterScreen;
            testCredentialForm.Focus();
            testCredentialForm.ShowDialog();
        }

        private string ExtractLiteralValueFromProperty(string propertyName)
        {
            // check if the user set a Literal value and not a variable for the given field.
            ModelProperty UsernameProperty = ModelItem.Properties[propertyName];
            object usernameobj = UsernameProperty.ComputedValue;
            if (Utils.IsLiteral(usernameobj))
                return UsernameProperty.Value.Content.Value.ToString();
            return "";
        }
    }
}

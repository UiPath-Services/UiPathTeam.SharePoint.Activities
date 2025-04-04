namespace UiPathTeam.SharePoint.Activities.Design
{
    partial class TestCredentials
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestCredentials));
            this.Username = new System.Windows.Forms.TextBox();
            this.UsernameLbl = new System.Windows.Forms.Label();
            this.PasswordLbl = new System.Windows.Forms.Label();
            this.Password = new System.Windows.Forms.TextBox();
            this.TestConnection = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.URLlbl = new System.Windows.Forms.Label();
            this.url = new System.Windows.Forms.TextBox();
            this.instanceTypelbl = new System.Windows.Forms.Label();
            this.errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this.InstanceType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ResetCredentialsLbl = new System.Windows.Forms.Label();
            this.ResetCredentialsCheck = new System.Windows.Forms.CheckBox();
            this.LoginTimeoutLbl = new System.Windows.Forms.Label();
            this.TimeoutField = new System.Windows.Forms.TextBox();
            this.ExceptionLbl = new System.Windows.Forms.Label();
            this.AzureAppIDLbl = new System.Windows.Forms.Label();
            this.AzureAppIDField = new System.Windows.Forms.TextBox();
            this.GetConsentBtn = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.PermissionsField = new System.Windows.Forms.ListBox();
            this.PermissionsLbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).BeginInit();
            this.SuspendLayout();
            // 
            // Username
            // 
            this.Username.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Username.Location = new System.Drawing.Point(150, 145);
            this.Username.Margin = new System.Windows.Forms.Padding(2);
            this.Username.Name = "Username";
            this.Username.Size = new System.Drawing.Size(148, 23);
            this.Username.TabIndex = 0;
            // 
            // UsernameLbl
            // 
            this.UsernameLbl.AutoSize = true;
            this.UsernameLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UsernameLbl.Location = new System.Drawing.Point(69, 145);
            this.UsernameLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.UsernameLbl.Name = "UsernameLbl";
            this.UsernameLbl.Size = new System.Drawing.Size(86, 17);
            this.UsernameLbl.TabIndex = 1;
            this.UsernameLbl.Text = "Username:";
            // 
            // PasswordLbl
            // 
            this.PasswordLbl.AutoSize = true;
            this.PasswordLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PasswordLbl.Location = new System.Drawing.Point(76, 170);
            this.PasswordLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.PasswordLbl.Name = "PasswordLbl";
            this.PasswordLbl.Size = new System.Drawing.Size(77, 17);
            this.PasswordLbl.TabIndex = 3;
            this.PasswordLbl.Text = "Password";
            // 
            // Password
            // 
            this.Password.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Password.Location = new System.Drawing.Point(150, 170);
            this.Password.Margin = new System.Windows.Forms.Padding(2);
            this.Password.Name = "Password";
            this.Password.PasswordChar = '*';
            this.Password.Size = new System.Drawing.Size(148, 23);
            this.Password.TabIndex = 2;
            // 
            // TestConnection
            // 
            this.TestConnection.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TestConnection.Location = new System.Drawing.Point(80, 311);
            this.TestConnection.Margin = new System.Windows.Forms.Padding(2);
            this.TestConnection.Name = "TestConnection";
            this.TestConnection.Size = new System.Drawing.Size(142, 30);
            this.TestConnection.TabIndex = 4;
            this.TestConnection.Text = "Test Connection";
            this.TestConnection.UseVisualStyleBackColor = true;
            this.TestConnection.Click += new System.EventHandler(this.TestConnection_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(10, 25);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(374, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "Introduce your SharePoint connection parameters:";
            // 
            // URLlbl
            // 
            this.URLlbl.AutoSize = true;
            this.URLlbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.URLlbl.Location = new System.Drawing.Point(105, 119);
            this.URLlbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.URLlbl.Name = "URLlbl";
            this.URLlbl.Size = new System.Drawing.Size(44, 17);
            this.URLlbl.TabIndex = 7;
            this.URLlbl.Text = "URL:";
            this.URLlbl.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // url
            // 
            this.url.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.url.Location = new System.Drawing.Point(150, 119);
            this.url.Margin = new System.Windows.Forms.Padding(2);
            this.url.Name = "url";
            this.url.Size = new System.Drawing.Size(148, 23);
            this.url.TabIndex = 6;
            this.url.Validated += new System.EventHandler(this.ValidateURL);
            // 
            // instanceTypelbl
            // 
            this.instanceTypelbl.AutoSize = true;
            this.instanceTypelbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.instanceTypelbl.Location = new System.Drawing.Point(45, 92);
            this.instanceTypelbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.instanceTypelbl.Name = "instanceTypelbl";
            this.instanceTypelbl.Size = new System.Drawing.Size(115, 17);
            this.instanceTypelbl.TabIndex = 8;
            this.instanceTypelbl.Text = "Instance Type:";
            // 
            // errorProvider1
            // 
            this.errorProvider1.ContainerControl = this;
            // 
            // InstanceType
            // 
            this.InstanceType.FormattingEnabled = true;
            this.InstanceType.Items.AddRange(new object[] {
            "OnPremises",
            "Online",
            "WebLogin",
            "AzureApp"});
            this.InstanceType.Location = new System.Drawing.Point(150, 92);
            this.InstanceType.Margin = new System.Windows.Forms.Padding(2);
            this.InstanceType.Name = "InstanceType";
            this.InstanceType.Size = new System.Drawing.Size(124, 21);
            this.InstanceType.TabIndex = 9;
            this.InstanceType.SelectedIndexChanged += new System.EventHandler(this.InstanceType_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(110, 41);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(112, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "( Don\'t use variables! )";
            // 
            // ResetCredentialsLbl
            // 
            this.ResetCredentialsLbl.AutoSize = true;
            this.ResetCredentialsLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ResetCredentialsLbl.Location = new System.Drawing.Point(45, 149);
            this.ResetCredentialsLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.ResetCredentialsLbl.Name = "ResetCredentialsLbl";
            this.ResetCredentialsLbl.Size = new System.Drawing.Size(111, 17);
            this.ResetCredentialsLbl.TabIndex = 11;
            this.ResetCredentialsLbl.Text = "For New User:";
            this.ResetCredentialsLbl.Visible = false;
            // 
            // ResetCredentialsCheck
            // 
            this.ResetCredentialsCheck.AutoSize = true;
            this.ResetCredentialsCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ResetCredentialsCheck.Location = new System.Drawing.Point(150, 145);
            this.ResetCredentialsCheck.Margin = new System.Windows.Forms.Padding(2);
            this.ResetCredentialsCheck.Name = "ResetCredentialsCheck";
            this.ResetCredentialsCheck.Size = new System.Drawing.Size(156, 21);
            this.ResetCredentialsCheck.TabIndex = 12;
            this.ResetCredentialsCheck.Text = "Reset Credentials";
            this.ResetCredentialsCheck.UseVisualStyleBackColor = true;
            this.ResetCredentialsCheck.Visible = false;
            // 
            // LoginTimeoutLbl
            // 
            this.LoginTimeoutLbl.AutoSize = true;
            this.LoginTimeoutLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoginTimeoutLbl.Location = new System.Drawing.Point(43, 172);
            this.LoginTimeoutLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.LoginTimeoutLbl.Name = "LoginTimeoutLbl";
            this.LoginTimeoutLbl.Size = new System.Drawing.Size(116, 17);
            this.LoginTimeoutLbl.TabIndex = 13;
            this.LoginTimeoutLbl.Text = "LoginTimeout: ";
            // 
            // TimeoutField
            // 
            this.TimeoutField.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TimeoutField.Location = new System.Drawing.Point(150, 172);
            this.TimeoutField.Margin = new System.Windows.Forms.Padding(2);
            this.TimeoutField.Name = "TimeoutField";
            this.TimeoutField.Size = new System.Drawing.Size(148, 23);
            this.TimeoutField.TabIndex = 6;
            this.TimeoutField.Visible = false;
            this.TimeoutField.Validated += new System.EventHandler(this.TimeoutField_Validated);
            // 
            // ExceptionLbl
            // 
            this.ExceptionLbl.AutoSize = true;
            this.ExceptionLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ExceptionLbl.ForeColor = System.Drawing.Color.Red;
            this.ExceptionLbl.Location = new System.Drawing.Point(43, 65);
            this.ExceptionLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.ExceptionLbl.Name = "ExceptionLbl";
            this.ExceptionLbl.Size = new System.Drawing.Size(306, 17);
            this.ExceptionLbl.TabIndex = 14;
            this.ExceptionLbl.Text = "You have to introduce a Sharepoint URL!";
            this.ExceptionLbl.Visible = false;
            // 
            // AzureAppIDLbl
            // 
            this.AzureAppIDLbl.AutoSize = true;
            this.AzureAppIDLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AzureAppIDLbl.Location = new System.Drawing.Point(45, 197);
            this.AzureAppIDLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.AzureAppIDLbl.Name = "AzureAppIDLbl";
            this.AzureAppIDLbl.Size = new System.Drawing.Size(108, 17);
            this.AzureAppIDLbl.TabIndex = 15;
            this.AzureAppIDLbl.Text = "Azure App ID:";
            // 
            // AzureAppIDField
            // 
            this.AzureAppIDField.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AzureAppIDField.Location = new System.Drawing.Point(150, 199);
            this.AzureAppIDField.Margin = new System.Windows.Forms.Padding(2);
            this.AzureAppIDField.Name = "AzureAppIDField";
            this.AzureAppIDField.Size = new System.Drawing.Size(148, 23);
            this.AzureAppIDField.TabIndex = 16;
            this.AzureAppIDField.Visible = false;
            // 
            // GetConsentBtn
            // 
            this.GetConsentBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GetConsentBtn.Location = new System.Drawing.Point(242, 311);
            this.GetConsentBtn.Margin = new System.Windows.Forms.Padding(2);
            this.GetConsentBtn.Name = "GetConsentBtn";
            this.GetConsentBtn.Size = new System.Drawing.Size(142, 30);
            this.GetConsentBtn.TabIndex = 17;
            this.GetConsentBtn.Text = "Get User Consent";
            this.toolTip1.SetToolTip(this.GetConsentBtn, resources.GetString("GetConsentBtn.ToolTip"));
            this.GetConsentBtn.UseVisualStyleBackColor = true;
            this.GetConsentBtn.Click += new System.EventHandler(this.GetConsentBtn_Click);
            // 
            // PermissionsField
            // 
            this.PermissionsField.FormattingEnabled = true;
            this.PermissionsField.Items.AddRange(new object[] {
            "Read",
            "Write",
            "Manage",
            "FullControl"});
            this.PermissionsField.Location = new System.Drawing.Point(150, 238);
            this.PermissionsField.Name = "PermissionsField";
            this.PermissionsField.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.PermissionsField.Size = new System.Drawing.Size(156, 56);
            this.PermissionsField.TabIndex = 18;
            this.PermissionsField.Visible = false;
            // 
            // PermissionsLbl
            // 
            this.PermissionsLbl.AutoSize = true;
            this.PermissionsLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PermissionsLbl.Location = new System.Drawing.Point(49, 238);
            this.PermissionsLbl.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.PermissionsLbl.Name = "PermissionsLbl";
            this.PermissionsLbl.Size = new System.Drawing.Size(100, 17);
            this.PermissionsLbl.TabIndex = 19;
            this.PermissionsLbl.Text = "Permissions:";
            // 
            // TestCredentials
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(430, 371);
            this.Controls.Add(this.PermissionsLbl);
            this.Controls.Add(this.PermissionsField);
            this.Controls.Add(this.GetConsentBtn);
            this.Controls.Add(this.AzureAppIDField);
            this.Controls.Add(this.AzureAppIDLbl);
            this.Controls.Add(this.ExceptionLbl);
            this.Controls.Add(this.TimeoutField);
            this.Controls.Add(this.LoginTimeoutLbl);
            this.Controls.Add(this.ResetCredentialsCheck);
            this.Controls.Add(this.ResetCredentialsLbl);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.InstanceType);
            this.Controls.Add(this.instanceTypelbl);
            this.Controls.Add(this.URLlbl);
            this.Controls.Add(this.url);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TestConnection);
            this.Controls.Add(this.PasswordLbl);
            this.Controls.Add(this.Password);
            this.Controls.Add(this.UsernameLbl);
            this.Controls.Add(this.Username);
            this.ForeColor = System.Drawing.Color.CornflowerBlue;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "TestCredentials";
            this.Text = "TestCredentials";
            this.Load += new System.EventHandler(this.TestCredentials_Load);
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Username;
        private System.Windows.Forms.Label UsernameLbl;
        private System.Windows.Forms.Label PasswordLbl;
        private System.Windows.Forms.TextBox Password;
        private System.Windows.Forms.Button TestConnection;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label URLlbl;
        private System.Windows.Forms.TextBox url;
        private System.Windows.Forms.Label instanceTypelbl;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.ComboBox InstanceType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label ResetCredentialsLbl;
        private System.Windows.Forms.CheckBox ResetCredentialsCheck;
        private System.Windows.Forms.TextBox TimeoutField;
        private System.Windows.Forms.Label LoginTimeoutLbl;
        private System.Windows.Forms.Label ExceptionLbl;
        private System.Windows.Forms.Label AzureAppIDLbl;
        private System.Windows.Forms.TextBox AzureAppIDField;
        private System.Windows.Forms.Button GetConsentBtn;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label PermissionsLbl;
        private System.Windows.Forms.ListBox PermissionsField;
    }
}
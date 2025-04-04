using Microsoft.Win32;
using System;
using System.Activities;
using System.Activities.Presentation.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UiPathTeam.SharePoint.Activities.Design
{
    // Interaction logic for SharePointActivityDesigner.xaml
    public partial class SharePointActivityDesigner
    {
        
        public SharePointActivityDesigner()
        {
            
            InitializeComponent();
          
        }

        private void Button_Click_ShowLocalPath(object sender, RoutedEventArgs e)
        {
            if (this.ModelItem.Properties["ChooseFile"].Value.ToString().ToLower().Equals("true"))
           
            {
                Microsoft.Win32.OpenFileDialog _openFileDialog = new Microsoft.Win32.OpenFileDialog();
                _openFileDialog.Title = "What file do you want to upload?";
                _openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();


                if (_openFileDialog.ShowDialog() == true)
                {
                    ModelProperty property = this.ModelItem.Properties["LocalPath"];
                    property.SetValue(new InArgument<string>(Utils.TrimFilePath(_openFileDialog.FileName, Directory.GetCurrentDirectory())));
                    
                }
            }
            else
            {
                FolderBrowserDialog folderBrowser = new FolderBrowserDialog();

                folderBrowser.Description = "Where do you want to save the file?";
                folderBrowser.SelectedPath = Directory.GetCurrentDirectory();
                folderBrowser.ShowNewFolderButton = true;
                


                if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ModelProperty property = this.ModelItem.Properties["LocalPath"];
                    // property.SetValue(new InArgument<string>(folderBrowser.SelectedPath));
                    property.SetValue(new InArgument<string>(Utils.TrimFilePath(folderBrowser.SelectedPath, Directory.GetCurrentDirectory())));
                   



                }

            }
        }
        private void Button_Click_SelectLocalPath(object sender, RoutedEventArgs e)
        {

            Microsoft.Win32.OpenFileDialog _openFileDialog = new Microsoft.Win32.OpenFileDialog();
            _openFileDialog.Title = "What file do you want to upload?";
            _openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();


            if (_openFileDialog.ShowDialog() == true)
            {
                ModelProperty property = this.ModelItem.Properties["LocalPath"];
                property.SetValue(new InArgument<string>(_openFileDialog.FileName));
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UiPathTeam.SharePoint.Activities.Design
{
    // Interaction logic for ProcessListItems.xaml
    public partial class ProcessListItems
    {
        public ProcessListItems()
        {
            InitializeComponent();

           
        }
        private void ExpressionTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            ((System.Activities.Presentation.View.ExpressionTextBox)sender).ExpressionType = typeof(Dictionary<string,object>); 
        }

        private void AttachFiles_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
            ArgumenCollectionEditor.ShowDialog("Attachments", ModelItem);
        }
    }
}

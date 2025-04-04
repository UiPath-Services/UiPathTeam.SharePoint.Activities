using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Converters;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.PropertyEditing;
using UiPathTeam.SharePoint.Activities.Design.Editors;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace UiPathTeam.SharePoint.Activities.Design
{
    public class ArgumenCollectionEditor:  DialogPropertyValueEditor
    {
        private static DataTemplate EditorTemplate = (DataTemplate)new EditorTemplates()["ArgumentDictionaryEditor"];

        public ArgumenCollectionEditor()
        {
            this.InlineEditorTemplate = EditorTemplate;
        }

        public override void ShowDialog(PropertyValue propertyValue, IInputElement commandSource)
        {
            string propertyName = propertyValue.ParentProperty.DisplayName;

            var ownerActivity = (new ModelPropertyEntryToOwnerActivityConverter()).Convert(
                propertyValue.ParentProperty, typeof(ModelItem), false, null) as ModelItem;

            ShowDialog(propertyName, ownerActivity);
        }

        public static void ShowDialog(string propertyName, ModelItem ownerActivity)
        {
            DynamicArgumentDesignerOptions options = new DynamicArgumentDesignerOptions()
            {
                Title = propertyName
            };

            ModelItem modelItem = ownerActivity.Properties["Attachments"].Collection;

            using (ModelEditingScope change = modelItem.BeginEdit(propertyName + "Editing"))
            {
                if (DynamicArgumentDialog.ShowDialog(ownerActivity, modelItem, ownerActivity.GetEditingContext(), ownerActivity.View, options))
                {
                    change.Complete();
                }
                else
                {
                    change.Revert();
                }
            }
        }
    }
}

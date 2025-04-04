using System.Activities.Presentation.Metadata;
using System.ComponentModel;

//using UiPathTeam.SharePoint.Activities.Libraries;
using UiPathTeam.SharePoint.Activities.Lists;
//using UiPathTeam.SharePoint.Activities.Permissions;
//using UiPathTeam.SharePoint.Activities.Users;
//using Microsoft.SharePoint.Client;
using System.Activities.Presentation.PropertyEditing;
using System.Activities;
using System.Reflection;
using System.Linq;
using UiPathTeam.SharePoint.Activities.Users;
using UiPathTeam.SharePoint.Activities.Permissions;
using UiPathTeam.SharePoint.Activities.Libraries;

namespace UiPathTeam.SharePoint.Activities.Design
{
    public class DesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            AttributeTableBuilder attributeTableBuilder = new AttributeTableBuilder();
            attributeTableBuilder.AddCustomAttributes(typeof(SharepointApplicationScope), new DesignerAttribute(typeof(SharePointApplicationScopeDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(SignOut), new DesignerAttribute(typeof(OnlyLogoDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetWebLoginUser), new DesignerAttribute(typeof(OnlyLogoDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetSPTimeZone), new DesignerAttribute(typeof(OnlyLogoDesigner)));
            //lib designers
            attributeTableBuilder.AddCustomAttributes(typeof(CreateFolder), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(Delete), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(RenameItem), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(MoveItem), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetChildrenNames), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetFile), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(UploadFile), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(UploadLargeFile), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(CheckInFile), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(CheckOutFile), new DesignerAttribute(typeof(SharePointActivityDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(DiscardCheckout), new DesignerAttribute(typeof(SharePointActivityDesigner)));

            ////designers for List related activities
            attributeTableBuilder.AddCustomAttributes(typeof(UpdateListItems), new DesignerAttribute(typeof(ProcessListItems)));
            attributeTableBuilder.AddCustomAttributes(typeof(DeleteListItems), new DesignerAttribute(typeof(ProcessListItems)));
            attributeTableBuilder.AddCustomAttributes(typeof(ReadListItems), new DesignerAttribute(typeof(ProcessListItems)));
            attributeTableBuilder.AddCustomAttributes(typeof(AddListItem), new DesignerAttribute(typeof(ProcessListItems)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetListItemAttachments), new DesignerAttribute(typeof(OnlyLogoDesigner)));
            attributeTableBuilder.AddCustomAttributes(typeof(AddListItemAttachments), new DesignerAttribute(typeof(ProcessListItems)));
            attributeTableBuilder.AddCustomAttributes(typeof(AddListItemAttachments), nameof(AddListItemAttachments.Attachments), new EditorAttribute(typeof(ArgumenCollectionEditor), typeof(DialogPropertyValueEditor)));
            attributeTableBuilder.AddCustomAttributes(typeof(AddListItemAttachments), new DisplayNameAttribute("Add List Item Attachments"));

            attributeTableBuilder.AddCustomAttributes(typeof(DeleteListItemAttachments), new DesignerAttribute(typeof(ProcessListItems)));
            attributeTableBuilder.AddCustomAttributes(typeof(DeleteListItemAttachments), nameof(DeleteListItemAttachments.Attachments), new EditorAttribute(typeof(ArgumenCollectionEditor), typeof(DialogPropertyValueEditor)));


            //DisplayName


            //designers for User&Group related activities
            attributeTableBuilder.AddCustomAttributes(typeof(AddUserToGroup), new DesignerAttribute(typeof(ProcessUsers)));
            attributeTableBuilder.AddCustomAttributes(typeof(CreateGroup), new DesignerAttribute(typeof(ProcessUsers)));
            attributeTableBuilder.AddCustomAttributes(typeof(DeleteGroup), new DesignerAttribute(typeof(ProcessUsers)));
            attributeTableBuilder.AddCustomAttributes(typeof(RemoveUserFromGroup), new DesignerAttribute(typeof(ProcessUsers)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetAllUsersFromGroup), new DesignerAttribute(typeof(ProcessUsers)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetUser), new DesignerAttribute(typeof(ProcessUsers)));

            ////designers for Permission related activities
            attributeTableBuilder.AddCustomAttributes(typeof(AddPermission), new DesignerAttribute(typeof(ProcessPermissions)));
            attributeTableBuilder.AddCustomAttributes(typeof(RemovePermission), new DesignerAttribute(typeof(ProcessPermissions)));
            attributeTableBuilder.AddCustomAttributes(typeof(GetPermissions), new DesignerAttribute(typeof(OnlyLogoDesigner)));

            MetadataStore.AddAttributeTable(attributeTableBuilder.CreateTable());

            //loading the SharePoint Dlls
            //ClientContext clientContext = new ClientContext("");
        }

       
    }
}

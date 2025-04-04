using System;
using System.Activities.Statements;
using System.Activities.Validation;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiPathTeam.SharePoint.Activities
{
    public abstract class SharePointCodeActivity : AsyncCodeActivity
    {
        [Browsable(false)]
        public bool ShowListName { get; set; }
        [Browsable(false)]
        public bool ShowPropertiesDictionary { get; set; }
        [Browsable(false)]
        public bool ShowCAMLQuery { get; set; }
        [Browsable(false)]
        public bool ShowAttachFiles { get; set; }
        [Browsable(false)]
        public string AttachmentsAction { get; set; }
        [Browsable(false)]
        public bool ShowPermissionDropdown { get; set; }
        [Browsable(false)]
        public bool ShowGroupName { get; set; }
        [Browsable(false)]
        public bool ShowGroupDescription { get; set; }
        [Browsable(false)]
        public bool ShowUserName { get; set; }

        //only for libraries
        [Browsable(false)]
        public bool ShowCAMLWarning { get; set; }
        [Browsable(false)]
        public bool ShowLibraryName { get; set; }
        [Browsable(false)]
        public bool ShowRelativeUrl { get; set; }
        [Browsable(false)]
        public string RelativeUrlHintText { get; set; }
        [Browsable(false)]
        public bool ShowMove { get; set; }
        [Browsable(false)]
        public bool ShowRename { get; set; }
        [Browsable(false)]
        public bool ShowLocalPath { get; set; }
        [Browsable(false)]
        public bool ChooseFile { get; set; }
        [Browsable(false)]
        public string LocalPathHintText { get; set; }

        public SharePointCodeActivity(bool allowBatchQueries = false)
        {


            if (!allowBatchQueries)
            {
                //only checking if there is an ancestor who is a SPScope
                base.Constraints.Add(CheckParent(ActivityIsSPScopeAndAllowsBatchQueries, "Activity has to be inside a SharepointApplicationScope with the Batch Query feature disabled"));
            }
            else
            {
                //only checking if there is an ancestor who is a SPScope
                base.Constraints.Add(CheckParent(CheckIfActivityIsSPScope, "Activity has to be inside a SharepointApplicationScope"));
            }

        }

        public static bool CheckIfActivityIsSPScope(Activity activity)
        {
            return object.Equals(activity.GetType(), typeof(SharepointApplicationScope));
        }
        public static bool ActivityIsSPScopeAndAllowsBatchQueries(Activity activity)
        {
            //in addition to checking the parent type, also check the 
            return CheckIfActivityIsSPScope(activity)
               && !((SharepointApplicationScope)activity).QueryGrouping;
        }



        internal static Constraint CheckParent(SPActivityTypeValidator validator, string validationMessage)
        {
            DelegateInArgument<SharePointCodeActivity> element = new DelegateInArgument<SharePointCodeActivity>();
            DelegateInArgument<ValidationContext> context = new DelegateInArgument<ValidationContext>();
            Variable<bool> result = new Variable<bool>();
            DelegateInArgument<Activity> parent = new DelegateInArgument<Activity>();

            return new Constraint<SharePointCodeActivity>
            {
                Body = new ActivityAction<SharePointCodeActivity, ValidationContext>
                {
                    Argument1 = element,
                    Argument2 = context,
                    Handler = new Sequence
                    {
                        Variables =
                    {
                        result
                    },
                        Activities =
                    {
                        new ForEach<Activity>
                        {
                            Values = new GetParentChain
                            {
                                ValidationContext = context
                            },
                            Body = new ActivityAction<Activity>
                            {
                                Argument = parent,
                                Handler = new If()
                                {
                                    Condition = new InArgument<bool>( env => validator(parent.Get(env))),
                                        Then = new Assign<bool>
                                        {
                                            Value = true,
                                            To = result
                                        }
                                }
                            }
                        },
                        new AssertValidation
                        {
                            Assertion = new InArgument<bool>(result),
                            Message = new InArgument<string> (validationMessage),
                        }
                    }
                    }
                }
            };
        }

        
    }

    public abstract class SharePointMultiQueryCodeActivity : SharePointCodeActivity
    {

        public SharePointMultiQueryCodeActivity(bool allowBatchQueries = false) : base()
        { }

        [Category("Input")]
        [Description("The maximum number of items that will be processed at once. Use this in order to not go over the maximum request size.")]
        public InArgument<int> NumberOfItemsProcessedAtOnce { get; set; }

        [Category("Input")]
        [Description("The CAMLQuery property can be null or empty only if this is checked.")]
        public bool AllowOperationOnAllItems { get; set; }

        [Category("Input")]
        [Description("Query that filters out the items that need to be processed. Leaving this empty will update/delete all items")]
        public InArgument<string> CAMLQuery { get; set; }

        protected override void CacheMetadata(CodeActivityMetadata metadata)
        {
            if (CAMLQuery == null && !AllowOperationOnAllItems)
            {
                ValidationError error = new ValidationError("The CamlQuery can be null or empty string only if AllowOperationOnAllItems is enabled.");
                metadata.AddValidationError(error);
            }
            base.CacheMetadata(metadata);
        }

        public void CheckIfEmptyQueriesAreAllowed(string query)
        {
            if (!AllowOperationOnAllItems && (query == null || String.IsNullOrWhiteSpace(query))) throw new Exception("The CAML query can be null or empty string only if AllowOperationOnAllItems is enabled.");
        }
    }

    /*parent class for all activities that don't need to be placed
    inside a SharePoint App Scope as long as the URL of the SP site was provided*/
    public abstract class SPUrlOnlyCodeActivity : AsyncCodeActivity
    {
        [Category("Input")]
        [Description("Enter the url of the SP site")]
        public InArgument<string> URL { get; set; }

        public SPUrlOnlyCodeActivity()
        {
            base.Constraints.Add(CheckParent(CheckIfURLIsSetWhenActivityNotSPScope, "Activity has to be inside a SharepointApplicationScope if no URL provided"));

        }

        public bool CheckIfURLIsSetWhenActivityNotSPScope(Activity activity)
        {
            if (!object.Equals(activity.GetType(), typeof(SharepointApplicationScope)))
            {
                if (URL == null)
                    return false;
                else
                    return true;

            }
            else
                return true;
        }
        internal static Constraint CheckParent(SPActivityTypeValidator validator, string validationMessage)
        {
            DelegateInArgument<SPUrlOnlyCodeActivity> element = new DelegateInArgument<SPUrlOnlyCodeActivity>();
            DelegateInArgument<ValidationContext> context = new DelegateInArgument<ValidationContext>();
            Variable<bool> result = new Variable<bool>();
            DelegateInArgument<Activity> parent = new DelegateInArgument<Activity>();

            return new Constraint<SPUrlOnlyCodeActivity>
            {
                Body = new ActivityAction<SPUrlOnlyCodeActivity, ValidationContext>
                {
                    Argument1 = element,
                    Argument2 = context,
                    Handler = new Sequence
                    {
                        Variables =
                    {
                        result
                    },
                        Activities =
                    {
                        new ForEach<Activity>
                        {
                            Values = new GetParentChain
                            {
                                ValidationContext = context
                            },
                            Body = new ActivityAction<Activity>
                            {
                                Argument = parent,
                                Handler = new If()
                                {
                                    Condition = new InArgument<bool>( env => validator(parent.Get(env))),
                                        Then = new Assign<bool>
                                        {
                                            Value = true,
                                            To = result
                                        }
                                }
                            }
                        },
                        new AssertValidation
                        {
                            Assertion = new InArgument<bool>(result),
                            Message = new InArgument<string> (validationMessage),
                        }
                    }
                    }
                }
            };
        }
    }
}

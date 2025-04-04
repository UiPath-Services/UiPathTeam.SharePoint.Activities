using System.Activities.XamlIntegration;
using System.Activities;
using Xunit;

namespace UiPathTeam.SharePoint.Activities.Tests
{
    public class ActivityTemplateWorkflowTests
    {
        [Fact]
        public void Test()
        {
            ActivityXamlServicesSettings settings = new ActivityXamlServicesSettings
            {
                CompileExpressions = true
            };


            Activity workflow = ActivityXamlServices.Load("TestAllActivities.xaml", settings);
            WorkflowInvoker.Invoke(workflow);

            Console.WriteLine("Press <enter> to exit");
            Console.ReadLine();
        }

    }
}

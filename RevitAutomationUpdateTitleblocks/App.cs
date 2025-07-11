using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using DesignAutomationFramework;

namespace RevitAutomationUpdateTitleblocks
{
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class App : IExternalDBApplication
    {
        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }

        public ExternalDBApplicationResult OnStartup(ControlledApplication application)
        {
            DesignAutomationBridge.DesignAutomationReadyEvent += HandleDesignAutomationReadyEvent;
            return ExternalDBApplicationResult.Succeeded;
        }

        public void HandleDesignAutomationReadyEvent(object? sender, DesignAutomationReadyEventArgs e)
        {
            e.Succeeded = ProcessParameters(e.DesignAutomationData);
        }

        public bool ProcessParameters(DesignAutomationData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Application rvtApp = data.RevitApp;
            if (rvtApp == null)
                throw new InvalidDataException(nameof(rvtApp));

            Document rvtDoc = data.RevitDoc;
            if (rvtDoc == null)
                throw new InvalidDataException(nameof(rvtDoc));

            return UpdateTitleblocks(rvtDoc);
        }

        public bool UpdateTitleblocks(Document rvtDoc)
        {
            if (rvtDoc == null)
                throw new ArgumentNullException(nameof(rvtDoc));

            Console.WriteLine("Start Updating Titleblocks");
            string dirPath = Directory.GetCurrentDirectory();

            SaveAsOptions saveAsOptions = new SaveAsOptions();
            saveAsOptions.OverwriteExistingFile = true;
            rvtDoc.SaveAs(Path.Combine(dirPath, "result.rvt"), saveAsOptions);

            Console.WriteLine("Complete Updating Titleblocks");
            return true;
        }
    }
}

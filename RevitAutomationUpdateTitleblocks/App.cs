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

        public static bool UpdateTitleblocks(Document rvtDoc)
        {
            if (rvtDoc == null)
                throw new ArgumentNullException(nameof(rvtDoc));

            Console.WriteLine("Start Updating Titleblocks");

            // 1. Collect all titleblock instances in the document
            var collector = new FilteredElementCollector(rvtDoc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsNotElementType();

            var titleblocks = collector.Cast<FamilyInstance>().ToList();

            // 2. Read CSV file for updates
            string dirPath = Directory.GetCurrentDirectory();
            string csvPath = Path.Combine(dirPath, "titleblock_updates.csv");
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"CSV file not found: {csvPath}");
                return false;
            }

            // Parse CSV into a dictionary: SheetNumber -> List of (ParameterName, ParameterValue)
            var updates = new Dictionary<string, List<(string, string)>>();
            foreach (var line in File.ReadLines(csvPath).Skip(1)) // Skip header
            {
                var parts = line.Split(',');
                if (parts.Length < 3) continue;
                string sheetNumber = parts[0].Trim();
                string paramName = parts[1].Trim();
                string paramValue = parts[2].Trim();

                if (!updates.ContainsKey(sheetNumber))
                    updates[sheetNumber] = new List<(string, string)>();
                updates[sheetNumber].Add((paramName, paramValue));
            }

            // 3. Update titleblock parameters
            int updatedCount = 0;
            using (Transaction tx = new Transaction(rvtDoc, "Update Titleblocks"))
            {
                tx.Start();
                foreach (var tb in titleblocks)
                {
                    // Get the sheet number from the titleblock's owner view (Sheet)
                    var ownerView = rvtDoc.GetElement(tb.OwnerViewId) as ViewSheet;
                    if (ownerView == null) continue;
                    string sheetNumber = ownerView.SheetNumber;

                    if (updates.TryGetValue(sheetNumber, out var paramUpdates))
                    {
                        foreach (var (paramName, paramValue) in paramUpdates)
                        {
                            Parameter param = tb.LookupParameter(paramName);
                            if (param != null && !param.IsReadOnly)
                            {
                                if (param.StorageType == StorageType.String)
                                    param.Set(paramValue);
                                else if (param.StorageType == StorageType.Integer && int.TryParse(paramValue, out int intVal))
                                    param.Set(intVal);
                                else if (param.StorageType == StorageType.Double && double.TryParse(paramValue, out double dblVal))
                                    param.Set(dblVal);
                                // Add more type handling as needed
                            }
                        }
                        updatedCount++;
                    }
                }
                tx.Commit();
            }

            Console.WriteLine($"Updated {updatedCount} titleblocks.");
            // Save the document
            SaveAsOptions saveAsOptions = new SaveAsOptions { OverwriteExistingFile = true };
            rvtDoc.SaveAs(Path.Combine(dirPath, "result.rvt"), saveAsOptions);

            Console.WriteLine("Complete Updating Titleblocks");
            return true;
        }
    }
}

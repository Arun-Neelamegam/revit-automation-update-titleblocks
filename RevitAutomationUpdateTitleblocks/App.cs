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
            var sheets = new FilteredElementCollector(rvtDoc)
                .OfCategory(BuiltInCategory.OST_Sheets)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>();

            // 2. Read CSV file for updates
            string dirPath = Directory.GetCurrentDirectory();
            string csvPath = Path.Combine(dirPath, "titleblock_updates.csv");
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"CSV file not found: {csvPath}");
                return false;
            }

            // Parse CSV into a list
            var updates = new Dictionary<string, List<(string, string)>>();
            var revisionDescriptionList = new List<string>();
            var revisionDateList = new List<string>();
            foreach (var line in File.ReadLines(csvPath).Skip(1)) // Skip header
            {
                var parts = line.Split(',');
                if (parts.Length < 2) continue;
                string revisionDescription = parts[0].Trim();
                string revisionDate = parts[1].Trim();
                revisionDescriptionList.Add(revisionDescription);
                revisionDateList.Add(revisionDate);
            }

            if(revisionDescriptionList.Count != revisionDateList.Count)
            {
                Console.WriteLine($"Excel File has mis-matched data");
            }

            // 3. Update titleblock parameters
            int updatedCount = 0;
            using (Transaction tx = new Transaction(rvtDoc, "Update Titleblocks"))
            {
                tx.Start();

                for(int i = 0; i < revisionDateList.Count; i++)
                {
                    // Create new revision
                    Revision revision = Revision.Create(rvtDoc);

                    // Optional: Customize revision properties
                    revision.Description = revisionDescriptionList[i];
                    revision.RevisionDate = revisionDateList[i];
                    revision.Visibility = RevisionVisibility.TagVisible;
                    foreach (ViewSheet sheet in sheets)
                    {
                        // Get the revisions already on the sheet
                        var existingIds = sheet.GetAdditionalRevisionIds();

                        // Add the new revision if not already present
                        if (!existingIds.Contains(revision.Id))
                        {
                            var allRevisions = existingIds.ToList();
                            allRevisions.Add(revision.Id);
                            sheet.SetAdditionalRevisionIds(allRevisions);
                            updatedCount++;
                        }
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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System.IO;

namespace DesignAutomationHandler
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UpdateTitleblocks : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Result r = Result.Succeeded;
            try
            {

                var app = commandData.Application.Application;
                var doc = commandData.Application.ActiveUIDocument?.Document;
                LocalDebugRibbon.HandleDAApplication(app, doc);

            }
            catch (Exception ex)
            {
                string error = ex.Message;
                r = Result.Failed;
            }
            return r;
        }
    }
}

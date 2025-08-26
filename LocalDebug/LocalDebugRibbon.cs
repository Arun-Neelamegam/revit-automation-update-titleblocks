using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DesignAutomationFramework;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesignAutomationHandler
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class LocalDebugRibbon : IExternalApplication
    {
        private const string WorkingDir = "C:\\Test\\Revit Automation Update Titleblocks";

        public Result OnStartup(UIControlledApplication application)
        {
            Result r = Result.Succeeded;
            try
            {

                if (Directory.GetCurrentDirectory() == WorkingDir)
                {
                    // create ribbon tab
                    application.CreateRibbonTab("Local Debug");

                    //create ribbon panels
                    RibbonPanel localDebugPanel = application.CreateRibbonPanel("Local Debug", "AU 2025 Demo");

                    // add Update Titleblocks button
                    PushButtonData updateTitleblocksButtonData = new PushButtonData(
                        "UpdateTitleblocks",
                        "Update\nTitleblocks",
                         typeof(LocalDebugRibbon).Assembly.Location,
                        typeof(LocalDebugRibbon).Namespace + ".UpdateTitleblocks");
                    PushButton updateTitleblocksPushButton = localDebugPanel.AddItem(updateTitleblocksButtonData) as PushButton;
                    updateTitleblocksPushButton.LargeImage = LoadPNGImageFromResource(typeof(LocalDebugRibbon).Namespace + ".Icons.Titleblocks.png");
                    updateTitleblocksPushButton.ToolTip = "Automate the updating of Title Blocks Parameters";
                    updateTitleblocksPushButton.LongDescription = "Update Specific Parameters for Title Blocks";
                }

            }
            catch (Exception ex)
            {
                string error = ex.Message;
                r = Result.Failed;
            }
            return r;
        }


        public static void HandleDAApplication(Autodesk.Revit.ApplicationServices.Application app, Document doc)
        {
            try
            {
                var filename = doc?.PathName;
                var currentdir = Directory.GetCurrentDirectory();
                var message = string.Empty;
                if (string.IsNullOrEmpty(filename))
                {
                    message = $"No input file.\nCopy the RVT file under the current folder:\n{currentdir}";
                    MessageBox.Show(message, "DesignAutomationHandler");
                }

                bool designAutomationResult = DesignAutomationBridge.SetDesignAutomationReady(app, filename);

                if (designAutomationResult)
                {
                    //var resultFolder = string.IsNullOrEmpty(filename) ? currentdir : Path.GetDirectoryName(filename);
                    message = $"Succeed!\nFind the results at folder: {currentdir}";
                }
                else
                {
                    message = $"Failed! You may debug the addin dll.";
                }

                MessageBox.Show(message, "DesignAutomationHandler");

            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.ToString(), "DesignAutomationHandler");
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            Result r = Result.Succeeded;
            try
            {
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                r = Result.Failed;
            }
            return r;
        }


        /// <summary>
        /// Loads the PNG image from resource.
        /// </summary>
        /// <param name="imageResourceName">Name of the image resource.</param>
        /// <returns></returns>
        private ImageSource LoadPNGImageFromResource(string imageResourceName)
        {

            ImageSource retVal = null;
            try
            {
                Assembly dotNetAssembly = Assembly.GetExecutingAssembly();
                System.IO.Stream iconStream = dotNetAssembly.GetManifestResourceStream(imageResourceName);
                if (iconStream != null)
                {
                    PngBitmapDecoder bitmapDecoder = new PngBitmapDecoder(iconStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    ImageSource imageSource = bitmapDecoder.Frames[0];
                    retVal = imageSource;
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            return retVal;
        }
    }
}

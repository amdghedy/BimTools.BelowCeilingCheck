using System;
using System.Reflection;
using Autodesk.Revit.UI;
using BimTools.BelowCeilingCheck.Commands;
using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck
{
    /// <summary>
    /// Registers the "BIM TOOLS" ribbon tab and the Below Ceiling Check button.
    /// </summary>
    public class App : IExternalApplication
    {
        public const string TabName = "BIM TOOLS";
        public const string PanelName = "Coordination";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                EnsureTab(application);
                RibbonPanel panel = GetOrCreatePanel(application);

                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                PushButtonData buttonData = new PushButtonData(
                    "BimTools_BelowCeilingCheck",
                    "Below Ceiling\nCheck",
                    assemblyPath,
                    typeof(BelowCeilingCheckCommand).FullName);

                buttonData.ToolTip = "Find MEP elements located below the ceiling.";
                buttonData.LongDescription =
                    "Coordination check performed after clash detection. Reports MEP elements (host or linked) " +
                    "that sit below the ceiling underside, including partially intersecting elements and headroom violations.";
                buttonData.LargeImage = IconFactory.CreateIcon(32);
                buttonData.Image = IconFactory.CreateIcon(16);

                panel.AddItem(buttonData);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("BIM TOOLS - Below Ceiling Check", "The add-in could not create its ribbon button.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            BelowCeilingCheckCommand.CloseWindow();
            return Result.Succeeded;
        }

        private static void EnsureTab(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // The tab already exists because another BIM TOOLS add-in created it first.
            }
        }

        private static RibbonPanel GetOrCreatePanel(UIControlledApplication application)
        {
            foreach (RibbonPanel existing in application.GetRibbonPanels(TabName))
            {
                if (string.Equals(existing.Name, PanelName, StringComparison.Ordinal))
                {
                    return existing;
                }
            }

            return application.CreateRibbonPanel(TabName, PanelName);
        }
    }
}

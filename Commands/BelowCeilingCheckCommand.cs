using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.BelowCeilingCheck.Infrastructure;
using BimTools.BelowCeilingCheck.Services;
using BimTools.BelowCeilingCheck.ViewModels;
using BimTools.BelowCeilingCheck.Views;

namespace BimTools.BelowCeilingCheck.Commands
{
    /// <summary>
    /// Opens the modeless Below Ceiling Check window. All Revit API work done by the window
    /// is marshalled through an ExternalEvent dispatcher.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BelowCeilingCheckCommand : IExternalCommand
    {
        private const string DialogTitle = "BIM TOOLS - Below Ceiling Check";

        private static MainWindow _window;
        private static RevitActionDispatcher _dispatcher;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;

                if (uiDoc == null)
                {
                    TaskDialog.Show(DialogTitle, "Open a Revit project before running this tool.");
                    return Result.Cancelled;
                }

                if (uiDoc.Document.IsFamilyDocument)
                {
                    TaskDialog.Show(DialogTitle, "This tool works in project documents only, not in the Family Editor.");
                    return Result.Cancelled;
                }

                if (_window != null)
                {
                    MainViewModel existing = _window.DataContext as MainViewModel;
                    if (existing != null)
                    {
                        existing.LoadModel(uiApp);
                    }

                    if (_window.WindowState == WindowState.Minimized)
                    {
                        _window.WindowState = WindowState.Normal;
                    }

                    _window.Activate();
                    return Result.Succeeded;
                }

                _dispatcher = new RevitActionDispatcher();
                MainViewModel viewModel = new MainViewModel(_dispatcher, new DialogService(), new SettingsService());
                viewModel.LoadModel(uiApp);

                _window = new MainWindow();
                _window.DataContext = viewModel;
                new WindowInteropHelper(_window).Owner = Process.GetCurrentProcess().MainWindowHandle;
                _window.Closed += OnWindowClosed;
                _window.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(DialogTitle, "The tool could not be started.\n\n" + ex.Message);
                return Result.Failed;
            }
        }

        public static void CloseWindow()
        {
            if (_window != null)
            {
                _window.Close();
            }
        }

        private static void OnWindowClosed(object sender, EventArgs e)
        {
            _window = null;
            if (_dispatcher != null)
            {
                _dispatcher.Dispose();
                _dispatcher = null;
            }
        }
    }
}

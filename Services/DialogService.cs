using System;
using System.ComponentModel;
using System.Diagnostics;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace BimTools.BelowCeilingCheck.Services
{
    public sealed class DialogService : IDialogService
    {
        public void ShowInfo(string title, string message)
        {
            TaskDialog dialog = new TaskDialog(title);
            dialog.MainContent = message;
            dialog.CommonButtons = TaskDialogCommonButtons.Close;
            dialog.Show();
        }

        public void ShowError(string title, string message)
        {
            TaskDialog dialog = new TaskDialog(title);
            dialog.MainIcon = TaskDialogIcon.TaskDialogIconError;
            dialog.MainContent = message;
            dialog.CommonButtons = TaskDialogCommonButtons.Close;
            dialog.Show();
        }

        public bool Confirm(string title, string message)
        {
            TaskDialog dialog = new TaskDialog(title);
            dialog.MainContent = message;
            dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
            dialog.DefaultButton = TaskDialogResult.No;
            return dialog.Show() == TaskDialogResult.Yes;
        }

        public string PromptSaveFile(string title, string filter, string defaultFileName)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = title;
            dialog.Filter = filter;
            dialog.FileName = defaultFileName;
            dialog.OverwritePrompt = true;
            bool? result = dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }

        public bool CopyToClipboard(string text)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                return false;
            }
        }

        public void OpenPath(string path)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(path);
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Win32Exception ex)
            {
                ShowError("BIM TOOLS - Below Ceiling Check", "The file could not be opened.\n\n" + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                ShowError("BIM TOOLS - Below Ceiling Check", "The file could not be opened.\n\n" + ex.Message);
            }
        }
    }
}

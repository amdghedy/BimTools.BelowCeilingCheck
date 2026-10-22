namespace BimTools.BelowCeilingCheck.Services
{
    public interface IDialogService
    {
        void ShowInfo(string title, string message);

        void ShowError(string title, string message);

        bool Confirm(string title, string message);

        /// <summary>Returns the chosen path, or null when the user cancels.</summary>
        string PromptSaveFile(string title, string filter, string defaultFileName);

        void OpenPath(string path);

        /// <summary>Returns false when the clipboard is locked by another application.</summary>
        bool CopyToClipboard(string text);
    }
}

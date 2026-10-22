using System;
using System.Windows.Threading;

namespace BimTools.BelowCeilingCheck.Infrastructure
{
    /// <summary>
    /// Lets the WPF window repaint and process input (for example the Cancel button)
    /// while a long Revit API loop is running on the UI thread.
    /// </summary>
    public static class UiPump
    {
        public static void Pump()
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}

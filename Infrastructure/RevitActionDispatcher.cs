using System;
using System.Collections.Concurrent;
using Autodesk.Revit.UI;

namespace BimTools.BelowCeilingCheck.Infrastructure
{
    /// <summary>
    /// Queues actions raised from the modeless window and runs them inside a valid Revit API context.
    /// Must be constructed from within an IExternalCommand.Execute call.
    /// </summary>
    public sealed class RevitActionDispatcher : IExternalEventHandler, IDisposable
    {
        private readonly ConcurrentQueue<Action<UIApplication>> _queue = new ConcurrentQueue<Action<UIApplication>>();
        private readonly ExternalEvent _externalEvent;

        public RevitActionDispatcher()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        public void Post(Action<UIApplication> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            _queue.Enqueue(action);
            _externalEvent.Raise();
        }

        public void Execute(UIApplication app)
        {
            Action<UIApplication> action;
            while (_queue.TryDequeue(out action))
            {
                try
                {
                    action(app);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("BIM TOOLS - Below Ceiling Check", "The requested operation failed.\n\n" + ex.Message);
                }
            }
        }

        public string GetName()
        {
            return "BIM TOOLS - Below Ceiling Check Dispatcher";
        }

        public void Dispose()
        {
            _externalEvent.Dispose();
        }
    }
}

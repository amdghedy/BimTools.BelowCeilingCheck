using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace BimTools.BelowCeilingCheck.Models
{
    /// <summary>
    /// Snapshot of everything the check engine needs, captured on the UI before the run starts.
    /// </summary>
    public sealed class CheckRequest
    {
        public CheckRequest()
        {
            Categories = new List<BuiltInCategory>();
            Links = new List<LinkOption>();
        }

        public CheckSettings Settings { get; set; }

        public List<BuiltInCategory> Categories { get; private set; }

        /// <summary>Ceiling levels to include; null means no level filter.</summary>
        public HashSet<string> LevelNames { get; set; }

        public IList<LinkOption> Links { get; set; }

        public Func<bool> IsCancelled { get; set; }

        public Action<double, string> Progress { get; set; }

        public void ReportProgress(double fraction, string text)
        {
            Action<double, string> progress = Progress;
            if (progress != null)
            {
                progress(Math.Max(0.0, Math.Min(1.0, fraction)), text);
            }
        }

        public void ThrowIfCancelled()
        {
            Func<bool> cancelled = IsCancelled;
            if (cancelled != null && cancelled())
            {
                throw new OperationCanceledException();
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace BimTools.BelowCeilingCheck.Models
{
    public sealed class CheckSummary
    {
        public CheckSummary()
        {
            ByCategory = new List<SummaryLine>();
            ByLevel = new List<SummaryLine>();
            BySystem = new List<SummaryLine>();
            RunDate = DateTime.Now;
            ModelTitle = string.Empty;
        }

        public string ModelTitle { get; set; }

        public DateTime RunDate { get; set; }

        public TimeSpan Duration { get; set; }

        public int ElementsChecked { get; set; }

        public int CeilingFaceCount { get; set; }

        public int ElementsWithoutGeometry { get; set; }

        public int ElementsIgnored { get; set; }

        public int CriticalCount { get; set; }

        public int WarningCount { get; set; }

        public int HeadroomCount { get; set; }

        public List<SummaryLine> ByCategory { get; private set; }

        public List<SummaryLine> ByLevel { get; private set; }

        public List<SummaryLine> BySystem { get; private set; }

        public CheckSettings Settings { get; set; }
    }
}

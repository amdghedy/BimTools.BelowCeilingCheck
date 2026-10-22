using System.Collections.Generic;

namespace BimTools.BelowCeilingCheck.Models
{
    public sealed class CheckResult
    {
        public CheckResult()
        {
            Items = new List<CheckResultItem>();
            Log = new List<string>();
            Summary = new CheckSummary();
        }

        public List<CheckResultItem> Items { get; private set; }

        public List<string> Log { get; private set; }

        public CheckSummary Summary { get; set; }
    }
}

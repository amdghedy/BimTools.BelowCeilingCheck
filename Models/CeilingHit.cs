namespace BimTools.BelowCeilingCheck.Models
{
    /// <summary>One ceiling that an element is below, with the worst depth found against that ceiling.</summary>
    public sealed class CeilingHit
    {
        /// <summary>Element id of the ceiling in its own document; -1 for the fixed-height reference.</summary>
        public int IdValue { get; set; }

        public bool IsLinked { get; set; }

        /// <summary>RevitLinkInstance id in the host model when the ceiling is linked.</summary>
        public int LinkInstanceId { get; set; }

        public string SourceName { get; set; }

        public string Name { get; set; }

        public string LevelName { get; set; }

        /// <summary>Ceiling underside elevation at the position of the worst depth (feet).</summary>
        public double CeilingZFt { get; set; }

        /// <summary>Largest distance the element goes below this ceiling (feet).</summary>
        public double DepthFt { get; set; }

        public int SampleCount { get; set; }
    }
}

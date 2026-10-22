namespace BimTools.BelowCeilingCheck.Models
{
    public enum Severity
    {
        /// <summary>The element lies entirely below the ceiling underside.</summary>
        Critical = 0,

        /// <summary>The element crosses the ceiling plane (partly below, partly above).</summary>
        Warning = 1
    }
}

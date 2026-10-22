using Autodesk.Revit.DB;

namespace BimTools.BelowCeilingCheck.Infrastructure
{
    /// <summary>
    /// Keeps ElementId access compatible with Revit releases before and after the
    /// transition from IntegerValue (32-bit) to Value (64-bit).
    /// </summary>
    internal static class ElementIdCompat
    {
        public static long GetValue(ElementId id)
        {
#if REVIT2026_OR_GREATER
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }
    }
}

using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>A temporary DirectShape that copies the geometry of a linked element or ceiling so it can be coloured.</summary>
    public sealed class MarkerInfo
    {
        public MarkerInfo(ElementId id, bool isCeiling, Severity severity)
        {
            Id = id;
            IsCeiling = isCeiling;
            Severity = severity;
        }

        public ElementId Id { get; private set; }

        public bool IsCeiling { get; private set; }

        public Severity Severity { get; private set; }
    }
}

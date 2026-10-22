using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>An element or ceiling that lives in a linked model and takes part in a finding.</summary>
    public sealed class LinkedTarget
    {
        public LinkedTarget(int instanceId, int elementId, bool isCeiling, Severity severity, string sourceName)
        {
            InstanceId = instanceId;
            ElementId = elementId;
            IsCeiling = isCeiling;
            Severity = severity;
            SourceName = string.IsNullOrEmpty(sourceName) ? "a linked model" : sourceName;
        }

        /// <summary>Id of the RevitLinkInstance in the host model.</summary>
        public int InstanceId { get; private set; }

        /// <summary>Id of the element inside the linked document.</summary>
        public int ElementId { get; private set; }

        public bool IsCeiling { get; private set; }

        public Severity Severity { get; private set; }

        public string SourceName { get; private set; }

        /// <summary>True when a coloured marker solid was created for this target.</summary>
        public bool Covered { get; set; }
    }
}

using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Everything a visual-check action works on: host elements and ceilings, linked targets and their markers.</summary>
    public sealed class TargetSet
    {
        public TargetSet()
        {
            Elements = new List<KeyValuePair<ElementId, Severity>>();
            Ceilings = new List<ElementId>();
            Markers = new List<MarkerInfo>();
            LinkedTargets = new List<LinkedTarget>();
            Report = new OperationReport();
        }

        public List<KeyValuePair<ElementId, Severity>> Elements { get; private set; }

        public List<ElementId> Ceilings { get; private set; }

        public List<MarkerInfo> Markers { get; private set; }

        public List<LinkedTarget> LinkedTargets { get; private set; }

        public int WithoutCeilingElement { get; set; }

        public OperationReport Report { get; private set; }

        /// <summary>Host elements, host ceilings and markers: what gets selected and coloured.</summary>
        public List<ElementId> SelectionIds()
        {
            List<ElementId> ids = new List<ElementId>();
            foreach (KeyValuePair<ElementId, Severity> pair in Elements)
            {
                ids.Add(pair.Key);
            }

            ids.AddRange(Ceilings);
            foreach (MarkerInfo marker in Markers)
            {
                ids.Add(marker.Id);
            }

            return ids;
        }

        /// <summary>Selection plus the link instances whose linked targets have no marker, so those links stay visible when isolating.</summary>
        public List<ElementId> IsolationIds()
        {
            List<ElementId> ids = SelectionIds();
            HashSet<int> added = new HashSet<int>();
            foreach (LinkedTarget target in LinkedTargets)
            {
                if (!target.Covered && added.Add(target.InstanceId))
                {
                    ids.Add(new ElementId(target.InstanceId));
                }
            }

            return ids;
        }

        /// <summary>Link instances that can be hidden because every linked target in them is shown as a marker.</summary>
        public List<ElementId> FullyCoveredLinkIds()
        {
            Dictionary<int, bool> covered = new Dictionary<int, bool>();
            foreach (LinkedTarget target in LinkedTargets)
            {
                bool all;
                covered[target.InstanceId] = covered.TryGetValue(target.InstanceId, out all) ? all && target.Covered : target.Covered;
            }

            List<ElementId> ids = new List<ElementId>();
            foreach (KeyValuePair<int, bool> pair in covered)
            {
                if (pair.Value)
                {
                    ids.Add(new ElementId(pair.Key));
                }
            }

            return ids;
        }
    }
}

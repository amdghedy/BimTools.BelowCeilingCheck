using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Models;
using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Reads levels and Revit links from the active document.</summary>
    public sealed class ModelInfoService
    {
        public List<LevelOption> GetLevels(Document doc)
        {
            List<LevelOption> levels = new List<LevelOption>();

            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(Level));
                foreach (Element element in collector)
                {
                    try
                    {
                        Level level = element as Level;
                        if (level != null)
                        {
                            levels.Add(new LevelOption(level.Name, level.Elevation));
                        }
                    }
                    finally
                    {
                        element.Dispose();
                    }
                }
            }

            levels.Sort(delegate (LevelOption a, LevelOption b) { return a.Elevation.CompareTo(b.Elevation); });
            return levels;
        }

        public List<LinkOption> GetLinks(Document doc)
        {
            List<LinkOption> links = new List<LinkOption>();

            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(RevitLinkInstance));
                foreach (Element element in collector)
                {
                    try
                    {
                        RevitLinkInstance instance = element as RevitLinkInstance;
                        if (instance == null)
                        {
                            continue;
                        }

                        Document linkDoc = instance.GetLinkDocument();
                        links.Add(new LinkOption(instance.Name, (int)ElementIdCompat.GetValue(instance.Id), linkDoc != null));
                    }
                    finally
                    {
                        element.Dispose();
                    }
                }
            }

            links.Sort(delegate (LinkOption a, LinkOption b) { return string.CompareOrdinal(a.Name, b.Name); });
            return links;
        }
    }
}

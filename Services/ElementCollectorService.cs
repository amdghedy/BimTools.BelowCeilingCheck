using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.BelowCeilingCheck.Models;
using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Creates the collectors that gather MEP elements for the selected scope.</summary>
    public sealed class ElementCollectorService
    {
        public FilteredElementCollector CreateCollector(
            Document doc,
            IList<BuiltInCategory> categories,
            ScopeMode scope,
            bool isHost,
            ICollection<ElementId> selection)
        {
            FilteredElementCollector collector;

            if (isHost && scope == ScopeMode.ActiveView)
            {
                View view = doc.ActiveView;
                if (view == null || view is ViewSheet || view is ViewSchedule || view.ViewType == ViewType.Legend)
                {
                    throw new InvalidOperationException("The active view is not a graphical view (plan, section, 3D...). Switch to such a view or choose another scope.");
                }

                collector = new FilteredElementCollector(doc, view.Id);
            }
            else if (isHost && scope == ScopeMode.CurrentSelection)
            {
                collector = new FilteredElementCollector(doc, new List<ElementId>(selection));
            }
            else
            {
                collector = new FilteredElementCollector(doc);
            }

            List<BuiltInCategory> list = new List<BuiltInCategory>(categories);
            collector.WherePasses(new ElementMulticategoryFilter(list)).WhereElementIsNotElementType();
            return collector;
        }

        public List<ElementId> GetSelectedElementIds(UIDocument uiDoc, ICollection<BuiltInCategory> categories)
        {
            List<ElementId> result = new List<ElementId>();
            Document doc = uiDoc.Document;

            foreach (ElementId id in uiDoc.Selection.GetElementIds())
            {
                Element element = doc.GetElement(id);
                try
                {
                    if (element == null || element.Category == null)
                    {
                        continue;
                    }

                    BuiltInCategory category = (BuiltInCategory)ElementIdCompat.GetValue(element.Category.Id);
                    if (categories.Contains(category))
                    {
                        result.Add(id);
                    }
                }
                finally
                {
                    if (element != null)
                    {
                        element.Dispose();
                    }
                }
            }

            return result;
        }
    }
}

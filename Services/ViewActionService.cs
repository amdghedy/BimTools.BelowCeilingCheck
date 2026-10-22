using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>
    /// Review actions on the Revit model: selection, zoom, isolation, colouring, section boxes,
    /// review views and comment marking. Must be called from a valid Revit API context.
    /// Every model change runs inside its own Transaction.
    /// </summary>
    public sealed class ViewActionService
    {
        public const string MarkerTag = "[BCC]";
        public const string MarkerApplicationId = "BimTools.BelowCeilingCheck.Marker";

        private static readonly Color CriticalColor = new Color(229, 57, 53);
        private static readonly Color WarningColor = new Color(255, 152, 0);

        public OperationReport Select(UIDocument uiDoc, IList<CheckResultItem> items)
        {
            OperationReport report = new OperationReport();
            List<ElementId> ids = new List<ElementId>();

            foreach (CheckResultItem item in items)
            {
                if (item.IsLinked)
                {
                    report.Skipped++;
                    continue;
                }

                using (Element element = uiDoc.Document.GetElement(new ElementId(item.ElementIdValue)))
                {
                    if (element == null)
                    {
                        report.Failed++;
                        continue;
                    }

                    ids.Add(element.Id);
                }
            }

            uiDoc.Selection.SetElementIds(ids);
            report.Succeeded = ids.Count;
            return report;
        }

        public OperationReport Zoom(UIDocument uiDoc, IList<CheckResultItem> items, double paddingFt)
        {
            OperationReport report = new OperationReport();
            XYZ min;
            XYZ max;
            Union(items, paddingFt, false, out min, out max);

            if (ZoomRect(uiDoc, min, max))
            {
                report.Succeeded = items.Count;
            }
            else
            {
                report.Failed = items.Count;
            }

            return report;
        }

        /// <summary>
        /// Selects the flagged elements (and optionally their ceilings), colours them in the active view
        /// (element = severity colour, ceiling = translucent cyan) and zooms to the gap between them.
        /// When isolating, host and linked elements stay visible: linked elements and ceilings are shown as
        /// coloured temporary marker solids, or, when markers are off, their whole link stays visible.
        /// </summary>
        public OperationReport Highlight(UIDocument uiDoc, IList<CheckResultItem> items, double paddingFt, bool includeCeilings, bool isolate, bool markLinked)
        {
            Document doc = uiDoc.Document;
            View view = doc.ActiveView;
            TargetSet targets = CollectTargets(doc, items, includeCeilings);
            ElementId solidFill = FindSolidFillId(doc);
            List<string> extraNotes = new List<string>();

            using (Transaction transaction = new Transaction(doc, includeCeilings ? "BCC Visual check" : "BCC Highlight"))
            {
                transaction.Start();

                if (isolate && markLinked && targets.LinkedTargets.Count > 0)
                {
                    RemoveMarkers(doc);
                    CreateMarkers(doc, targets);
                }

                ApplyOverrides(view, targets, solidFill);

                if (includeCeilings && targets.Ceilings.Count > 0)
                {
                    extraNotes.Add(EnsureCategoryVisible(view, BuiltInCategory.OST_Ceilings, "Ceilings"));
                }

                if (targets.Markers.Count > 0)
                {
                    extraNotes.Add(EnsureCategoryVisible(view, BuiltInCategory.OST_GenericModel, "Generic Models"));
                }

                if (isolate)
                {
                    if (!view.CanUseTemporaryVisibilityModes())
                    {
                        throw new InvalidOperationException("The active view does not support temporary isolate. Activate a section, 3D or ceiling plan view.");
                    }

                    List<ElementId> isolateIds = targets.IsolationIds();
                    if (isolateIds.Count > 0)
                    {
                        view.IsolateElementsTemporary(isolateIds);
                    }
                }

                transaction.Commit();
            }

            List<ElementId> selection = targets.SelectionIds();
            uiDoc.Selection.SetElementIds(selection);

            XYZ min;
            XYZ max;
            PairBounds(items, paddingFt, out min, out max);
            ZoomRect(uiDoc, min, max);

            OperationReport report = targets.Report;
            report.Succeeded = selection.Count;
            report.Note = BuildNote(targets, view, includeCeilings, isolate, extraNotes);
            return report;
        }

        /// <summary>Creates a 3D view cropped by a section box around the elements and their ceilings, with both coloured.</summary>
        public OperationReport CreateCheckView(UIDocument uiDoc, IList<CheckResultItem> items, double paddingFt, bool markLinked)
        {
            Document doc = uiDoc.Document;
            ElementId viewTypeId = Find3DViewTypeId(doc);
            if (viewTypeId.IntegerValue == ElementId.InvalidElementId.IntegerValue)
            {
                throw new InvalidOperationException("The project has no 3D view type, so a check view cannot be created.");
            }

            TargetSet targets = CollectTargets(doc, items, true);
            ElementId solidFill = FindSolidFillId(doc);
            List<string> extraNotes = new List<string>();
            XYZ min;
            XYZ max;
            PairBounds(items, paddingFt, out min, out max);
            View3D created;

            using (Transaction transaction = new Transaction(doc, "BCC Create check view"))
            {
                transaction.Start();

                if (markLinked && targets.LinkedTargets.Count > 0)
                {
                    RemoveMarkers(doc);
                    CreateMarkers(doc, targets);
                }

                created = View3D.CreateIsometric(doc, viewTypeId);
                string label = items.Count == 1 ? items[0].ElementIdValue.ToString() : items.Count + " items";
                created.Name = "BCC Check " + label + " " + DateTime.Now.ToString("yyyyMMdd-HHmmss");

                BoundingBoxXYZ box = new BoundingBoxXYZ();
                box.Min = min;
                box.Max = max;
                created.IsSectionBoxActive = true;
                created.SetSectionBox(box);
                ApplyOverrides(created, targets, solidFill);

                if (targets.Markers.Count > 0)
                {
                    extraNotes.Add(EnsureCategoryVisible(created, BuiltInCategory.OST_GenericModel, "Generic Models"));

                    // The markers replace the linked originals, so those links are hidden in this view only.
                    List<ElementId> hideLinks = targets.FullyCoveredLinkIds();
                    if (hideLinks.Count > 0)
                    {
                        created.HideElements(hideLinks);
                    }
                }

                transaction.Commit();
            }

            uiDoc.ActiveView = created;
            List<ElementId> selection = targets.SelectionIds();
            uiDoc.Selection.SetElementIds(selection);

            OperationReport report = targets.Report;
            report.Succeeded = selection.Count;
            report.Note = BuildNote(targets, created, true, false, extraNotes);
            return report;
        }

        /// <summary>Removes colour overrides, deletes marker solids and ends temporary isolate.</summary>
        public OperationReport ClearHighlight(Document doc, IList<CheckResultItem> items)
        {
            View view = doc.ActiveView;
            TargetSet targets = CollectTargets(doc, items, true);
            int removedMarkers;

            using (Transaction transaction = new Transaction(doc, "BCC Clear highlight"))
            {
                transaction.Start();

                foreach (ElementId id in targets.SelectionIds())
                {
                    using (OverrideGraphicSettings blank = new OverrideGraphicSettings())
                    {
                        view.SetElementOverrides(id, blank);
                    }
                }

                if (view.IsInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate))
                {
                    view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                }

                removedMarkers = RemoveMarkers(doc);
                transaction.Commit();
            }

            OperationReport report = targets.Report;
            report.Succeeded = targets.SelectionIds().Count + removedMarkers;
            return report;
        }

        private static TargetSet CollectTargets(Document doc, IList<CheckResultItem> items, bool includeCeilings)
        {
            TargetSet set = new TargetSet();
            HashSet<int> seenElements = new HashSet<int>();
            HashSet<int> seenCeilings = new HashSet<int>();
            HashSet<string> seenLinked = new HashSet<string>();

            foreach (CheckResultItem item in items)
            {
                if (item.IsLinked)
                {
                    if (seenLinked.Add(item.LinkInstanceIdValue + ":" + item.ElementIdValue + ":e"))
                    {
                        set.LinkedTargets.Add(new LinkedTarget(item.LinkInstanceIdValue, item.ElementIdValue, false, item.Severity, item.SourceName));
                    }
                }
                else if (seenElements.Add(item.ElementIdValue))
                {
                    ElementId id = new ElementId(item.ElementIdValue);
                    if (ElementExists(doc, id))
                    {
                        set.Elements.Add(new KeyValuePair<ElementId, Severity>(id, item.Severity));
                    }
                    else
                    {
                        set.Report.Failed++;
                    }
                }

                if (!includeCeilings)
                {
                    continue;
                }

                bool hasCeilingElement = false;
                foreach (CeilingHit hit in item.CeilingHits)
                {
                    if (hit.IdValue <= 0)
                    {
                        continue;
                    }

                    hasCeilingElement = true;
                    if (hit.IsLinked)
                    {
                        if (seenLinked.Add(hit.LinkInstanceId + ":" + hit.IdValue + ":c"))
                        {
                            set.LinkedTargets.Add(new LinkedTarget(hit.LinkInstanceId, hit.IdValue, true, item.Severity, hit.SourceName));
                        }

                        continue;
                    }

                    if (seenCeilings.Add(hit.IdValue))
                    {
                        ElementId ceilingId = new ElementId(hit.IdValue);
                        if (ElementExists(doc, ceilingId))
                        {
                            set.Ceilings.Add(ceilingId);
                        }
                        else
                        {
                            set.Report.Failed++;
                        }
                    }
                }

                if (!hasCeilingElement)
                {
                    set.WithoutCeilingElement++;
                }
            }

            return set;
        }

        private static bool ElementExists(Document doc, ElementId id)
        {
            using (Element element = doc.GetElement(id))
            {
                return element != null;
            }
        }

        private static void ApplyOverrides(View view, TargetSet targets, ElementId solidFill)
        {
            foreach (KeyValuePair<ElementId, Severity> pair in targets.Elements)
            {
                using (OverrideGraphicSettings settings = BuildOverride(pair.Value, solidFill))
                {
                    view.SetElementOverrides(pair.Key, settings);
                }
            }

            foreach (ElementId ceilingId in targets.Ceilings)
            {
                using (OverrideGraphicSettings settings = BuildCeilingOverride(solidFill))
                {
                    view.SetElementOverrides(ceilingId, settings);
                }
            }

            foreach (MarkerInfo marker in targets.Markers)
            {
                using (OverrideGraphicSettings settings = marker.IsCeiling ? BuildCeilingOverride(solidFill) : BuildOverride(marker.Severity, solidFill))
                {
                    view.SetElementOverrides(marker.Id, settings);
                }
            }
        }

        private static OverrideGraphicSettings BuildCeilingOverride(ElementId solidFill)
        {
            Color cyan = new Color(0, 176, 255);
            OverrideGraphicSettings settings = new OverrideGraphicSettings();
            settings.SetProjectionLineColor(cyan);
            settings.SetSurfaceForegroundPatternColor(cyan);
            settings.SetSurfaceTransparency(35);
            if (solidFill.IntegerValue != ElementId.InvalidElementId.IntegerValue)
            {
                settings.SetSurfaceForegroundPatternId(solidFill);
            }

            return settings;
        }

        /// <summary>
        /// Copies the geometry of every linked target into a DirectShape in the host model so it can be coloured.
        /// Targets whose geometry cannot be copied stay uncovered and keep their link visible instead.
        /// </summary>
        private static void CreateMarkers(Document doc, TargetSet targets)
        {
            ElementId category = new ElementId(BuiltInCategory.OST_GenericModel);

            foreach (LinkedTarget target in targets.LinkedTargets)
            {
                using (Element instanceElement = doc.GetElement(new ElementId(target.InstanceId)))
                {
                    RevitLinkInstance instance = instanceElement as RevitLinkInstance;
                    Document linkDoc = instance == null ? null : instance.GetLinkDocument();
                    if (linkDoc == null)
                    {
                        continue;
                    }

                    List<GeometryObject> shapes;
                    using (Element source = linkDoc.GetElement(new ElementId(target.ElementId)))
                    {
                        if (source == null)
                        {
                            continue;
                        }

                        shapes = CopyGeometry(source, instance.GetTotalTransform());
                    }

                    if (shapes.Count == 0)
                    {
                        continue;
                    }

                    DirectShape marker = null;
                    try
                    {
                        marker = DirectShape.CreateElement(doc, category);
                        marker.ApplicationId = MarkerApplicationId;
                        marker.ApplicationDataId = Guid.NewGuid().ToString();
                        marker.SetShape(shapes);

                        using (Parameter comments = marker.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS))
                        {
                            if (comments != null && !comments.IsReadOnly)
                            {
                                comments.Set("BCC temporary marker - delete before issuing the model");
                            }
                        }

                        targets.Markers.Add(new MarkerInfo(marker.Id, target.IsCeiling, target.Severity));
                        target.Covered = true;
                    }
                    catch (ArgumentException)
                    {
                        DeleteQuietly(doc, marker);
                    }
                    catch (InvalidOperationException)
                    {
                        DeleteQuietly(doc, marker);
                    }
                }
            }
        }

        private static void DeleteQuietly(Document doc, Element element)
        {
            if (element != null && element.IsValidObject)
            {
                doc.Delete(element.Id);
            }
        }

        private static List<GeometryObject> CopyGeometry(Element source, Transform transform)
        {
            List<GeometryObject> shapes = new List<GeometryObject>();
            using (Options options = new Options())
            {
                options.ComputeReferences = false;
                options.DetailLevel = ViewDetailLevel.Fine;
                options.IncludeNonVisibleObjects = false;

                using (GeometryElement geometry = source.get_Geometry(options))
                {
                    if (geometry != null)
                    {
                        AppendSolids(geometry, transform, shapes);
                    }
                }
            }

            return shapes;
        }

        private static void AppendSolids(GeometryElement geometry, Transform transform, List<GeometryObject> shapes)
        {
            foreach (GeometryObject geometryObject in geometry)
            {
                Solid solid = geometryObject as Solid;
                if (solid != null)
                {
                    if (!solid.Faces.IsEmpty && solid.Volume > 1e-9)
                    {
                        shapes.Add(SolidUtils.CreateTransformed(solid, transform));
                    }

                    continue;
                }

                GeometryInstance instance = geometryObject as GeometryInstance;
                if (instance != null)
                {
                    using (GeometryElement inner = instance.GetInstanceGeometry())
                    {
                        if (inner != null)
                        {
                            AppendSolids(inner, transform, shapes);
                        }
                    }
                }
            }
        }

        /// <summary>Deletes all marker solids created by this tool. Returns how many were removed.</summary>
        private static int RemoveMarkers(Document doc)
        {
            List<ElementId> ids = new List<ElementId>();
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(DirectShape));
                foreach (Element element in collector)
                {
                    try
                    {
                        DirectShape shape = element as DirectShape;
                        if (shape != null && string.Equals(shape.ApplicationId, MarkerApplicationId, StringComparison.Ordinal))
                        {
                            ids.Add(shape.Id);
                        }
                    }
                    finally
                    {
                        element.Dispose();
                    }
                }
            }

            if (ids.Count > 0)
            {
                doc.Delete(ids);
            }

            return ids.Count;
        }

        /// <summary>Unhides the category when it is hidden. Returns a note when that is not possible or was needed.</summary>
        private static string EnsureCategoryVisible(View view, BuiltInCategory category, string label)
        {
            ElementId id = new ElementId(category);
            if (!view.CanCategoryBeHidden(id) || !view.GetCategoryHidden(id))
            {
                return null;
            }

            try
            {
                view.SetCategoryHidden(id, false);
                return "The " + label + " category was hidden in this view and has been switched on.";
            }
            catch (InvalidOperationException)
            {
                return "The " + label + " category is hidden by the view template of this view, so those elements cannot be seen here.";
            }
        }

        private static string BuildNote(TargetSet targets, View view, bool includeCeilings, bool isolate, List<string> extraNotes)
        {
            List<string> lines = new List<string>();

            if (targets.Markers.Count > 0)
            {
                lines.Add(targets.Markers.Count + " linked element(s)/ceiling(s) are shown as temporary coloured marker solids "
                    + "(copies of their geometry, category Generic Models). Use 'Clear highlight and isolation' to delete them before you save or issue the model.");
            }

            List<string> uncoveredSources = new List<string>();
            int uncovered = 0;
            foreach (LinkedTarget target in targets.LinkedTargets)
            {
                if (target.Covered)
                {
                    continue;
                }

                uncovered++;
                if (!uncoveredSources.Contains(target.SourceName))
                {
                    uncoveredSources.Add(target.SourceName);
                }
            }

            if (uncovered > 0)
            {
                string names = string.Join("', '", uncoveredSources.ToArray());
                if (isolate)
                {
                    lines.Add(uncovered + " linked element(s)/ceiling(s) could not be coloured (turn on 'Colour linked elements and ceilings' in Settings, or their geometry could not be copied). "
                        + "The whole link '" + names + "' was kept visible in the isolation so you can still see them; Revit 2020 cannot select or colour linked elements by code.");
                }
                else
                {
                    lines.Add(uncovered + " linked element(s)/ceiling(s) in '" + names + "' were not selected or coloured because Revit 2020 cannot select linked elements by code. "
                        + "Use 'Visual check, isolated' or 'Create 3D check view' to see them coloured.");
                }
            }

            if (includeCeilings)
            {
                if (targets.WithoutCeilingElement > 0)
                {
                    lines.Add(targets.WithoutCeilingElement + " result(s) have no ceiling element (fixed-height or headroom finding).");
                }

                bool planView = view.ViewType == ViewType.FloorPlan
                    || view.ViewType == ViewType.EngineeringPlan
                    || view.ViewType == ViewType.AreaPlan;
                if (planView && targets.Ceilings.Count > 0)
                {
                    lines.Add("Ceilings are not displayed in floor plans. Use a reflected ceiling plan, a section, a 3D view or the '3D check view' command to see the ceiling.");
                }
            }

            foreach (string extra in extraNotes)
            {
                if (!string.IsNullOrEmpty(extra))
                {
                    lines.Add(extra);
                }
            }

            return lines.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, lines.ToArray());
        }

        private static bool ZoomRect(UIDocument uiDoc, XYZ min, XYZ max)
        {
            View active = uiDoc.ActiveView;
            bool zoomed = false;
            foreach (UIView uiView in uiDoc.GetOpenUIViews())
            {
                if (uiView.ViewId.IntegerValue == active.Id.IntegerValue)
                {
                    uiView.ZoomAndCenterRectangle(min, max);
                    zoomed = true;
                }
            }

            return zoomed;
        }

        /// <summary>Bounds of the elements plus the ceiling level above them, with padding.</summary>
        private static void PairBounds(IList<CheckResultItem> items, double paddingFt, out XYZ min, out XYZ max)
        {
            XYZ elementMin;
            XYZ elementMax;
            Union(items, paddingFt, true, out elementMin, out elementMax);

            double low = elementMin.Z;
            double high = elementMax.Z;
            foreach (CheckResultItem item in items)
            {
                low = Math.Min(low, item.CeilingElevationFt - 0.5);
                high = Math.Max(high, item.CeilingElevationFt + 1.5);
                foreach (CeilingHit hit in item.CeilingHits)
                {
                    low = Math.Min(low, hit.CeilingZFt - 0.5);
                    high = Math.Max(high, hit.CeilingZFt + 1.5);
                }
            }

            min = new XYZ(elementMin.X, elementMin.Y, low);
            max = new XYZ(elementMax.X, elementMax.Y, high);
        }

        public OperationReport Isolate(Document doc, IList<CheckResultItem> items)
        {
            OperationReport report = new OperationReport();
            View view = doc.ActiveView;
            if (!view.CanUseTemporaryVisibilityModes())
            {
                throw new InvalidOperationException("The active view does not support temporary isolate. Activate a plan, section or 3D view.");
            }

            List<ElementId> ids = GetHostIds(doc, items, report);
            if (ids.Count == 0)
            {
                throw new InvalidOperationException("None of the target elements belong to the host model, so there is nothing to isolate.");
            }

            using (Transaction transaction = new Transaction(doc, "BCC Isolate results"))
            {
                transaction.Start();
                view.IsolateElementsTemporary(ids);
                transaction.Commit();
            }

            report.Succeeded = ids.Count;
            return report;
        }

        public OperationReport ResetIsolation(Document doc)
        {
            OperationReport report = new OperationReport();
            View view = doc.ActiveView;

            if (view.IsInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate))
            {
                using (Transaction transaction = new Transaction(doc, "BCC Reset isolate"))
                {
                    transaction.Start();
                    view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                    transaction.Commit();
                }

                report.Succeeded = 1;
            }
            else
            {
                report.Skipped = 1;
            }

            return report;
        }

        public OperationReport ApplyColors(Document doc, IList<CheckResultItem> items)
        {
            OperationReport report = new OperationReport();
            View view = doc.ActiveView;
            ElementId solidFill = FindSolidFillId(doc);

            using (Transaction transaction = new Transaction(doc, "BCC Colour results"))
            {
                transaction.Start();
                foreach (CheckResultItem item in items)
                {
                    if (item.IsLinked)
                    {
                        report.Skipped++;
                        continue;
                    }

                    ElementId id = new ElementId(item.ElementIdValue);
                    using (Element element = doc.GetElement(id))
                    {
                        if (element == null)
                        {
                            report.Failed++;
                            continue;
                        }
                    }

                    using (OverrideGraphicSettings settings = BuildOverride(item.Severity, solidFill))
                    {
                        view.SetElementOverrides(id, settings);
                    }

                    report.Succeeded++;
                }

                transaction.Commit();
            }

            return report;
        }

        public OperationReport ResetColors(Document doc, IList<CheckResultItem> items)
        {
            OperationReport report = new OperationReport();
            View view = doc.ActiveView;

            using (Transaction transaction = new Transaction(doc, "BCC Reset colours"))
            {
                transaction.Start();
                foreach (CheckResultItem item in items)
                {
                    if (item.IsLinked)
                    {
                        report.Skipped++;
                        continue;
                    }

                    ElementId id = new ElementId(item.ElementIdValue);
                    using (Element element = doc.GetElement(id))
                    {
                        if (element == null)
                        {
                            report.Failed++;
                            continue;
                        }
                    }

                    using (OverrideGraphicSettings settings = new OverrideGraphicSettings())
                    {
                        view.SetElementOverrides(id, settings);
                    }

                    report.Succeeded++;
                }

                transaction.Commit();
            }

            return report;
        }

        public OperationReport SetSectionBox(Document doc, IList<CheckResultItem> items, double paddingFt)
        {
            OperationReport report = new OperationReport();
            View3D view3D = doc.ActiveView as View3D;
            if (view3D == null || view3D.IsPerspective)
            {
                throw new InvalidOperationException("Activate a non-perspective 3D view before applying a section box.");
            }

            XYZ min;
            XYZ max;
            Union(items, paddingFt, true, out min, out max);

            BoundingBoxXYZ box = new BoundingBoxXYZ();
            box.Min = min;
            box.Max = max;

            using (Transaction transaction = new Transaction(doc, "BCC Section box"))
            {
                transaction.Start();
                view3D.IsSectionBoxActive = true;
                view3D.SetSectionBox(box);
                transaction.Commit();
            }

            report.Succeeded = items.Count;
            return report;
        }

        public OperationReport CreateReviewView(UIDocument uiDoc, IList<CheckResultItem> items, double paddingFt)
        {
            OperationReport report = new OperationReport();
            Document doc = uiDoc.Document;
            ElementId viewTypeId = Find3DViewTypeId(doc);
            if (viewTypeId.IntegerValue == ElementId.InvalidElementId.IntegerValue)
            {
                throw new InvalidOperationException("The project has no 3D view type, so a review view cannot be created.");
            }

            XYZ min;
            XYZ max;
            Union(items, paddingFt, true, out min, out max);
            ElementId solidFill = FindSolidFillId(doc);
            View3D created;

            using (Transaction transaction = new Transaction(doc, "BCC Create review view"))
            {
                transaction.Start();
                created = View3D.CreateIsometric(doc, viewTypeId);
                created.Name = "BCC Review " + DateTime.Now.ToString("yyyyMMdd-HHmmss");

                BoundingBoxXYZ box = new BoundingBoxXYZ();
                box.Min = min;
                box.Max = max;
                created.IsSectionBoxActive = true;
                created.SetSectionBox(box);

                foreach (CheckResultItem item in items)
                {
                    if (item.IsLinked)
                    {
                        report.Skipped++;
                        continue;
                    }

                    using (OverrideGraphicSettings settings = BuildOverride(item.Severity, solidFill))
                    {
                        created.SetElementOverrides(new ElementId(item.ElementIdValue), settings);
                    }

                    report.Succeeded++;
                }

                transaction.Commit();
            }

            uiDoc.ActiveView = created;
            return report;
        }

        public OperationReport MarkComments(Document doc, IList<CheckResultItem> items)
        {
            return WriteComments(doc, items, false);
        }

        public OperationReport ClearComments(Document doc, IList<CheckResultItem> items)
        {
            return WriteComments(doc, items, true);
        }

        private static OperationReport WriteComments(Document doc, IList<CheckResultItem> items, bool clear)
        {
            OperationReport report = new OperationReport();
            string date = DateTime.Now.ToString("yyyy-MM-dd");

            using (Transaction transaction = new Transaction(doc, clear ? "BCC Clear comments" : "BCC Mark elements"))
            {
                transaction.Start();
                foreach (CheckResultItem item in items)
                {
                    if (item.IsLinked)
                    {
                        report.Skipped++;
                        continue;
                    }

                    using (Element element = doc.GetElement(new ElementId(item.ElementIdValue)))
                    {
                        if (element == null)
                        {
                            report.Failed++;
                            continue;
                        }

                        using (Parameter comments = element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS))
                        {
                            if (comments == null || comments.IsReadOnly)
                            {
                                report.Failed++;
                                continue;
                            }

                            string existing = comments.AsString() ?? string.Empty;
                            string baseText = StripMarker(existing);
                            string updated;

                            if (clear)
                            {
                                updated = baseText;
                            }
                            else
                            {
                                string subject = item.CeilingName == "Headroom limit" ? "headroom limit" : "ceiling";
                                string tag = MarkerTag + " " + item.DepthText + " below " + subject + " - " + item.SeverityText + " - " + date;
                                updated = baseText.Length > 0 ? baseText + " | " + tag : tag;
                            }

                            if (comments.Set(updated))
                            {
                                report.Succeeded++;
                            }
                            else
                            {
                                report.Failed++;
                            }
                        }
                    }
                }

                transaction.Commit();
            }

            return report;
        }

        private static string StripMarker(string text)
        {
            int index = text.IndexOf(MarkerTag, StringComparison.Ordinal);
            if (index < 0)
            {
                return text;
            }

            return text.Substring(0, index).TrimEnd(' ', '|');
        }

        private static List<ElementId> GetHostIds(Document doc, IList<CheckResultItem> items, OperationReport report)
        {
            List<ElementId> ids = new List<ElementId>();
            foreach (CheckResultItem item in items)
            {
                if (item.IsLinked)
                {
                    report.Skipped++;
                    continue;
                }

                using (Element element = doc.GetElement(new ElementId(item.ElementIdValue)))
                {
                    if (element == null)
                    {
                        report.Failed++;
                        continue;
                    }

                    ids.Add(element.Id);
                }
            }

            return ids;
        }

        private static OverrideGraphicSettings BuildOverride(Severity severity, ElementId solidFill)
        {
            Color color = severity == Severity.Critical ? CriticalColor : WarningColor;
            OverrideGraphicSettings settings = new OverrideGraphicSettings();
            settings.SetProjectionLineColor(color);
            settings.SetSurfaceForegroundPatternColor(color);
            if (solidFill.IntegerValue != ElementId.InvalidElementId.IntegerValue)
            {
                settings.SetSurfaceForegroundPatternId(solidFill);
            }

            return settings;
        }

        private static ElementId FindSolidFillId(Document doc)
        {
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(FillPatternElement));
                foreach (Element element in collector)
                {
                    try
                    {
                        FillPatternElement patternElement = element as FillPatternElement;
                        if (patternElement == null)
                        {
                            continue;
                        }

                        using (FillPattern pattern = patternElement.GetFillPattern())
                        {
                            if (pattern.IsSolidFill && pattern.Target == FillPatternTarget.Drafting)
                            {
                                return patternElement.Id;
                            }
                        }
                    }
                    finally
                    {
                        element.Dispose();
                    }
                }
            }

            return ElementId.InvalidElementId;
        }

        private static ElementId Find3DViewTypeId(Document doc)
        {
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(ViewFamilyType));
                foreach (Element element in collector)
                {
                    try
                    {
                        ViewFamilyType type = element as ViewFamilyType;
                        if (type != null && type.ViewFamily == ViewFamily.ThreeDimensional)
                        {
                            return type.Id;
                        }
                    }
                    finally
                    {
                        element.Dispose();
                    }
                }
            }

            return ElementId.InvalidElementId;
        }

        private static void Union(IList<CheckResultItem> items, double paddingFt, bool useFocus, out XYZ min, out XYZ max)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            foreach (CheckResultItem item in items)
            {
                XYZ lower = useFocus && item.FocusMin != null ? item.FocusMin : item.Min;
                XYZ upper = useFocus && item.FocusMax != null ? item.FocusMax : item.Max;
                if (lower == null || upper == null)
                {
                    continue;
                }

                if (lower.X < minX) minX = lower.X;
                if (lower.Y < minY) minY = lower.Y;
                if (lower.Z < minZ) minZ = lower.Z;
                if (upper.X > maxX) maxX = upper.X;
                if (upper.Y > maxY) maxY = upper.Y;
                if (upper.Z > maxZ) maxZ = upper.Z;
            }

            if (minX > maxX)
            {
                throw new InvalidOperationException("The target elements have no location data.");
            }

            min = new XYZ(minX - paddingFt, minY - paddingFt, minZ - paddingFt);
            max = new XYZ(maxX + paddingFt, maxY + paddingFt, maxZ + paddingFt);
        }
    }
}

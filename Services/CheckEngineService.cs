using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.BelowCeilingCheck.Geometry;
using BimTools.BelowCeilingCheck.Infrastructure;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>
    /// Core check. For each MEP element the lowest point of its bounding box is compared with the
    /// underside of the ceiling that belongs to the same storey at the element's plan position.
    /// Must be called from a valid Revit API context. It is read-only and starts no transaction.
    /// </summary>
    public sealed class CheckEngineService
    {
        private const double FloorStandingFt = 0.5;
        private const int PumpInterval = 100;

        private readonly ModelInfoService _modelInfo = new ModelInfoService();
        private readonly CeilingCollectorService _ceilings = new CeilingCollectorService();
        private readonly ElementCollectorService _elements = new ElementCollectorService();

        public CheckResult Run(UIApplication uiApp, CheckRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;
            CheckSettings settings = request.Settings;
            CheckResult result = new CheckResult();

            if (request.Categories.Count == 0)
            {
                throw new InvalidOperationException("Select at least one MEP category to check.");
            }

            List<LevelOption> levels = _modelInfo.GetLevels(doc);
            if (levels.Count == 0)
            {
                throw new InvalidOperationException("The model contains no levels, so storeys cannot be determined.");
            }

            CheckContext ctx = new CheckContext(settings, request.LevelNames, levels);
            ctx.IgnoreTokens = ParseTokens(settings.IgnoreKeywords);

            List<ModelSource> ceilingSources = new List<ModelSource>();
            List<ModelSource> mepSources = new List<ModelSource>();
            BuildSources(doc, request, ceilingSources, mepSources);

            if (mepSources.Count == 0)
            {
                throw new InvalidOperationException("No MEP source is selected. Enable the host model or at least one loaded link as an MEP source.");
            }

            List<ElementId> selection = null;
            if (settings.Scope == ScopeMode.CurrentSelection)
            {
                selection = _elements.GetSelectedElementIds(uiDoc, request.Categories);
                if (selection.Count == 0)
                {
                    throw new InvalidOperationException("The scope is 'Current selection' but no element of the selected categories is selected in the host model.");
                }
            }

            if (settings.Mode == ReferenceMode.CeilingElements)
            {
                request.ReportProgress(0.0, "Reading ceilings...");
                List<CeilingFace> faces = new List<CeilingFace>();
                foreach (ModelSource source in ceilingSources)
                {
                    request.ThrowIfCancelled();
                    List<CeilingFace> sourceFaces = _ceilings.Collect(source.Document, source.Transform, source.Name, null, result.Log);
                    foreach (CeilingFace sourceFace in sourceFaces)
                    {
                        sourceFace.IsLinked = source.IsLink;
                        sourceFace.LinkInstanceId = source.InstanceId;
                    }

                    result.Log.Add(source.Name + ": " + sourceFaces.Count + " ceiling underside face(s) read.");
                    faces.AddRange(sourceFaces);
                }

                if (faces.Count == 0)
                {
                    throw new InvalidOperationException("No ceilings were found in the selected ceiling sources. Choose the model that contains the ceilings, or switch to the fixed-height reference.");
                }

                ctx.CeilingIndex = new CeilingSpatialIndex(faces);
                ctx.CeilingFaceCount = faces.Count;
            }

            int total = 0;
            foreach (ModelSource source in mepSources)
            {
                using (FilteredElementCollector counter = _elements.CreateCollector(source.Document, request.Categories, settings.Scope, !source.IsLink, selection))
                {
                    total += counter.GetElementCount();
                }
            }

            int processed = 0;
            foreach (ModelSource source in mepSources)
            {
                using (FilteredElementCollector collector = _elements.CreateCollector(source.Document, request.Categories, settings.Scope, !source.IsLink, selection))
                {
                    foreach (Element element in collector)
                    {
                        try
                        {
                            ProcessElement(ctx, source, element);
                        }
                        finally
                        {
                            element.Dispose();
                        }

                        processed++;
                        if (processed % PumpInterval == 0)
                        {
                            request.ReportProgress(total == 0 ? 1.0 : (double)processed / total, "Checking elements " + processed + " / " + total);
                            UiPump.Pump();
                            request.ThrowIfCancelled();
                        }
                    }
                }
            }

            request.ReportProgress(1.0, "Preparing results...");
            FinishResult(doc, ctx, result, stopwatch);
            return result;
        }

        private static void BuildSources(Document doc, CheckRequest request, List<ModelSource> ceilingSources, List<ModelSource> mepSources)
        {
            CheckSettings settings = request.Settings;
            ModelSource host = new ModelSource(doc, Transform.Identity, false, doc.Title, -1);
            bool entireModel = settings.Scope == ScopeMode.EntireModel;

            if (settings.UseHostCeilings)
            {
                ceilingSources.Add(host);
            }

            if (settings.UseHostMep || !entireModel)
            {
                mepSources.Add(host);
            }

            foreach (LinkOption link in request.Links)
            {
                if (!link.IsLoaded || (!link.UseForCeilings && !link.UseForMep))
                {
                    continue;
                }

                RevitLinkInstance instance = doc.GetElement(new ElementId(link.InstanceId)) as RevitLinkInstance;
                if (instance == null)
                {
                    continue;
                }

                Document linkDoc = instance.GetLinkDocument();
                if (linkDoc == null)
                {
                    continue;
                }

                ModelSource source = new ModelSource(linkDoc, instance.GetTotalTransform(), true, link.Name, link.InstanceId);
                if (link.UseForCeilings && settings.Mode == ReferenceMode.CeilingElements)
                {
                    ceilingSources.Add(source);
                }

                if (link.UseForMep && entireModel)
                {
                    mepSources.Add(source);
                }
            }
        }

        private static List<string> ParseTokens(string keywords)
        {
            List<string> tokens = new List<string>();
            if (string.IsNullOrWhiteSpace(keywords))
            {
                return tokens;
            }

            string[] parts = keywords.Split(new[] { ';', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string trimmed = part.Trim().ToLowerInvariant();
                if (trimmed.Length > 0)
                {
                    tokens.Add(trimmed);
                }
            }

            return tokens;
        }

        private void ProcessElement(CheckContext ctx, ModelSource source, Element element)
        {
            if (element.Category == null)
            {
                return;
            }

            BoundsBox bounds;
            if (!TryGetBounds(element, source.Transform, out bounds))
            {
                ctx.ElementsWithoutGeometry++;
                return;
            }

            LevelOption storey = ctx.FindLevel(bounds.MinZ);
            if (ctx.LevelFilter != null && !ctx.LevelFilter.Contains(storey.Name))
            {
                return;
            }

            double floor = storey.Elevation;
            if (bounds.MaxZ <= floor + ctx.ToleranceFt && bounds.MinZ < floor)
            {
                return;
            }

            CheckResultItem item = new CheckResultItem();
            FillNames(element, item);

            if (IsIgnored(ctx, item))
            {
                ctx.ElementsIgnored++;
                return;
            }

            ctx.ElementsChecked++;

            ElementSampler.Build(element, source.Transform, bounds, ctx.Samples);
            ElementEvaluation evaluation = new ElementEvaluation();
            Evaluate(ctx, bounds, evaluation);
            bool below = evaluation.BelowCount > 0;

            double aboveFloor = bounds.MinZ - floor;
            bool headroom = ctx.Settings.CheckHeadroom
                && aboveFloor > FloorStandingFt
                && aboveFloor < ctx.HeadroomFt;

            if (!below && !headroom)
            {
                return;
            }

            item.ElementIdValue = (int)ElementIdCompat.GetValue(element.Id);
            item.IsLinked = source.IsLink;
            item.SourceName = source.Name;
            item.LinkInstanceIdValue = source.InstanceId;
            item.LevelName = storey.Name;
            item.ReferenceLevelElevationFt = floor;
            item.BottomElevationFt = bounds.MinZ;
            item.TopElevationFt = bounds.MaxZ;
            item.Min = new XYZ(bounds.MinX, bounds.MinY, bounds.MinZ);
            item.Max = new XYZ(bounds.MaxX, bounds.MaxY, bounds.MaxZ);
            item.FocusMin = item.Min;
            item.FocusMax = item.Max;
            item.HeadroomViolation = headroom;

            if (below)
            {
                List<CeilingHit> hits = evaluation.GetHits();
                CeilingHit primary = hits[0];
                item.CeilingHits.AddRange(hits);
                item.CeilingName = primary.Name;
                item.CeilingIdValue = primary.IdValue;
                item.CeilingLevelName = primary.LevelName;
                item.CeilingIsLinked = primary.IsLinked;
                item.CeilingSourceName = primary.SourceName;
                item.CeilingElevationFt = evaluation.WorstCeilingZ;
                item.DepthFt = evaluation.WorstDepth;
                item.BelowFraction = (double)evaluation.BelowCount / ctx.Samples.Count;
                item.Severity = evaluation.WorstTop <= evaluation.WorstCeilingZ + ctx.ToleranceFt ? Severity.Critical : Severity.Warning;
                item.FocusMin = new XYZ(evaluation.MinX, evaluation.MinY, evaluation.MinZ);
                item.FocusMax = new XYZ(evaluation.MaxX, evaluation.MaxY, evaluation.MaxZ);

                if (primary.IdValue <= 0)
                {
                    item.CeilingName = "Fixed height " + UnitConverter.Format(ctx.FixedHeightFt, ctx.Settings.DisplayUnit);
                    item.CeilingLevelName = storey.Name;
                }
            }
            else
            {
                item.CeilingElevationFt = floor + ctx.HeadroomFt;
                item.DepthFt = item.CeilingElevationFt - bounds.MinZ;
                item.Severity = Severity.Warning;
                item.CeilingName = "Headroom limit";
                item.CeilingIdValue = -1;
                item.CeilingLevelName = storey.Name;
            }

            ctx.Items.Add(item);
        }

        /// <summary>
        /// Tests every sample position of the element. A position counts as below when its lowest point is
        /// lower than the underside of the lowest ceiling of its storey at that plan position, so a run that
        /// is above the ceiling elsewhere only reports the ceilings it is really below.
        /// </summary>
        private static void Evaluate(CheckContext ctx, BoundsBox bounds, ElementEvaluation evaluation)
        {
            bool ceilingMode = ctx.Settings.Mode == ReferenceMode.CeilingElements;
            if (ceilingMode)
            {
                ctx.Candidates.Clear();
                ctx.CeilingIndex.Query(bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY, ctx.Candidates);
                if (ctx.Candidates.Count == 0)
                {
                    return;
                }
            }

            for (int i = 0; i < ctx.Samples.Count; i++)
            {
                SamplePoint sample = ctx.Samples[i];
                LevelOption storey = ctx.FindLevel(sample.Bottom);
                double floor = storey.Elevation;
                if (sample.Top <= floor + ctx.ToleranceFt && sample.Bottom < floor)
                {
                    continue;
                }

                CeilingFace face = null;
                double ceilingZ;
                if (ceilingMode)
                {
                    face = LowestCeiling(ctx, sample.X, sample.Y, floor, ctx.NextLevelElevation(sample.Bottom), out ceilingZ);
                    if (face == null)
                    {
                        continue;
                    }
                }
                else
                {
                    ceilingZ = floor + ctx.FixedHeightFt;
                }

                double depth = ceilingZ - sample.Bottom;
                if (depth > ctx.ToleranceFt)
                {
                    evaluation.AddBelow(face, "Fixed height", storey.Name, ceilingZ, depth, sample);
                }
            }
        }

        private static CeilingFace LowestCeiling(CheckContext ctx, double x, double y, double floor, double limit, out double ceilingZ)
        {
            CeilingFace lowest = null;
            ceilingZ = double.MaxValue;

            for (int k = 0; k < ctx.Candidates.Count; k++)
            {
                CeilingFace face = ctx.Candidates[k];
                if (!face.ContainsXY(x, y))
                {
                    continue;
                }

                double z = face.ElevationAt(x, y);
                if (z <= floor + 0.05 || z >= limit - 0.1)
                {
                    continue;
                }

                if (z < ceilingZ)
                {
                    ceilingZ = z;
                    lowest = face;
                }
            }

            return lowest;
        }

        private static bool TryGetBounds(Element element, Transform transform, out BoundsBox bounds)
        {
            bounds = new BoundsBox();

            using (BoundingBoxXYZ box = element.get_BoundingBox(null))
            {
                if (box == null)
                {
                    return false;
                }

                Transform combined = transform.Multiply(box.Transform);
                bounds.MinX = double.MaxValue;
                bounds.MinY = double.MaxValue;
                bounds.MinZ = double.MaxValue;
                bounds.MaxX = double.MinValue;
                bounds.MaxY = double.MinValue;
                bounds.MaxZ = double.MinValue;

                for (int i = 0; i < 8; i++)
                {
                    double x = (i & 1) == 0 ? box.Min.X : box.Max.X;
                    double y = (i & 2) == 0 ? box.Min.Y : box.Max.Y;
                    double z = (i & 4) == 0 ? box.Min.Z : box.Max.Z;
                    XYZ point = combined.OfPoint(new XYZ(x, y, z));
                    if (point.X < bounds.MinX) bounds.MinX = point.X;
                    if (point.Y < bounds.MinY) bounds.MinY = point.Y;
                    if (point.Z < bounds.MinZ) bounds.MinZ = point.Z;
                    if (point.X > bounds.MaxX) bounds.MaxX = point.X;
                    if (point.Y > bounds.MaxY) bounds.MaxY = point.Y;
                    if (point.Z > bounds.MaxZ) bounds.MaxZ = point.Z;
                }

                return true;
            }
        }

        private static void FillNames(Element element, CheckResultItem item)
        {
            item.Category = element.Category.Name;
            item.Family = string.Empty;
            item.TypeName = element.Name;

            ElementId typeId = element.GetTypeId();
            if (typeId != null && ElementIdCompat.GetValue(typeId) != ElementIdCompat.GetValue(ElementId.InvalidElementId))
            {
                ElementType type = element.Document.GetElement(typeId) as ElementType;
                try
                {
                    if (type != null)
                    {
                        item.Family = type.FamilyName ?? string.Empty;
                        item.TypeName = type.Name;
                    }
                }
                finally
                {
                    if (type != null)
                    {
                        type.Dispose();
                    }
                }
            }

            string system = ReadString(element, BuiltInParameter.RBS_SYSTEM_NAME_PARAM);
            if (system.Length == 0)
            {
                system = ReadString(element, BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);
            }

            item.SystemName = system.Length == 0 ? "(Unassigned)" : system;
        }

        private static string ReadString(Element element, BuiltInParameter parameterId)
        {
            using (Parameter parameter = element.get_Parameter(parameterId))
            {
                if (parameter == null || !parameter.HasValue)
                {
                    return string.Empty;
                }

                string value = parameter.AsString();
                if (string.IsNullOrEmpty(value))
                {
                    value = parameter.AsValueString();
                }

                return value ?? string.Empty;
            }
        }

        private static bool IsIgnored(CheckContext ctx, CheckResultItem item)
        {
            if (ctx.IgnoreTokens.Count == 0)
            {
                return false;
            }

            string text = (item.Category + "|" + item.Family + "|" + item.TypeName + "|" + item.SystemName).ToLowerInvariant();
            foreach (string token in ctx.IgnoreTokens)
            {
                if (text.IndexOf(token, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void FinishResult(Document doc, CheckContext ctx, CheckResult result, Stopwatch stopwatch)
        {
            ctx.Items.Sort(delegate (CheckResultItem a, CheckResultItem b)
            {
                int bySeverity = a.Severity.CompareTo(b.Severity);
                return bySeverity != 0 ? bySeverity : b.DepthFt.CompareTo(a.DepthFt);
            });

            Dictionary<string, int> byCategory = new Dictionary<string, int>();
            Dictionary<string, int> byLevel = new Dictionary<string, int>();
            Dictionary<string, int> bySystem = new Dictionary<string, int>();
            int critical = 0;
            int warning = 0;
            int headroom = 0;

            for (int i = 0; i < ctx.Items.Count; i++)
            {
                CheckResultItem item = ctx.Items[i];
                item.Number = i + 1;
                item.ApplyUnit(ctx.Settings.DisplayUnit);
                Increment(byCategory, item.Category);
                Increment(byLevel, item.LevelName);
                Increment(bySystem, item.SystemName);
                if (item.Severity == Severity.Critical) critical++; else warning++;
                if (item.HeadroomViolation) headroom++;
                result.Items.Add(item);
            }

            CheckSummary summary = result.Summary;
            summary.ModelTitle = doc.Title;
            summary.RunDate = DateTime.Now;
            summary.Duration = stopwatch.Elapsed;
            summary.ElementsChecked = ctx.ElementsChecked;
            summary.CeilingFaceCount = ctx.CeilingFaceCount;
            summary.ElementsWithoutGeometry = ctx.ElementsWithoutGeometry;
            summary.ElementsIgnored = ctx.ElementsIgnored;
            summary.CriticalCount = critical;
            summary.WarningCount = warning;
            summary.HeadroomCount = headroom;
            summary.Settings = ctx.Settings.Clone();
            summary.ByCategory.AddRange(ToLines(byCategory));
            summary.ByLevel.AddRange(ToLines(byLevel));
            summary.BySystem.AddRange(ToLines(bySystem));

            if (ctx.ElementsWithoutGeometry > 0)
            {
                result.Log.Add(ctx.ElementsWithoutGeometry + " element(s) had no bounding box and were not checked.");
            }

            if (ctx.ElementsIgnored > 0)
            {
                result.Log.Add(ctx.ElementsIgnored + " element(s) were excluded by the ignore keywords.");
            }
        }

        private static void Increment(Dictionary<string, int> map, string key)
        {
            string safe = string.IsNullOrEmpty(key) ? "(None)" : key;
            int count;
            map.TryGetValue(safe, out count);
            map[safe] = count + 1;
        }

        private static List<SummaryLine> ToLines(Dictionary<string, int> map)
        {
            List<SummaryLine> lines = new List<SummaryLine>();
            foreach (KeyValuePair<string, int> pair in map)
            {
                lines.Add(new SummaryLine(pair.Key, pair.Value));
            }

            lines.Sort(delegate (SummaryLine a, SummaryLine b)
            {
                int byCount = b.Count.CompareTo(a.Count);
                return byCount != 0 ? byCount : string.CompareOrdinal(a.Label, b.Label);
            });
            return lines;
        }
    }
}

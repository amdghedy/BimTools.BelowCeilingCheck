using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Geometry;
using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Extracts the underside faces of all ceilings of a document (host or link).</summary>
    public sealed class CeilingCollectorService
    {
        private const double MinFaceAreaSqFt = 0.05;

        public List<CeilingFace> Collect(Document doc, Transform transform, string sourceName, HashSet<string> levelNames, List<string> log)
        {
            List<CeilingFace> faces = new List<CeilingFace>();
            int withoutGeometry = 0;

            using (Options options = new Options())
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                options.ComputeReferences = false;
                options.DetailLevel = ViewDetailLevel.Coarse;
                options.IncludeNonVisibleObjects = false;

                collector.OfCategory(BuiltInCategory.OST_Ceilings).WhereElementIsNotElementType();

                foreach (Element ceiling in collector)
                {
                    try
                    {
                        string levelName = "(No level)";
                        double levelElevation = 0.0;
                        ElementId levelId = ceiling.LevelId;
                        if (levelId != null && ElementIdCompat.GetValue(levelId) != ElementIdCompat.GetValue(ElementId.InvalidElementId))
                        {
                            Level level = doc.GetElement(levelId) as Level;
                            if (level != null)
                            {
                                levelName = level.Name;
                                levelElevation = transform.OfPoint(new XYZ(0.0, 0.0, level.Elevation)).Z;
                            }
                        }

                        if (levelNames != null && !levelNames.Contains(levelName))
                        {
                            continue;
                        }

                        string ceilingName = GetTypeName(doc, ceiling);
                        int before = faces.Count;

                        using (GeometryElement geometry = ceiling.get_Geometry(options))
                        {
                            if (geometry != null)
                            {
                                CollectFaces(geometry, transform, ElementIdCompat.GetValue(ceiling.Id), ceilingName, levelName, levelElevation, sourceName, faces);
                            }
                        }

                        if (faces.Count == before && !AddBoundingBoxFace(ceiling, transform, ceilingName, levelName, levelElevation, sourceName, faces))
                        {
                            withoutGeometry++;
                        }
                    }
                    finally
                    {
                        ceiling.Dispose();
                    }
                }
            }

            if (withoutGeometry > 0)
            {
                log.Add(sourceName + ": " + withoutGeometry + " ceiling(s) had no usable geometry and were skipped.");
            }

            return faces;
        }

        private static string GetTypeName(Document doc, Element ceiling)
        {
            ElementId typeId = ceiling.GetTypeId();
            if (typeId == null || ElementIdCompat.GetValue(typeId) == ElementIdCompat.GetValue(ElementId.InvalidElementId))
            {
                return ceiling.Name;
            }

            Element type = doc.GetElement(typeId);
            try
            {
                return type != null ? type.Name : ceiling.Name;
            }
            finally
            {
                if (type != null)
                {
                    type.Dispose();
                }
            }
        }

        private static void CollectFaces(
            GeometryElement geometry,
            Transform transform,
            int ceilingId,
            string ceilingName,
            string levelName,
            double levelElevation,
            string sourceName,
            List<CeilingFace> faces)
        {
            foreach (GeometryObject geometryObject in geometry)
            {
                Solid solid = geometryObject as Solid;
                if (solid != null)
                {
                    if (solid.Faces.IsEmpty || solid.Volume <= 1e-9)
                    {
                        continue;
                    }

                    foreach (Face face in solid.Faces)
                    {
                        CeilingFace ceilingFace = BuildFace(face, transform);
                        if (ceilingFace != null)
                        {
                            ceilingFace.CeilingIdValue = ceilingId;
                            ceilingFace.CeilingName = ceilingName;
                            ceilingFace.LevelName = levelName;
                            ceilingFace.LevelElevationFt = levelElevation;
                            ceilingFace.SourceName = sourceName;
                            faces.Add(ceilingFace);
                        }
                    }

                    continue;
                }

                GeometryInstance instance = geometryObject as GeometryInstance;
                if (instance != null)
                {
                    using (GeometryElement instanceGeometry = instance.GetInstanceGeometry())
                    {
                        if (instanceGeometry != null)
                        {
                            CollectFaces(instanceGeometry, transform, ceilingId, ceilingName, levelName, levelElevation, sourceName, faces);
                        }
                    }
                }
            }
        }

        private static CeilingFace BuildFace(Face face, Transform transform)
        {
            PlanarFace planar = face as PlanarFace;
            if (planar == null)
            {
                return null;
            }

            XYZ normal = planar.FaceNormal;
            if (normal.Z > -0.5)
            {
                return null;
            }

            IList<CurveLoop> loops = planar.GetEdgesAsCurveLoops();
            if (loops == null || loops.Count == 0)
            {
                return null;
            }

            List<Polygon2D> polygons = new List<Polygon2D>();
            foreach (CurveLoop loop in loops)
            {
                Polygon2D polygon = BuildPolygon(loop, transform);
                if (polygon != null)
                {
                    polygons.Add(polygon);
                }
            }

            if (polygons.Count == 0)
            {
                return null;
            }

            int outerIndex = 0;
            for (int i = 1; i < polygons.Count; i++)
            {
                if (System.Math.Abs(polygons[i].SignedArea) > System.Math.Abs(polygons[outerIndex].SignedArea))
                {
                    outerIndex = i;
                }
            }

            if (System.Math.Abs(polygons[outerIndex].SignedArea) < MinFaceAreaSqFt)
            {
                return null;
            }

            CeilingFace result = new CeilingFace(polygons[outerIndex]);
            for (int i = 0; i < polygons.Count; i++)
            {
                if (i != outerIndex)
                {
                    result.Holes.Add(polygons[i]);
                }
            }

            XYZ origin = transform.OfPoint(planar.Origin);
            XYZ worldNormal = transform.OfVector(normal);
            result.OriginX = origin.X;
            result.OriginY = origin.Y;
            result.OriginZ = origin.Z;
            result.NormalX = worldNormal.X;
            result.NormalY = worldNormal.Y;
            result.NormalZ = worldNormal.Z;
            result.ComputeBounds();
            return result;
        }

        private static Polygon2D BuildPolygon(CurveLoop loop, Transform transform)
        {
            List<double> xs = new List<double>();
            List<double> ys = new List<double>();

            foreach (Curve curve in loop)
            {
                IList<XYZ> points = curve.Tessellate();
                for (int i = 0; i < points.Count - 1; i++)
                {
                    XYZ point = transform.OfPoint(points[i]);
                    xs.Add(point.X);
                    ys.Add(point.Y);
                }
            }

            if (xs.Count < 3)
            {
                return null;
            }

            return new Polygon2D(xs.ToArray(), ys.ToArray());
        }

        private static bool AddBoundingBoxFace(
            Element ceiling,
            Transform transform,
            string ceilingName,
            string levelName,
            double levelElevation,
            string sourceName,
            List<CeilingFace> faces)
        {
            using (BoundingBoxXYZ box = ceiling.get_BoundingBox(null))
            {
                if (box == null)
                {
                    return false;
                }

                Transform combined = transform.Multiply(box.Transform);
                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double minZ = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                for (int i = 0; i < 8; i++)
                {
                    double x = (i & 1) == 0 ? box.Min.X : box.Max.X;
                    double y = (i & 2) == 0 ? box.Min.Y : box.Max.Y;
                    double z = (i & 4) == 0 ? box.Min.Z : box.Max.Z;
                    XYZ point = combined.OfPoint(new XYZ(x, y, z));
                    if (point.X < minX) minX = point.X;
                    if (point.Y < minY) minY = point.Y;
                    if (point.Z < minZ) minZ = point.Z;
                    if (point.X > maxX) maxX = point.X;
                    if (point.Y > maxY) maxY = point.Y;
                }

                if (maxX - minX < 1e-6 || maxY - minY < 1e-6)
                {
                    return false;
                }

                Polygon2D polygon = new Polygon2D(
                    new double[] { minX, maxX, maxX, minX },
                    new double[] { minY, minY, maxY, maxY });

                CeilingFace face = new CeilingFace(polygon);
                face.OriginX = minX;
                face.OriginY = minY;
                face.OriginZ = minZ;
                face.NormalX = 0.0;
                face.NormalY = 0.0;
                face.NormalZ = -1.0;
                face.CeilingIdValue = ElementIdCompat.GetValue(ceiling.Id);
                face.CeilingName = ceilingName;
                face.LevelName = levelName;
                face.LevelElevationFt = levelElevation;
                face.SourceName = sourceName;
                face.ComputeBounds();
                faces.Add(face);
                return true;
            }
        }
    }
}

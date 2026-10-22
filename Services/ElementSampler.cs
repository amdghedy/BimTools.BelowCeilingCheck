using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Geometry;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>
    /// Turns an element into positions that can each be compared with the ceiling above them.
    /// Ducts, pipes, conduits and cable trays are sampled along their centre line, so a run that is
    /// below the ceiling in one area and above it in another is evaluated position by position.
    /// Other elements are sampled on a plan grid at their lowest point.
    /// </summary>
    public static class ElementSampler
    {
        private const double CurveStepFt = 1.5;
        private const int MaxCurveSamples = 200;
        private const double GridStepFt = 3.0;
        private const int MaxGridSamplesPerAxis = 15;

        public static void Build(Element element, Transform transform, BoundsBox bounds, List<SamplePoint> samples)
        {
            samples.Clear();
            if (!TryBuildAlongCurve(element, transform, bounds, samples))
            {
                BuildGrid(bounds, samples);
            }
        }

        private static bool TryBuildAlongCurve(Element element, Transform transform, BoundsBox bounds, List<SamplePoint> samples)
        {
            MEPCurve mepCurve = element as MEPCurve;
            if (mepCurve == null)
            {
                return false;
            }

            LocationCurve location = mepCurve.Location as LocationCurve;
            if (location == null)
            {
                return false;
            }

            using (Curve curve = location.Curve)
            {
                if (curve == null || !curve.IsBound)
                {
                    return false;
                }

                int count = Math.Max(2, Math.Min(MaxCurveSamples, (int)Math.Ceiling(curve.ApproximateLength / CurveStepFt) + 1));
                XYZ[] points = new XYZ[count];
                double zMin = double.MaxValue;
                double zMax = double.MinValue;

                for (int i = 0; i < count; i++)
                {
                    XYZ point = transform.OfPoint(curve.Evaluate((double)i / (count - 1), true));
                    points[i] = point;
                    if (point.Z < zMin) zMin = point.Z;
                    if (point.Z > zMax) zMax = point.Z;
                }

                // Vertical half-size of the cross-section = what the bounding box adds to the centre-line height range.
                double halfHeight = Math.Max(0.0, ((bounds.MaxZ - bounds.MinZ) - (zMax - zMin)) / 2.0);
                for (int i = 0; i < count; i++)
                {
                    samples.Add(new SamplePoint(points[i].X, points[i].Y, points[i].Z - halfHeight, points[i].Z + halfHeight));
                }

                return true;
            }
        }

        private static void BuildGrid(BoundsBox bounds, List<SamplePoint> samples)
        {
            int nx = AxisCount(bounds.MaxX - bounds.MinX);
            int ny = AxisCount(bounds.MaxY - bounds.MinY);

            for (int i = 0; i < nx; i++)
            {
                double x = bounds.MinX + ((bounds.MaxX - bounds.MinX) * i / (nx - 1));
                for (int j = 0; j < ny; j++)
                {
                    double y = bounds.MinY + ((bounds.MaxY - bounds.MinY) * j / (ny - 1));
                    samples.Add(new SamplePoint(x, y, bounds.MinZ, bounds.MaxZ));
                }
            }
        }

        private static int AxisCount(double span)
        {
            int count = (int)Math.Ceiling(span / GridStepFt) + 1;
            return Math.Max(2, Math.Min(MaxGridSamplesPerAxis, count));
        }
    }
}

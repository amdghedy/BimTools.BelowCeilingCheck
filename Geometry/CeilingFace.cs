using System;
using System.Collections.Generic;

namespace BimTools.BelowCeilingCheck.Geometry
{
    /// <summary>
    /// A downward-facing planar face of a ceiling (its underside), expressed in host coordinates.
    /// Supports sloped ceilings through the stored plane.
    /// </summary>
    public sealed class CeilingFace
    {
        public CeilingFace(Polygon2D outer)
        {
            if (outer == null)
            {
                throw new ArgumentNullException("outer");
            }

            Outer = outer;
            Holes = new List<Polygon2D>();
        }

        public Polygon2D Outer { get; private set; }

        public List<Polygon2D> Holes { get; private set; }

        public double NormalX { get; set; }

        public double NormalY { get; set; }

        public double NormalZ { get; set; }

        public double OriginX { get; set; }

        public double OriginY { get; set; }

        public double OriginZ { get; set; }

        public double MinX { get; private set; }

        public double MinY { get; private set; }

        public double MaxX { get; private set; }

        public double MaxY { get; private set; }

        public double MinZ { get; private set; }

        public double MaxZ { get; private set; }

        public int CeilingIdValue { get; set; }

        public string CeilingName { get; set; }

        public string LevelName { get; set; }

        public double LevelElevationFt { get; set; }

        public string SourceName { get; set; }

        /// <summary>True when the ceiling belongs to a linked model.</summary>
        public bool IsLinked { get; set; }

        public int LinkInstanceId { get; set; }

        /// <summary>Used by the spatial index to avoid reporting a face twice per query.</summary>
        public int QueryStamp { get; set; }

        public void ComputeBounds()
        {
            MinX = Outer.MinX;
            MinY = Outer.MinY;
            MaxX = Outer.MaxX;
            MaxY = Outer.MaxY;
            MinZ = double.MaxValue;
            MaxZ = double.MinValue;

            for (int i = 0; i < Outer.Count; i++)
            {
                double z = ElevationAt(Outer.GetX(i), Outer.GetY(i));
                if (z < MinZ) MinZ = z;
                if (z > MaxZ) MaxZ = z;
            }
        }

        public bool ContainsXY(double x, double y)
        {
            if (x < MinX || x > MaxX || y < MinY || y > MaxY)
            {
                return false;
            }

            if (!Outer.Contains(x, y))
            {
                return false;
            }

            for (int i = 0; i < Holes.Count; i++)
            {
                if (Holes[i].Contains(x, y))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Ceiling underside elevation at the given plan position.</summary>
        public double ElevationAt(double x, double y)
        {
            if (Math.Abs(NormalZ) < 1e-9)
            {
                return OriginZ;
            }

            return OriginZ - ((NormalX * (x - OriginX)) + (NormalY * (y - OriginY))) / NormalZ;
        }
    }
}

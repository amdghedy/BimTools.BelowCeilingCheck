using System;
using System.Collections.Generic;

namespace BimTools.BelowCeilingCheck.Geometry
{
    /// <summary>Uniform grid index over ceiling faces so each element only tests nearby ceilings.</summary>
    public sealed class CeilingSpatialIndex
    {
        private const double CellSizeFt = 25.0;
        private const long MaxCellsPerFace = 2500;

        private readonly Dictionary<long, List<CeilingFace>> _cells = new Dictionary<long, List<CeilingFace>>();
        private readonly List<CeilingFace> _oversized = new List<CeilingFace>();
        private int _stamp;

        public CeilingSpatialIndex(IList<CeilingFace> faces)
        {
            if (faces == null)
            {
                throw new ArgumentNullException("faces");
            }

            GlobalMaxZ = double.MinValue;

            foreach (CeilingFace face in faces)
            {
                if (face.MaxZ > GlobalMaxZ)
                {
                    GlobalMaxZ = face.MaxZ;
                }

                int x0 = Cell(face.MinX);
                int x1 = Cell(face.MaxX);
                int y0 = Cell(face.MinY);
                int y1 = Cell(face.MaxY);
                long count = (long)(x1 - x0 + 1) * (long)(y1 - y0 + 1);

                if (count > MaxCellsPerFace)
                {
                    _oversized.Add(face);
                    continue;
                }

                for (int cx = x0; cx <= x1; cx++)
                {
                    for (int cy = y0; cy <= y1; cy++)
                    {
                        long key = Key(cx, cy);
                        List<CeilingFace> bucket;
                        if (!_cells.TryGetValue(key, out bucket))
                        {
                            bucket = new List<CeilingFace>();
                            _cells.Add(key, bucket);
                        }

                        bucket.Add(face);
                    }
                }
            }
        }

        /// <summary>Highest ceiling elevation of any indexed face.</summary>
        public double GlobalMaxZ { get; private set; }

        public void Query(double minX, double minY, double maxX, double maxY, List<CeilingFace> output)
        {
            _stamp++;
            if (_stamp == int.MaxValue)
            {
                _stamp = 1;
            }

            for (int i = 0; i < _oversized.Count; i++)
            {
                TryAdd(_oversized[i], minX, minY, maxX, maxY, output);
            }

            int x0 = Cell(minX);
            int x1 = Cell(maxX);
            int y0 = Cell(minY);
            int y1 = Cell(maxY);

            for (int cx = x0; cx <= x1; cx++)
            {
                for (int cy = y0; cy <= y1; cy++)
                {
                    List<CeilingFace> bucket;
                    if (!_cells.TryGetValue(Key(cx, cy), out bucket))
                    {
                        continue;
                    }

                    for (int i = 0; i < bucket.Count; i++)
                    {
                        TryAdd(bucket[i], minX, minY, maxX, maxY, output);
                    }
                }
            }
        }

        private void TryAdd(CeilingFace face, double minX, double minY, double maxX, double maxY, List<CeilingFace> output)
        {
            if (face.QueryStamp == _stamp)
            {
                return;
            }

            face.QueryStamp = _stamp;

            if (face.MaxX < minX || face.MinX > maxX || face.MaxY < minY || face.MinY > maxY)
            {
                return;
            }

            output.Add(face);
        }

        private static int Cell(double value)
        {
            return (int)Math.Floor(value / CellSizeFt);
        }

        private static long Key(int cx, int cy)
        {
            return ((long)cx << 32) | (long)(uint)cy;
        }
    }
}

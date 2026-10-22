using System;

namespace BimTools.BelowCeilingCheck.Geometry
{
    /// <summary>Simple closed polygon in plan (X/Y) used for point-in-polygon tests.</summary>
    public sealed class Polygon2D
    {
        private readonly double[] _xs;
        private readonly double[] _ys;

        public Polygon2D(double[] xs, double[] ys)
        {
            if (xs == null || ys == null || xs.Length != ys.Length || xs.Length < 3)
            {
                throw new ArgumentException("A polygon needs at least three matching X/Y vertices.");
            }

            _xs = xs;
            _ys = ys;
            MinX = double.MaxValue;
            MinY = double.MaxValue;
            MaxX = double.MinValue;
            MaxY = double.MinValue;

            double twiceArea = 0.0;
            for (int i = 0, j = xs.Length - 1; i < xs.Length; j = i++)
            {
                if (xs[i] < MinX) MinX = xs[i];
                if (xs[i] > MaxX) MaxX = xs[i];
                if (ys[i] < MinY) MinY = ys[i];
                if (ys[i] > MaxY) MaxY = ys[i];
                twiceArea += (xs[j] * ys[i]) - (xs[i] * ys[j]);
            }

            SignedArea = twiceArea / 2.0;
        }

        public double MinX { get; private set; }

        public double MinY { get; private set; }

        public double MaxX { get; private set; }

        public double MaxY { get; private set; }

        public double SignedArea { get; private set; }

        public int Count
        {
            get { return _xs.Length; }
        }

        public double GetX(int index)
        {
            return _xs[index];
        }

        public double GetY(int index)
        {
            return _ys[index];
        }

        public bool Contains(double x, double y)
        {
            if (x < MinX || x > MaxX || y < MinY || y > MaxY)
            {
                return false;
            }

            bool inside = false;
            for (int i = 0, j = _xs.Length - 1; i < _xs.Length; j = i++)
            {
                bool crosses = (_ys[i] > y) != (_ys[j] > y);
                if (crosses && x < (_xs[j] - _xs[i]) * (y - _ys[i]) / (_ys[j] - _ys[i]) + _xs[i])
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}

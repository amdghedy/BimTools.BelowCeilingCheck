namespace BimTools.BelowCeilingCheck.Geometry
{
    /// <summary>A position along an element with the lowest and highest points of its cross-section there (host coordinates, feet).</summary>
    public struct SamplePoint
    {
        public SamplePoint(double x, double y, double bottom, double top)
        {
            X = x;
            Y = y;
            Bottom = bottom;
            Top = top;
        }

        public readonly double X;
        public readonly double Y;
        public readonly double Bottom;
        public readonly double Top;
    }
}

namespace BimTools.BelowCeilingCheck.Geometry
{
    /// <summary>Axis-aligned bounds in host coordinates (feet).</summary>
    public struct BoundsBox
    {
        public double MinX;
        public double MinY;
        public double MinZ;
        public double MaxX;
        public double MaxY;
        public double MaxZ;

        public double CenterX
        {
            get { return (MinX + MaxX) * 0.5; }
        }

        public double CenterY
        {
            get { return (MinY + MaxY) * 0.5; }
        }

        public double CenterZ
        {
            get { return (MinZ + MaxZ) * 0.5; }
        }
    }
}

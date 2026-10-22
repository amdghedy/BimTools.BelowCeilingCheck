using System.Collections.Generic;
using BimTools.BelowCeilingCheck.Geometry;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Outcome of testing every sample position of one element against the ceilings.</summary>
    public sealed class ElementEvaluation
    {
        private readonly Dictionary<string, CeilingHit> _hits = new Dictionary<string, CeilingHit>();

        public ElementEvaluation()
        {
            MinX = double.MaxValue;
            MinY = double.MaxValue;
            MinZ = double.MaxValue;
            MaxX = double.MinValue;
            MaxY = double.MinValue;
            MaxZ = double.MinValue;
        }

        /// <summary>Number of sample positions that are below their ceiling.</summary>
        public int BelowCount { get; private set; }

        public double WorstDepth { get; private set; }

        public double WorstCeilingZ { get; private set; }

        public double WorstTop { get; private set; }

        /// <summary>Region (host coordinates) that contains all positions below a ceiling.</summary>
        public double MinX { get; private set; }

        public double MinY { get; private set; }

        public double MinZ { get; private set; }

        public double MaxX { get; private set; }

        public double MaxY { get; private set; }

        public double MaxZ { get; private set; }

        /// <summary>Ceilings the element is below, deepest first.</summary>
        public List<CeilingHit> GetHits()
        {
            List<CeilingHit> list = new List<CeilingHit>(_hits.Values);
            list.Sort(delegate (CeilingHit a, CeilingHit b) { return b.DepthFt.CompareTo(a.DepthFt); });
            return list;
        }

        public void AddBelow(CeilingFace face, string fixedName, string levelName, double ceilingZ, double depth, SamplePoint sample)
        {
            BelowCount++;

            if (sample.X < MinX) MinX = sample.X;
            if (sample.Y < MinY) MinY = sample.Y;
            if (sample.Bottom < MinZ) MinZ = sample.Bottom;
            if (sample.X > MaxX) MaxX = sample.X;
            if (sample.Y > MaxY) MaxY = sample.Y;
            if (sample.Top > MaxZ) MaxZ = sample.Top;

            if (depth > WorstDepth)
            {
                WorstDepth = depth;
                WorstCeilingZ = ceilingZ;
                WorstTop = sample.Top;
            }

            string key = face == null ? "fixed" : (face.IsLinked ? "L|" + face.SourceName : "H") + "|" + face.CeilingIdValue;
            CeilingHit hit;
            if (!_hits.TryGetValue(key, out hit))
            {
                hit = new CeilingHit();
                hit.IdValue = face == null ? -1 : face.CeilingIdValue;
                hit.IsLinked = face != null && face.IsLinked;
                hit.LinkInstanceId = face == null ? -1 : face.LinkInstanceId;
                hit.SourceName = face == null ? string.Empty : face.SourceName;
                hit.Name = face == null ? fixedName : face.CeilingName;
                hit.LevelName = levelName;
                _hits.Add(key, hit);
            }

            hit.SampleCount++;
            if (depth > hit.DepthFt)
            {
                hit.DepthFt = depth;
                hit.CeilingZFt = ceilingZ;
            }
        }
    }
}

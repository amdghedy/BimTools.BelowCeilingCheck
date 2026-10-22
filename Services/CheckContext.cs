using System.Collections.Generic;
using BimTools.BelowCeilingCheck.Geometry;
using BimTools.BelowCeilingCheck.Infrastructure;
using BimTools.BelowCeilingCheck.Models;

namespace BimTools.BelowCeilingCheck.Services
{
    /// <summary>Working state shared by the check engine while a run is in progress.</summary>
    public sealed class CheckContext
    {
        public CheckContext(CheckSettings settings, HashSet<string> levelFilter, List<LevelOption> levels)
        {
            Settings = settings;
            LevelFilter = levelFilter;
            Levels = levels;
            ToleranceFt = UnitConverter.MmToFeet(settings.ToleranceMm);
            FixedHeightFt = UnitConverter.MmToFeet(settings.FixedCeilingHeightMm);
            HeadroomFt = UnitConverter.MmToFeet(settings.MinHeadroomMm);
            IgnoreTokens = new List<string>();
            Items = new List<CheckResultItem>();
            Candidates = new List<CeilingFace>();
            Samples = new List<SamplePoint>();
        }

        public CheckSettings Settings { get; private set; }

        public HashSet<string> LevelFilter { get; private set; }

        public List<LevelOption> Levels { get; private set; }

        public double ToleranceFt { get; private set; }

        public double FixedHeightFt { get; private set; }

        public double HeadroomFt { get; private set; }

        public List<string> IgnoreTokens { get; set; }

        public CeilingSpatialIndex CeilingIndex { get; set; }

        public int CeilingFaceCount { get; set; }

        public List<CheckResultItem> Items { get; private set; }

        public int ElementsChecked { get; set; }

        public int ElementsWithoutGeometry { get; set; }

        public int ElementsIgnored { get; set; }

        public List<CeilingFace> Candidates { get; private set; }

        public List<SamplePoint> Samples { get; private set; }

        /// <summary>Level whose elevation is the highest one at or below the given Z.</summary>
        public LevelOption FindLevel(double z)
        {
            LevelOption found = Levels[0];
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i].Elevation <= z + 0.01)
                {
                    found = Levels[i];
                }
                else
                {
                    break;
                }
            }

            return found;
        }

        /// <summary>Elevation of the next level above the storey that contains z, or positive infinity for the top storey.</summary>
        public double NextLevelElevation(double z)
        {
            LevelOption current = FindLevel(z);
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i].Elevation > current.Elevation + 0.01)
                {
                    return Levels[i].Elevation;
                }
            }

            return double.PositiveInfinity;
        }
    }
}

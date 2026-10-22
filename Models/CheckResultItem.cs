using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck.Models
{
    /// <summary>
    /// One reported element. All lengths are stored in Revit internal units (feet);
    /// the *Text properties are re-rendered whenever the display unit changes.
    /// </summary>
    public sealed class CheckResultItem : ObservableObject
    {
        private bool _isReviewed;
        private string _notes = string.Empty;
        private string _depthText = string.Empty;
        private string _bottomAffText = string.Empty;
        private string _ceilingAffText = string.Empty;

        public int Number { get; set; }

        public int ElementIdValue { get; set; }

        public bool IsLinked { get; set; }

        public string SourceName { get; set; }

        public int LinkInstanceIdValue { get; set; }

        public string Category { get; set; }

        public string Family { get; set; }

        public string TypeName { get; set; }

        public string SystemName { get; set; }

        public string LevelName { get; set; }

        public string CeilingLevelName { get; set; }

        public string CeilingName { get; set; }

        public int CeilingIdValue { get; set; }

        public CheckResultItem()
        {
            CeilingHits = new List<CeilingHit>();
        }

        /// <summary>Every ceiling this element is below (deepest first). The primary ceiling is the first one.</summary>
        public List<CeilingHit> CeilingHits { get; private set; }

        /// <summary>Share of the element's sampled length that is below a ceiling (1 = the whole run).</summary>
        public double BelowFraction { get; set; }

        /// <summary>Region of the element that is below a ceiling (host coordinates), used for zooming and section boxes.</summary>
        public XYZ FocusMin { get; set; }

        public XYZ FocusMax { get; set; }

        public string ExtentText
        {
            get
            {
                if (CeilingHits.Count == 0)
                {
                    return string.Empty;
                }

                if (BelowFraction >= 0.95)
                {
                    return "Entire run";
                }

                return "Partial (" + Math.Round(BelowFraction * 100.0).ToString("0") + "%)";
            }
        }

        public string CeilingCountText
        {
            get
            {
                int real = 0;
                foreach (CeilingHit hit in CeilingHits)
                {
                    if (hit.IdValue > 0)
                    {
                        real++;
                    }
                }

                return real == 0 ? string.Empty : real.ToString();
            }
        }

        public bool CeilingIsLinked { get; set; }

        public string CeilingSourceName { get; set; }

        public string CeilingIdText
        {
            get { return CeilingIdValue > 0 ? CeilingIdValue.ToString() : string.Empty; }
        }

        public string CeilingModelText
        {
            get
            {
                if (CeilingIdValue <= 0)
                {
                    return string.Empty;
                }

                return CeilingIsLinked ? CeilingSourceName : "Host";
            }
        }

        public double CeilingElevationFt { get; set; }

        public double BottomElevationFt { get; set; }

        public double TopElevationFt { get; set; }

        public double ReferenceLevelElevationFt { get; set; }

        /// <summary>How far the element bottom is below the ceiling underside.</summary>
        public double DepthFt { get; set; }

        public Severity Severity { get; set; }

        public bool HeadroomViolation { get; set; }

        public XYZ Min { get; set; }

        public XYZ Max { get; set; }

        public double BottomAffFt
        {
            get { return BottomElevationFt - ReferenceLevelElevationFt; }
        }

        public double CeilingAffFt
        {
            get { return CeilingElevationFt - ReferenceLevelElevationFt; }
        }

        public string SeverityText
        {
            get { return Severity == Severity.Critical ? "Critical" : "Warning"; }
        }

        public string SourceText
        {
            get { return IsLinked ? SourceName : "Host"; }
        }

        public string HeadroomText
        {
            get { return HeadroomViolation ? "Yes" : string.Empty; }
        }

        public bool IsReviewed
        {
            get { return _isReviewed; }
            set { SetProperty(ref _isReviewed, value); }
        }

        public string Notes
        {
            get { return _notes; }
            set { SetProperty(ref _notes, value ?? string.Empty); }
        }

        public string DepthText
        {
            get { return _depthText; }
            private set { SetProperty(ref _depthText, value); }
        }

        public string BottomAffText
        {
            get { return _bottomAffText; }
            private set { SetProperty(ref _bottomAffText, value); }
        }

        public string CeilingAffText
        {
            get { return _ceilingAffText; }
            private set { SetProperty(ref _ceilingAffText, value); }
        }

        public void ApplyUnit(LengthUnit unit)
        {
            DepthText = UnitConverter.Format(DepthFt, unit);
            BottomAffText = UnitConverter.Format(BottomAffFt, unit);
            CeilingAffText = UnitConverter.Format(CeilingAffFt, unit);
        }
    }
}

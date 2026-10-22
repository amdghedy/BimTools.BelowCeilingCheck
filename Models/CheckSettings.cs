using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BimTools.BelowCeilingCheck.Models
{
    /// <summary>
    /// User options. Persisted as JSON in %AppData%\BIM TOOLS\BelowCeilingCheck\settings.json.
    /// </summary>
    [DataContract]
    public sealed class CheckSettings
    {
        public CheckSettings()
        {
            SetDefaults();
        }

        [DataMember]
        public LengthUnit DisplayUnit { get; set; }

        [DataMember]
        public ReferenceMode Mode { get; set; }

        [DataMember]
        public ScopeMode Scope { get; set; }

        /// <summary>Elements must be lower than the ceiling by more than this value to be reported.</summary>
        [DataMember]
        public double ToleranceMm { get; set; }

        [DataMember]
        public double FixedCeilingHeightMm { get; set; }

        [DataMember]
        public bool UseHostCeilings { get; set; }

        [DataMember]
        public bool UseHostMep { get; set; }

        [DataMember]
        public bool CheckHeadroom { get; set; }

        [DataMember]
        public double MinHeadroomMm { get; set; }

        [DataMember]
        public double SectionBoxPaddingMm { get; set; }

        [DataMember]
        public string IgnoreKeywords { get; set; }

        [DataMember]
        public List<string> SelectedCategories { get; set; }

        [DataMember]
        public string CompanyName { get; set; }

        [DataMember]
        public string PreparedBy { get; set; }

        [DataMember]
        public bool AutoSelectOnRowChange { get; set; }

        /// <summary>Show linked elements and ceilings as coloured temporary marker solids when isolating.</summary>
        [DataMember]
        public bool MarkLinkedElements { get; set; }

        public CheckSettings Clone()
        {
            CheckSettings copy = (CheckSettings)MemberwiseClone();
            copy.SelectedCategories = new List<string>(SelectedCategories ?? new List<string>());
            return copy;
        }

        public void Normalize()
        {
            if (ToleranceMm < 0) ToleranceMm = 0;
            if (FixedCeilingHeightMm <= 0) FixedCeilingHeightMm = 2700;
            if (MinHeadroomMm < 0) MinHeadroomMm = 0;
            if (SectionBoxPaddingMm < 0) SectionBoxPaddingMm = 0;
            if (IgnoreKeywords == null) IgnoreKeywords = string.Empty;
            if (CompanyName == null) CompanyName = string.Empty;
            if (PreparedBy == null) PreparedBy = string.Empty;
            if (SelectedCategories == null) SelectedCategories = new List<string>();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            SetDefaults();
        }

        private void SetDefaults()
        {
            DisplayUnit = LengthUnit.Millimeters;
            Mode = ReferenceMode.CeilingElements;
            Scope = ScopeMode.EntireModel;
            ToleranceMm = 5;
            FixedCeilingHeightMm = 2700;
            UseHostCeilings = true;
            UseHostMep = true;
            CheckHeadroom = false;
            MinHeadroomMm = 2100;
            SectionBoxPaddingMm = 1500;
            IgnoreKeywords = string.Empty;
            SelectedCategories = new List<string>();
            CompanyName = string.Empty;
            PreparedBy = string.Empty;
            AutoSelectOnRowChange = false;
            MarkLinkedElements = true;
        }
    }
}

using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck.Models
{
    public sealed class LevelOption : ObservableObject
    {
        private bool _isSelected = true;

        public LevelOption(string name, double elevationFeet)
        {
            Name = name;
            Elevation = elevationFeet;
        }

        public string Name { get; private set; }

        /// <summary>Level elevation in Revit internal units (feet).</summary>
        public double Elevation { get; private set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }
    }
}

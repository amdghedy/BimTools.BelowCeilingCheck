using Autodesk.Revit.DB;
using BimTools.BelowCeilingCheck.Infrastructure;

namespace BimTools.BelowCeilingCheck.Models
{
    public sealed class CategoryOption : ObservableObject
    {
        private bool _isSelected;

        public CategoryOption(string name, BuiltInCategory category, string discipline, bool defaultSelected)
        {
            Name = name;
            Category = category;
            Discipline = discipline;
            DefaultSelected = defaultSelected;
            _isSelected = defaultSelected;
        }

        public string Name { get; private set; }

        public BuiltInCategory Category { get; private set; }

        public string Discipline { get; private set; }

        public bool DefaultSelected { get; private set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }
    }
}

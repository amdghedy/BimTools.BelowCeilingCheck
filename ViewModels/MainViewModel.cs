using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.BelowCeilingCheck.Infrastructure;
using BimTools.BelowCeilingCheck.Models;
using BimTools.BelowCeilingCheck.Services;

namespace BimTools.BelowCeilingCheck.ViewModels
{
    /// <summary>View model of the Below Ceiling Check window.</summary>
    public sealed class MainViewModel : ObservableObject
    {
        private const string Title = "BIM TOOLS - Below Ceiling Check";
        private const string AllLabel = "(All)";

        private readonly RevitActionDispatcher _dispatcher;
        private readonly IDialogService _dialogs;
        private readonly SettingsService _settingsService;
        private readonly CheckEngineService _engine = new CheckEngineService();
        private readonly ModelInfoService _modelInfo = new ModelInfoService();
        private readonly ViewActionService _viewActions = new ViewActionService();
        private readonly CsvExportService _csvExport = new CsvExportService();
        private readonly HtmlReportService _htmlReport = new HtmlReportService();
        private readonly List<CheckResultItem> _selected = new List<CheckResultItem>();

        private LengthUnit _displayUnit;
        private ReferenceMode _mode;
        private ScopeMode _scope;
        private double _tolerance;
        private double _fixedHeight;
        private double _minHeadroom;
        private double _sectionPadding;
        private bool _useHostCeilings;
        private bool _useHostMep;
        private bool _checkHeadroom;
        private bool _autoSelect;
        private bool _markLinked;
        private string _ignoreKeywords;
        private string _companyName;
        private string _preparedBy;

        private bool _isBusy;
        private bool _cancelRequested;
        private double _progress;
        private string _statusText = "Ready.";
        private string _modelTitle = string.Empty;
        private string _logText = string.Empty;
        private string _visibleText = string.Empty;
        private CheckSummary _summary;

        private string _searchText = string.Empty;
        private int _severityFilterIndex;
        private string _selectedFilterCategory = AllLabel;
        private bool _hideReviewed;

        public MainViewModel(RevitActionDispatcher dispatcher, IDialogService dialogs, SettingsService settingsService)
        {
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");
            if (dialogs == null) throw new ArgumentNullException("dialogs");
            if (settingsService == null) throw new ArgumentNullException("settingsService");

            _dispatcher = dispatcher;
            _dialogs = dialogs;
            _settingsService = settingsService;

            Categories = new ObservableCollection<CategoryOption>();
            Levels = new ObservableCollection<LevelOption>();
            Links = new ObservableCollection<LinkOption>();
            Results = new ObservableCollection<CheckResultItem>();
            FilterCategories = new ObservableCollection<string>();
            ByCategory = new ObservableCollection<SummaryLine>();
            ByLevel = new ObservableCollection<SummaryLine>();
            BySystem = new ObservableCollection<SummaryLine>();

            CategoriesView = new ListCollectionView(Categories);
            CategoriesView.GroupDescriptions.Add(new PropertyGroupDescription("Discipline"));

            ResultsView = CollectionViewSource.GetDefaultView(Results);
            ResultsView.Filter = FilterItem;

            CheckSettings settings = _settingsService.Load();
            foreach (CategoryOption option in CategoryCatalog.CreateOptions(settings.SelectedCategories))
            {
                Categories.Add(option);
            }

            ApplySettings(settings);

            RunCheckCommand = new RelayCommand(RunCheck, delegate { return !_isBusy; });
            CancelCommand = new RelayCommand(delegate { _cancelRequested = true; StatusText = "Cancelling..."; }, delegate { return _isBusy; });
            ReloadModelCommand = new RelayCommand(ReloadModel, delegate { return !_isBusy; });
            SelectCommand = new RelayCommand(delegate { PostOperation("Select", SelectOperation); }, CanAct);
            ZoomCommand = new RelayCommand(delegate { PostOperation("Zoom", ZoomOperation); }, CanAct);
            IsolateCommand = new RelayCommand(delegate { PostOperation("Isolate", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.Isolate(app.ActiveUIDocument.Document, t); }); }, CanAct);
            ResetIsolateCommand = new RelayCommand(delegate { PostOperation("Reset isolate", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.ResetIsolation(app.ActiveUIDocument.Document); }); }, CanAct);
            ColorCommand = new RelayCommand(delegate { PostOperation("Colour", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.ApplyColors(app.ActiveUIDocument.Document, t); }); }, CanAct);
            ResetColorCommand = new RelayCommand(delegate { PostOperation("Reset colour", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.ResetColors(app.ActiveUIDocument.Document, t); }); }, CanAct);
            SectionBoxCommand = new RelayCommand(delegate { PostOperation("Section box", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.SetSectionBox(app.ActiveUIDocument.Document, t, PaddingFeet()); }); }, CanAct);
            ReviewViewCommand = new RelayCommand(delegate { PostOperation("Review view", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.CreateReviewView(app.ActiveUIDocument, t, PaddingFeet()); }); }, CanAct);
            HighlightCommand = new RelayCommand(delegate { PostOperation("Highlight", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.Highlight(app.ActiveUIDocument, t, PaddingFeet(), false, false, _markLinked); }); }, CanAct);
            VisualCheckCommand = new RelayCommand(delegate { PostOperation("Visual check", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.Highlight(app.ActiveUIDocument, t, PaddingFeet(), true, false, _markLinked); }); }, CanAct);
            VisualCheckIsolatedCommand = new RelayCommand(delegate { PostOperation("Visual check (isolated)", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.Highlight(app.ActiveUIDocument, t, PaddingFeet(), true, true, _markLinked); }); }, CanAct);
            CheckViewCommand = new RelayCommand(delegate { PostOperation("3D check view", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.CreateCheckView(app.ActiveUIDocument, t, PaddingFeet(), _markLinked); }); }, CanAct);
            ClearHighlightCommand = new RelayCommand(delegate { PostOperation("Clear highlight", delegate (UIApplication app, IList<CheckResultItem> t) { return _viewActions.ClearHighlight(app.ActiveUIDocument.Document, t); }); }, CanAct);
            CopyIdsCommand = new RelayCommand(CopyIds, CanAct);
            MarkCommentsCommand = new RelayCommand(MarkComments, CanAct);
            ClearCommentsCommand = new RelayCommand(ClearComments, CanAct);
            MarkReviewedCommand = new RelayCommand(MarkReviewed, CanAct);
            ExportCsvCommand = new RelayCommand(ExportCsv, delegate { return Results.Count > 0; });
            ExportHtmlCommand = new RelayCommand(ExportHtml, delegate { return Results.Count > 0 && _summary != null; });
            ResetSettingsCommand = new RelayCommand(delegate { ResetSettings(); });
            SelectAllCategoriesCommand = new RelayCommand(delegate { SetCategories(true, false); });
            ClearCategoriesCommand = new RelayCommand(delegate { SetCategories(false, false); });
            DefaultCategoriesCommand = new RelayCommand(delegate { SetCategories(false, true); });
            SelectAllLevelsCommand = new RelayCommand(delegate { SetLevels(true); });
            ClearLevelsCommand = new RelayCommand(delegate { SetLevels(false); });
        }

        public ObservableCollection<CategoryOption> Categories { get; private set; }
        public ListCollectionView CategoriesView { get; private set; }
        public ObservableCollection<LevelOption> Levels { get; private set; }
        public ObservableCollection<LinkOption> Links { get; private set; }
        public ObservableCollection<CheckResultItem> Results { get; private set; }
        public ICollectionView ResultsView { get; private set; }
        public ObservableCollection<string> FilterCategories { get; private set; }
        public ObservableCollection<SummaryLine> ByCategory { get; private set; }
        public ObservableCollection<SummaryLine> ByLevel { get; private set; }
        public ObservableCollection<SummaryLine> BySystem { get; private set; }

        public ICommand RunCheckCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand ReloadModelCommand { get; private set; }
        public ICommand SelectCommand { get; private set; }
        public ICommand ZoomCommand { get; private set; }
        public ICommand IsolateCommand { get; private set; }
        public ICommand ResetIsolateCommand { get; private set; }
        public ICommand ColorCommand { get; private set; }
        public ICommand ResetColorCommand { get; private set; }
        public ICommand SectionBoxCommand { get; private set; }
        public ICommand ReviewViewCommand { get; private set; }
        public ICommand HighlightCommand { get; private set; }
        public ICommand VisualCheckCommand { get; private set; }
        public ICommand VisualCheckIsolatedCommand { get; private set; }
        public ICommand CheckViewCommand { get; private set; }
        public ICommand ClearHighlightCommand { get; private set; }
        public ICommand CopyIdsCommand { get; private set; }
        public ICommand MarkCommentsCommand { get; private set; }
        public ICommand ClearCommentsCommand { get; private set; }
        public ICommand MarkReviewedCommand { get; private set; }
        public ICommand ExportCsvCommand { get; private set; }
        public ICommand ExportHtmlCommand { get; private set; }
        public ICommand ResetSettingsCommand { get; private set; }
        public ICommand SelectAllCategoriesCommand { get; private set; }
        public ICommand ClearCategoriesCommand { get; private set; }
        public ICommand DefaultCategoriesCommand { get; private set; }
        public ICommand SelectAllLevelsCommand { get; private set; }
        public ICommand ClearLevelsCommand { get; private set; }

        public string ModelTitle
        {
            get { return _modelTitle; }
            private set { SetProperty(ref _modelTitle, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged("IsNotBusy");
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsNotBusy
        {
            get { return !_isBusy; }
        }

        public double Progress
        {
            get { return _progress; }
            private set { SetProperty(ref _progress, value); }
        }

        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        public string LogText
        {
            get { return _logText; }
            private set { SetProperty(ref _logText, value); }
        }

        public string VisibleText
        {
            get { return _visibleText; }
            private set { SetProperty(ref _visibleText, value); }
        }

        public int CriticalCount
        {
            get { return _summary == null ? 0 : _summary.CriticalCount; }
        }

        public int WarningCount
        {
            get { return _summary == null ? 0 : _summary.WarningCount; }
        }

        public int HeadroomCount
        {
            get { return _summary == null ? 0 : _summary.HeadroomCount; }
        }

        public int ElementsChecked
        {
            get { return _summary == null ? 0 : _summary.ElementsChecked; }
        }

        public int CeilingFaceCount
        {
            get { return _summary == null ? 0 : _summary.CeilingFaceCount; }
        }

        public int UnitIndex
        {
            get { return (int)_displayUnit; }
            set
            {
                LengthUnit newUnit = (LengthUnit)value;
                if (newUnit == _displayUnit)
                {
                    return;
                }

                LengthUnit oldUnit = _displayUnit;
                _displayUnit = newUnit;
                _tolerance = Convert(_tolerance, oldUnit, newUnit);
                _fixedHeight = Convert(_fixedHeight, oldUnit, newUnit);
                _minHeadroom = Convert(_minHeadroom, oldUnit, newUnit);
                _sectionPadding = Convert(_sectionPadding, oldUnit, newUnit);

                foreach (CheckResultItem item in Results)
                {
                    item.ApplyUnit(newUnit);
                }

                OnPropertyChanged(string.Empty);
            }
        }

        public string UnitText
        {
            get { return UnitConverter.Abbreviation(_displayUnit); }
        }

        public int ModeIndex
        {
            get { return (int)_mode; }
            set
            {
                if (SetProperty(ref _mode, (ReferenceMode)value))
                {
                    OnPropertyChanged("IsFixedMode");
                    OnPropertyChanged("IsCeilingMode");
                }
            }
        }

        public bool IsFixedMode
        {
            get { return _mode == ReferenceMode.FixedHeightAboveLevel; }
        }

        public bool IsCeilingMode
        {
            get { return _mode == ReferenceMode.CeilingElements; }
        }

        public int ScopeIndex
        {
            get { return (int)_scope; }
            set { SetProperty(ref _scope, (ScopeMode)value); }
        }

        public double Tolerance
        {
            get { return _tolerance; }
            set { SetProperty(ref _tolerance, value); }
        }

        public double FixedHeight
        {
            get { return _fixedHeight; }
            set { SetProperty(ref _fixedHeight, value); }
        }

        public double MinHeadroom
        {
            get { return _minHeadroom; }
            set { SetProperty(ref _minHeadroom, value); }
        }

        public double SectionPadding
        {
            get { return _sectionPadding; }
            set { SetProperty(ref _sectionPadding, value); }
        }

        public bool UseHostCeilings
        {
            get { return _useHostCeilings; }
            set { SetProperty(ref _useHostCeilings, value); }
        }

        public bool UseHostMep
        {
            get { return _useHostMep; }
            set { SetProperty(ref _useHostMep, value); }
        }

        public bool CheckHeadroom
        {
            get { return _checkHeadroom; }
            set { SetProperty(ref _checkHeadroom, value); }
        }

        public bool MarkLinked
        {
            get { return _markLinked; }
            set { SetProperty(ref _markLinked, value); }
        }

        public bool AutoSelect
        {
            get { return _autoSelect; }
            set { SetProperty(ref _autoSelect, value); }
        }

        public string IgnoreKeywords
        {
            get { return _ignoreKeywords; }
            set { SetProperty(ref _ignoreKeywords, value); }
        }

        public string CompanyName
        {
            get { return _companyName; }
            set { SetProperty(ref _companyName, value); }
        }

        public string PreparedBy
        {
            get { return _preparedBy; }
            set { SetProperty(ref _preparedBy, value); }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (SetProperty(ref _searchText, value ?? string.Empty))
                {
                    RefreshFilter();
                }
            }
        }

        public int SeverityFilterIndex
        {
            get { return _severityFilterIndex; }
            set
            {
                if (SetProperty(ref _severityFilterIndex, value))
                {
                    RefreshFilter();
                }
            }
        }

        public string SelectedFilterCategory
        {
            get { return _selectedFilterCategory; }
            set
            {
                if (SetProperty(ref _selectedFilterCategory, value ?? AllLabel))
                {
                    RefreshFilter();
                }
            }
        }

        public bool HideReviewed
        {
            get { return _hideReviewed; }
            set
            {
                if (SetProperty(ref _hideReviewed, value))
                {
                    RefreshFilter();
                }
            }
        }

        /// <summary>Reads levels and links from the active document. Must run in a Revit API context.</summary>
        public void LoadModel(UIApplication uiApp)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            bool sameModel = string.Equals(_modelTitle, doc.Title, StringComparison.Ordinal);

            Levels.Clear();
            foreach (LevelOption level in _modelInfo.GetLevels(doc))
            {
                Levels.Add(level);
            }

            Links.Clear();
            foreach (LinkOption link in _modelInfo.GetLinks(doc))
            {
                Links.Add(link);
            }

            ModelTitle = doc.Title;
            if (!sameModel)
            {
                ClearResults();
                StatusText = "Model loaded: " + doc.Title;
            }
        }

        public void SetSelectedItems(IList<CheckResultItem> items)
        {
            _selected.Clear();
            _selected.AddRange(items);

            if (_autoSelect && _selected.Count > 0 && !_isBusy)
            {
                PostOperation("Select and zoom", delegate (UIApplication app, IList<CheckResultItem> targets)
                {
                    OperationReport report = _viewActions.Select(app.ActiveUIDocument, targets);
                    _viewActions.Zoom(app.ActiveUIDocument, targets, PaddingFeet());
                    return report;
                });
            }
        }

        public void SaveSettings()
        {
            _settingsService.Save(ToSettings());
        }

        private static double Convert(double value, LengthUnit from, LengthUnit to)
        {
            return Math.Round(UnitConverter.FromMm(UnitConverter.ToMm(value, from), to), 3);
        }

        private bool CanAct()
        {
            return !_isBusy && Results.Count > 0;
        }

        private double PaddingFeet()
        {
            return UnitConverter.ToFeet(_sectionPadding, _displayUnit);
        }

        private CheckSettings ToSettings()
        {
            CheckSettings settings = new CheckSettings();
            settings.DisplayUnit = _displayUnit;
            settings.Mode = _mode;
            settings.Scope = _scope;
            settings.ToleranceMm = UnitConverter.ToMm(_tolerance, _displayUnit);
            settings.FixedCeilingHeightMm = UnitConverter.ToMm(_fixedHeight, _displayUnit);
            settings.MinHeadroomMm = UnitConverter.ToMm(_minHeadroom, _displayUnit);
            settings.SectionBoxPaddingMm = UnitConverter.ToMm(_sectionPadding, _displayUnit);
            settings.UseHostCeilings = _useHostCeilings;
            settings.UseHostMep = _useHostMep;
            settings.CheckHeadroom = _checkHeadroom;
            settings.AutoSelectOnRowChange = _autoSelect;
            settings.MarkLinkedElements = _markLinked;
            settings.IgnoreKeywords = _ignoreKeywords;
            settings.CompanyName = _companyName;
            settings.PreparedBy = _preparedBy;
            settings.SelectedCategories = Categories.Where(c => c.IsSelected).Select(c => c.Category.ToString()).ToList();
            settings.Normalize();
            return settings;
        }

        private void ApplySettings(CheckSettings settings)
        {
            _displayUnit = settings.DisplayUnit;
            _mode = settings.Mode;
            _scope = settings.Scope;
            _tolerance = Math.Round(UnitConverter.FromMm(settings.ToleranceMm, _displayUnit), 3);
            _fixedHeight = Math.Round(UnitConverter.FromMm(settings.FixedCeilingHeightMm, _displayUnit), 3);
            _minHeadroom = Math.Round(UnitConverter.FromMm(settings.MinHeadroomMm, _displayUnit), 3);
            _sectionPadding = Math.Round(UnitConverter.FromMm(settings.SectionBoxPaddingMm, _displayUnit), 3);
            _useHostCeilings = settings.UseHostCeilings;
            _useHostMep = settings.UseHostMep;
            _checkHeadroom = settings.CheckHeadroom;
            _autoSelect = settings.AutoSelectOnRowChange;
            _markLinked = settings.MarkLinkedElements;
            _ignoreKeywords = settings.IgnoreKeywords;
            _companyName = settings.CompanyName;
            _preparedBy = settings.PreparedBy;
            OnPropertyChanged(string.Empty);
        }

        private void ResetSettings()
        {
            CheckSettings defaults = new CheckSettings();
            ApplySettings(defaults);
            foreach (CategoryOption option in Categories)
            {
                option.IsSelected = option.DefaultSelected;
            }
        }

        private void SetCategories(bool selected, bool defaults)
        {
            foreach (CategoryOption option in Categories)
            {
                option.IsSelected = defaults ? option.DefaultSelected : selected;
            }
        }

        private void SetLevels(bool selected)
        {
            foreach (LevelOption level in Levels)
            {
                level.IsSelected = selected;
            }
        }

        private void ReloadModel()
        {
            _dispatcher.Post(delegate (UIApplication app)
            {
                LoadModel(app);
                StatusText = "Model reloaded: " + app.ActiveUIDocument.Document.Title;
            });
        }

        private void RunCheck()
        {
            CheckSettings settings = ToSettings();
            List<BuiltInCategory> categories = Categories.Where(c => c.IsSelected).Select(c => c.Category).ToList();
            if (categories.Count == 0)
            {
                _dialogs.ShowInfo(Title, "Select at least one MEP category on the Categories tab.");
                return;
            }

            List<LevelOption> selectedLevels = Levels.Where(l => l.IsSelected).ToList();
            if (selectedLevels.Count == 0)
            {
                _dialogs.ShowInfo(Title, "Select at least one level on the Levels tab.");
                return;
            }

            CheckRequest request = new CheckRequest();
            request.Settings = settings;
            request.Categories.AddRange(categories);
            request.Links = Links.ToList();
            request.LevelNames = selectedLevels.Count == Levels.Count
                ? null
                : new HashSet<string>(selectedLevels.Select(l => l.Name));
            request.IsCancelled = delegate { return _cancelRequested; };
            request.Progress = delegate (double fraction, string text)
            {
                Progress = fraction;
                StatusText = text;
            };

            SaveSettings();
            _cancelRequested = false;
            Progress = 0.0;
            StatusText = "Starting check...";
            IsBusy = true;
            _dispatcher.Post(delegate (UIApplication app) { ExecuteCheck(app, request); });
        }

        private void ExecuteCheck(UIApplication app, CheckRequest request)
        {
            try
            {
                EnsureSameDocument(app);
                CheckResult result = _engine.Run(app, request);
                ShowResult(result);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Check cancelled.";
            }
            catch (InvalidOperationException ex)
            {
                StatusText = "The check could not run.";
                _dialogs.ShowError(Title, ex.Message);
            }
            catch (Exception ex)
            {
                StatusText = "The check failed.";
                _dialogs.ShowError(Title, "An unexpected error stopped the check.\n\n" + ex.Message);
            }
            finally
            {
                Progress = 0.0;
                IsBusy = false;
            }
        }

        private void ShowResult(CheckResult result)
        {
            ClearResults();
            foreach (CheckResultItem item in result.Items)
            {
                Results.Add(item);
            }

            _summary = result.Summary;
            foreach (SummaryLine line in result.Summary.ByCategory) ByCategory.Add(line);
            foreach (SummaryLine line in result.Summary.ByLevel) ByLevel.Add(line);
            foreach (SummaryLine line in result.Summary.BySystem) BySystem.Add(line);

            FilterCategories.Add(AllLabel);
            foreach (SummaryLine line in result.Summary.ByCategory)
            {
                FilterCategories.Add(line.Label);
            }

            _selectedFilterCategory = AllLabel;
            OnPropertyChanged("SelectedFilterCategory");
            LogText = string.Join(Environment.NewLine, result.Log);
            RaiseSummaryChanged();
            RefreshFilter();

            StatusText = result.Items.Count + " finding(s) from " + result.Summary.ElementsChecked + " checked element(s) in "
                + result.Summary.Duration.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " s.";

            if (result.Items.Count == 0)
            {
                _dialogs.ShowInfo(Title, "No MEP elements were found below the ceiling with the current settings.");
            }
        }

        private void ClearResults()
        {
            Results.Clear();
            ByCategory.Clear();
            ByLevel.Clear();
            BySystem.Clear();
            FilterCategories.Clear();
            _selected.Clear();
            _summary = null;
            LogText = string.Empty;
            RaiseSummaryChanged();
            RefreshFilter();
        }

        private void RaiseSummaryChanged()
        {
            OnPropertyChanged("CriticalCount");
            OnPropertyChanged("WarningCount");
            OnPropertyChanged("HeadroomCount");
            OnPropertyChanged("ElementsChecked");
            OnPropertyChanged("CeilingFaceCount");
        }

        private void EnsureSameDocument(UIApplication app)
        {
            UIDocument uiDoc = app.ActiveUIDocument;
            if (uiDoc == null || !string.Equals(uiDoc.Document.Title, _modelTitle, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The active document changed. Click 'Reload model' and run the check again.");
            }
        }

        private List<CheckResultItem> TargetItems()
        {
            if (_selected.Count > 0)
            {
                return new List<CheckResultItem>(_selected);
            }

            return ResultsView.Cast<CheckResultItem>().ToList();
        }

        private void PostOperation(string name, Func<UIApplication, IList<CheckResultItem>, OperationReport> work)
        {
            List<CheckResultItem> targets = TargetItems();
            if (targets.Count == 0)
            {
                _dialogs.ShowInfo(Title, "There are no visible results to act on.");
                return;
            }

            _dispatcher.Post(delegate (UIApplication app)
            {
                try
                {
                    EnsureSameDocument(app);
                    OperationReport report = work(app, targets);
                    StatusText = name + ": " + report;
                    if (!string.IsNullOrEmpty(report.Note))
                    {
                        _dialogs.ShowInfo(Title, report.Note);
                    }
                }
                catch (Exception ex)
                {
                    StatusText = name + " failed.";
                    _dialogs.ShowError(Title, name + " could not be completed.\n\n" + ex.Message);
                }
            });
        }

        private OperationReport SelectOperation(UIApplication app, IList<CheckResultItem> targets)
        {
            return _viewActions.Select(app.ActiveUIDocument, targets);
        }

        private OperationReport ZoomOperation(UIApplication app, IList<CheckResultItem> targets)
        {
            return _viewActions.Zoom(app.ActiveUIDocument, targets, PaddingFeet());
        }

        private void MarkComments()
        {
            List<CheckResultItem> targets = TargetItems();
            if (!_dialogs.Confirm(Title, "Write a '" + ViewActionService.MarkerTag + "' note into the Comments parameter of " + targets.Count
                + " host element(s)? Existing comments are kept. This modifies the model."))
            {
                return;
            }

            PostOperation("Mark comments", delegate (UIApplication app, IList<CheckResultItem> t)
            {
                return _viewActions.MarkComments(app.ActiveUIDocument.Document, t);
            });
        }

        private void ClearComments()
        {
            List<CheckResultItem> targets = TargetItems();
            if (!_dialogs.Confirm(Title, "Remove the '" + ViewActionService.MarkerTag + "' note from the Comments parameter of " + targets.Count
                + " host element(s)? This modifies the model."))
            {
                return;
            }

            PostOperation("Clear comments", delegate (UIApplication app, IList<CheckResultItem> t)
            {
                return _viewActions.ClearComments(app.ActiveUIDocument.Document, t);
            });
        }

        private void CopyIds()
        {
            List<CheckResultItem> targets = TargetItems();
            string text = string.Join(", ", targets.Where(t => !t.IsLinked).Select(t => t.ElementIdValue.ToString()).ToArray());
            if (text.Length == 0)
            {
                StatusText = "The chosen elements belong to linked models, so their ids cannot be used in the host model.";
                return;
            }

            StatusText = _dialogs.CopyToClipboard(text)
                ? "Element ids copied to the clipboard: " + text
                : "The clipboard is in use by another application. Try again.";
        }

        private void MarkReviewed()
        {
            List<CheckResultItem> targets = TargetItems();
            foreach (CheckResultItem item in targets)
            {
                item.IsReviewed = true;
            }

            StatusText = targets.Count + " item(s) marked as reviewed.";
        }

        private void ExportCsv()
        {
            List<CheckResultItem> items = ResultsView.Cast<CheckResultItem>().ToList();
            string path = _dialogs.PromptSaveFile("Export results to CSV", "CSV file (*.csv)|*.csv", DefaultFileName("csv"));
            if (path == null)
            {
                return;
            }

            try
            {
                _csvExport.Save(path, items, _displayUnit);
                StatusText = "CSV exported: " + path;
                if (_dialogs.Confirm(Title, "CSV exported. Open it now?"))
                {
                    _dialogs.OpenPath(path);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _dialogs.ShowError(Title, "The CSV file could not be written.\n\n" + ex.Message);
            }
        }

        private void ExportHtml()
        {
            List<CheckResultItem> items = ResultsView.Cast<CheckResultItem>().ToList();
            string path = _dialogs.PromptSaveFile("Export HTML report", "HTML report (*.html)|*.html", DefaultFileName("html"));
            if (path == null)
            {
                return;
            }

            try
            {
                CheckSettings current = ToSettings();
                _summary.Settings = current;
                _htmlReport.Save(path, _summary, items);
                StatusText = "Report exported: " + path;
                if (_dialogs.Confirm(Title, "Report exported. Open it now?"))
                {
                    _dialogs.OpenPath(path);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                _dialogs.ShowError(Title, "The report could not be written.\n\n" + ex.Message);
            }
        }

        private string DefaultFileName(string extension)
        {
            string model = string.IsNullOrWhiteSpace(_modelTitle) ? "Model" : _modelTitle;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                model = model.Replace(c, '_');
            }

            return "BelowCeilingCheck_" + model + "_" + DateTime.Now.ToString("yyyyMMdd") + "." + extension;
        }

        private void RefreshFilter()
        {
            ResultsView.Refresh();
            int visible = ResultsView.Cast<object>().Count();
            VisibleText = "Showing " + visible + " of " + Results.Count;
            CommandManager.InvalidateRequerySuggested();
        }

        private bool FilterItem(object candidate)
        {
            CheckResultItem item = candidate as CheckResultItem;
            if (item == null)
            {
                return false;
            }

            if (_hideReviewed && item.IsReviewed)
            {
                return false;
            }

            if (_severityFilterIndex == 1 && item.Severity != Severity.Critical) return false;
            if (_severityFilterIndex == 2 && item.Severity != Severity.Warning) return false;
            if (_severityFilterIndex == 3 && !item.HeadroomViolation) return false;

            if (!string.IsNullOrEmpty(_selectedFilterCategory) && _selectedFilterCategory != AllLabel
                && !string.Equals(item.Category, _selectedFilterCategory, StringComparison.Ordinal))
            {
                return false;
            }

            if (_searchText.Length > 0)
            {
                string haystack = string.Join(" ", new[]
                {
                    item.Category, item.Family, item.TypeName, item.SystemName, item.LevelName,
                    item.CeilingName, item.SourceText, item.Notes, item.ElementIdValue.ToString()
                });

                if (haystack.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

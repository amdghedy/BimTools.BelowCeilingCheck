using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BimTools.BelowCeilingCheck.Models;
using BimTools.BelowCeilingCheck.ViewModels;

namespace BimTools.BelowCeilingCheck.Views
{
    public partial class MainWindow : Window
    {
        private DataGridRow _rightClickedRow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private MainViewModel ViewModel
        {
            get { return DataContext as MainViewModel; }
        }

        private void ResultsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel viewModel = ViewModel;
            if (viewModel != null)
            {
                viewModel.SetSelectedItems(ResultsGrid.SelectedItems.OfType<CheckResultItem>().ToList());
            }
        }

        private void ResultsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MainViewModel viewModel = ViewModel;
            if (viewModel == null || FindRow(e.OriginalSource as DependencyObject) == null)
            {
                return;
            }

            if (viewModel.ZoomCommand.CanExecute(null))
            {
                viewModel.ZoomCommand.Execute(null);
            }
        }

        /// <summary>Right-click selects the row under the mouse so the menu acts on it.</summary>
        private void ResultsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rightClickedRow = FindRow(e.OriginalSource as DependencyObject);
            if (_rightClickedRow != null && !_rightClickedRow.IsSelected)
            {
                ResultsGrid.SelectedItems.Clear();
                _rightClickedRow.IsSelected = true;
            }
        }

        private void ResultsGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_rightClickedRow == null)
            {
                e.Handled = true;
            }
        }

        private static DataGridRow FindRow(DependencyObject start)
        {
            DependencyObject current = start;
            while (current != null)
            {
                DataGridRow row = current as DataGridRow;
                if (row != null)
                {
                    return row;
                }

                current = current is Visual
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MainViewModel viewModel = ViewModel;
            if (viewModel != null)
            {
                viewModel.SaveSettings();
            }
        }
    }
}

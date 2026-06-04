using System.IO;
using System.Windows;
using System.Windows.Input;
using SlymLynk.Models;
using SlymLynk.ViewModels;

namespace SlymLynk.Views;

public partial class MainWindow : Window
{
    private Point _dragStart;
    private bool _isDragPending;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += Window_MouseLeftButtonDown;
        MouseLeftButtonUp += Window_MouseLeftButtonUp;
        MouseMove += Window_MouseMove;
    }

    // --- Click handling ---

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragPending = false;

        // Only fire click if the source-loaded buttons didn't handle it.
        if (ViewModel.IsIdle)
            ViewModel.BrowseSourceCommand.Execute(null);
        else if (ViewModel.IsSourceLoaded)
            ViewModel.SaveToDestinationCommand.Execute(null);
    }

    // --- Drag-out handling ---

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.IsSourceLoaded)
        {
            _dragStart = e.GetPosition(this);
            _isDragPending = true;
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragPending || e.LeftButton != MouseButtonState.Pressed || !ViewModel.IsSourceLoaded)
            return;

        var current = e.GetPosition(this);
        var delta = current - _dragStart;

        // Only start drag once the cursor has moved beyond the system drag threshold.
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDragPending = false;
        StartDragOut();
    }

    private void StartDragOut()
    {
        if (ViewModel.SourcePath is not { } sourcePath) return;

        var data = new DataObject(DataFormats.FileDrop, new[] { sourcePath });

        // DoDragDrop blocks until the user releases. GetDropDestinationFolder reads
        // the cursor position immediately after return to identify the Explorer target.
        var effect = DragDrop.DoDragDrop(this, data, DragDropEffects.Link | DragDropEffects.Copy);

        if (effect == DragDropEffects.None) return;

        var destFolder = DragOutHelper.GetDropDestinationFolder();
        if (destFolder is null)
        {
            // Drop target wasn't a recognised Explorer window — fall back to save dialog.
            ViewModel.SaveToDestinationCommand.Execute(null);
            return;
        }

        var linkName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(destFolder, linkName);
        ViewModel.CreateLink(destPath);
    }

    // --- Drag-in handling ---

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Link
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Link
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths && paths.Length > 0)
            ViewModel.AcceptDropCommand.Execute(paths[0]);
    }
}

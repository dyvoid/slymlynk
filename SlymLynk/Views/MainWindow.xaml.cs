using System.Windows;
using System.Windows.Input;
using SlymLynk.ViewModels;

namespace SlymLynk.Views;

public partial class MainWindow : Window
{
    private bool _capturingDrag;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += Window_MouseLeftButtonDown;
        MouseLeftButtonUp += Window_MouseLeftButtonUp;
        MouseMove += Window_MouseMove;
    }

    // --- Mouse capture for drag-out ---

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.IsSourceLoaded)
        {
            _capturingDrag = true;
            // Capture keeps mouse events coming even when cursor leaves the window.
            Mouse.Capture(this);
        }
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_capturingDrag) return;

        // Show a link cursor once the user drags outside the window bounds.
        var pos = e.GetPosition(this);
        bool outside = pos.X < 0 || pos.Y < 0 || pos.X > ActualWidth || pos.Y > ActualHeight;
        Mouse.OverrideCursor = outside ? Cursors.Cross : null;
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_capturingDrag)
        {
            _capturingDrag = false;
            Mouse.OverrideCursor = null;
            Mouse.Capture(null);

            var pos = e.GetPosition(this);
            bool releasedOutside = pos.X < 0 || pos.Y < 0 || pos.X > ActualWidth || pos.Y > ActualHeight;

            if (releasedOutside && ViewModel.IsSourceLoaded)
            {
                ViewModel.CompleteDragOutCommand.Execute(null);
                return;
            }
        }

        // Normal click: not a drag-out, or released inside window.
        if (ViewModel.IsIdle)
            ViewModel.BrowseSourceCommand.Execute(null);
        else if (ViewModel.IsSourceLoaded)
            ViewModel.SaveToDestinationCommand.Execute(null);
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

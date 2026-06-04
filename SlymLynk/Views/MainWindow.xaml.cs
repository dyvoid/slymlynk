using System.Windows;
using SlymLynk.ViewModels;

namespace SlymLynk.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
        MouseLeftButtonUp += (_, _) => OnWindowClick();
    }

    private void OnWindowClick()
    {
        if (ViewModel.IsIdle)
            ViewModel.BrowseSourceCommand.Execute(null);
        else if (ViewModel.IsSourceLoaded)
            ViewModel.SaveToDestinationCommand.Execute(null);
    }

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

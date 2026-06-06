using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlymLynk.Models;
using SlymLynk.Services;

namespace SlymLynk.ViewModels;

public enum AppState { Idle, SourceLoaded, Error }

public partial class MainViewModel : ObservableObject
{
    private readonly SymlinkService _service;
    private readonly IFileDialogService _dialogs;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsSourceLoaded))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    private AppState _state = AppState.Idle;

    [ObservableProperty] private string? _sourcePath;
    [ObservableProperty] private string? _sourceDisplayName;
    [ObservableProperty] private string? _errorMessage;

    public bool IsIdle => State == AppState.Idle;
    public bool IsSourceLoaded => State == AppState.SourceLoaded;
    public bool IsError => State == AppState.Error;

    public MainViewModel() : this(new SymlinkService(), new Win32FileDialogService()) { }

    public MainViewModel(SymlinkService service, IFileDialogService dialogs)
    {
        _service = service;
        _dialogs = dialogs;
    }

    /// <summary>Accepts a dropped path as the link source.</summary>
    [RelayCommand]
    public void AcceptDrop(string path)
    {
        try
        {
            SourcePath = _service.ValidateSource(path);
            SourceDisplayName = Path.GetFileName(SourcePath) is { Length: > 0 } n ? n : SourcePath;
            State = AppState.SourceLoaded;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    /// <summary>Opens a file/folder browser to select the link source.</summary>
    [RelayCommand]
    public void BrowseSource()
    {
        if (_dialogs.PickSource() is { } source)
            AcceptDrop(source);
    }

    /// <summary>Opens a save dialog to choose the destination and creates the link.</summary>
    [RelayCommand(CanExecute = nameof(IsSourceLoaded))]
    public void SaveToDestination()
    {
        if (SourcePath is null) return;

        var isDir = _service.IsDirectory(SourcePath);
        if (_dialogs.PickDestination(SourceDisplayName, isDir) is { } dest)
            CreateLink(dest);
    }

    /// <summary>Creates the link at the given destination path.</summary>
    public void CreateLink(string destinationPath)
    {
        if (SourcePath is null) return;

        try
        {
            _service.Create(SourcePath, destinationPath);
            // Stay in SourceLoaded — user can link to multiple destinations.
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    /// <summary>Resets to idle and clears the loaded source.</summary>
    [RelayCommand]
    public void Clear()
    {
        SourcePath = null;
        SourceDisplayName = null;
        ErrorMessage = null;
        State = AppState.Idle;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        State = AppState.Error;
    }
}

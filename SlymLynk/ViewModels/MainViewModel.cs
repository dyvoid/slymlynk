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
    private readonly IDropTargetResolver _dropTargets;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(IsSourceLoaded))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    private AppState _state = AppState.Idle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SourcePath))]
    private ValidatedSource? _source;

    [ObservableProperty] private string? _sourceDisplayName;
    [ObservableProperty] private string? _errorMessage;

    public bool IsIdle => State == AppState.Idle;
    public bool IsSourceLoaded => State == AppState.SourceLoaded;
    public bool IsError => State == AppState.Error;

    /// <summary>The resolved source path, or null when no source is loaded.</summary>
    public string? SourcePath => Source?.Path;

    public MainViewModel()
        : this(new SymlinkService(), new Win32FileDialogService(), new ExplorerDropTargetResolver()) { }

    public MainViewModel(SymlinkService service, IFileDialogService dialogs, IDropTargetResolver dropTargets)
    {
        _service = service;
        _dialogs = dialogs;
        _dropTargets = dropTargets;
    }

    /// <summary>Accepts a dropped path as the link source.</summary>
    [RelayCommand]
    public void AcceptDrop(string path)
    {
        try
        {
            var validated = _service.ValidateSource(path);
            Source = validated;
            SourceDisplayName = Path.GetFileName(validated.Path) is { Length: > 0 } n ? n : validated.Path;
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
        if (Source is not { } source) return;

        if (_dialogs.PickDestination(SourceDisplayName, source.IsDirectory) is { } dest)
            CreateLink(dest);
    }

    /// <summary>
    /// Completes a drag-out gesture: resolves the folder under the cursor and
    /// creates the link there, falling back to the destination picker when the
    /// drop target is not a recognised Explorer location.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsSourceLoaded))]
    public void CompleteDragOut()
    {
        if (Source is not { } source) return;

        var destFolder = _dropTargets.ResolveFolderUnderCursor();
        if (destFolder is null)
        {
            // Cursor wasn't over an Explorer window — fall back to the picker.
            SaveToDestination();
            return;
        }

        var linkName = Path.GetFileName(source.Path);
        CreateLink(Path.Combine(destFolder, linkName));
    }

    /// <summary>Creates the link at the given destination path.</summary>
    public void CreateLink(string destinationPath)
    {
        if (Source is not { } source) return;

        try
        {
            _service.Create(source, destinationPath);
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
        Source = null;
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

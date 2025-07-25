using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soundboard;
using Soundboard.Core.Shortcut;
using Soundboard.Core.Utilities;
using Soundboard.Views;

public partial class ShortcutPopupViewModel : ObservableObject
{
    private readonly ShortcutPopupWindow _owner;
    private bool _waitingForInput = true;

    public bool Success { get; private set; }

    [ObservableProperty]
    private string? capturedShortcut = "Waiting...";

    public ShortcutPopupViewModel(ShortcutPopupWindow owner)
    {
        _owner = owner;

        ShortcutManager.ShortcutPressed += shortcut =>
        {
            UiThreadHelper.Run(() =>
            {
                CaptureShortcut(shortcut);
            });
        };
    }

    private void CaptureShortcut(string shortcut)
    {
        if (!_waitingForInput) return;

        CapturedShortcut = shortcut;

        _waitingForInput = false;
    }

    [RelayCommand]
    private void AllowInput()
    {
        CapturedShortcut = "Waiting...";
        _waitingForInput = true;
    }

    [RelayCommand]
    private void OkPopup()
    {
        if (string.IsNullOrEmpty(CapturedShortcut) || CapturedShortcut == "Waiting...")
            Success = false;
        else
            Success = true;

        _owner.Close();
    }

    [RelayCommand]
    private void CancelPopup()
    {
        Success = false;

        _owner.Close();
    }

    [RelayCommand]
    private void ClearShortcut()
    {
        CapturedShortcut = "None";
    }
}

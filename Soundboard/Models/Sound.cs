using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soundboard.Core.Audio;
using Soundboard.Core.Config;
using Soundboard.Views;
using System.Threading.Tasks;

namespace Soundboard.Models;

public partial class Sound : ObservableObject
{
    public Sound() { }

    [ObservableProperty]
    public string name = string.Empty;

    [ObservableProperty]
    private bool enableVolume;

    [ObservableProperty]
    private float volume;

    [ObservableProperty]
    private bool enablePitch;

    [ObservableProperty]
    private float pitch;

    [ObservableProperty]
    private bool randomPitch;

    [ObservableProperty]
    private bool reversed;

    [ObservableProperty]
    private string shortcut = string.Empty;

    [ObservableProperty]
    public bool hasShortcut;

    public Sound(string name, bool customVolume, float volume, bool customPitch, float pitch, bool randomPitch, bool reversed, string shortcut)
    {
        Name = name;
        EnableVolume = customVolume;
        Volume = volume;
        EnablePitch = customPitch;
        Pitch = pitch;
        RandomPitch = randomPitch;
        Reversed = reversed;
        Shortcut = shortcut;
    }

    partial void OnEnableVolumeChanged(bool value)
    {
        if (value == true)
            AudioPlayer.UpdateSoundVolume(Name, Volume);
        else
            AudioPlayer.UpdateSoundVolume(Name, ConfigManager.ConfigData.GeneralVolume);
    }

    partial void OnEnablePitchChanged(bool value)
    {
        if (value == true)
            AudioPlayer.UpdateSoundPitch(Name, Pitch);
        else
            AudioPlayer.UpdateSoundPitch(Name, ConfigManager.ConfigData.GeneralPitch);
    }

    partial void OnVolumeChanged(float value)
    {
        if (!EnableVolume) return;

        AudioPlayer.UpdateSoundVolume(Name, value);
    }

    partial void OnPitchChanged(float value)
    {
        if (!EnablePitch) return;

        AudioPlayer.UpdateSoundPitch(Name, value);
    }

    partial void OnShortcutChanged(string value)
    {
        HasShortcut = string.IsNullOrEmpty(value) ? false : true;
    }

    [RelayCommand]
    private async Task ChangeSoundShortcut()
    {
        ShortcutPopupWindow popupWindow = new();
        ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

        if (popupViewModel == null)
            return;

        await popupWindow.ShowDialog(MainWindow.Instance);

        if (!popupViewModel.Success)
            return;

        if (popupViewModel.CapturedShortcut == null)
            return;

        Shortcut = popupViewModel.CapturedShortcut != "None" ? popupViewModel.CapturedShortcut : string.Empty;
    }

    [RelayCommand]
    private void ClearShortcut()
    {
        Shortcut = string.Empty;
    }
}

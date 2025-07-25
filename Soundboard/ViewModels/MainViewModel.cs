using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using SharpHook.Data;
using Soundboard.Core.Audio;
using Soundboard.Core.Config;
using Soundboard.Core.Shortcut;
using Soundboard.Core.Utilities;
using Soundboard.Models;
using Soundboard.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soundboard.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<Sound> Sounds { get; set; } = [];

    // Search bar
    [ObservableProperty]
    private string searchText = string.Empty;

    private readonly ObservableCollection<Sound> _filteredSounds = [];
    private readonly ReadOnlyObservableCollection<Sound> _readonlyFilteredSounds;
    public ReadOnlyObservableCollection<Sound> FilteredSounds => _readonlyFilteredSounds;

    // Voice recording
    private AudioRecorder? _recorder = new();
    private string _newSoundName = null!;
    private bool _isRecording;
    private Sound _latestVoiceRecorded = null!;

    // Shortcut
    private readonly ShortcutManager _shortcutManager = new();
    
    // View properties

    [ObservableProperty]
    private Sound? selectedSound;

    [ObservableProperty]
    private float generalVolume;

    [ObservableProperty]
    private string generalVolumeText = string.Empty;

    [ObservableProperty]
    private float generalPitch;

    [ObservableProperty]
    private string generalPitchText = string.Empty;

    [ObservableProperty]
    private string recordingState = "🎙️";

    private string _currentSoundsFolder = string.Empty;
    private Sound _latestSoundPlayed = null!;

    public MainViewModel()
    {
        _readonlyFilteredSounds = new ReadOnlyObservableCollection<Sound>(_filteredSounds);

        ConfigManager.LoadConfig();

        ApplyConfig();
        LoadSounds();

        AudioPlayer.IsPlayingChanged += OnIsPlayingChanged;

        ShortcutManager.ShortcutPressed += OnShortcutPressed;

        App.OnAppClosing += Dispose;
    }

    private static Sound GetSound(string name, bool customVolume = false, float volume = 0.5f, bool customPitch = false, float pitch = 1.0f, bool randomPitch = false, bool reversed = false, string shortcut = "")
    {
        return new Sound(name, customVolume, volume, customPitch, pitch, randomPitch, reversed, shortcut);
    }

    private void ApplyConfig()
    {
        ConfigData configData = ConfigManager.ConfigData;

        GeneralVolume = configData.GeneralVolume;
        GeneralPitch = configData.GeneralPitch;
        _currentSoundsFolder = configData.SoundsFolder;
    }

    private void LoadSounds()
    {
        if (string.IsNullOrEmpty(_currentSoundsFolder))
            return;

        var soundFilesPaths = Directory.EnumerateFiles(_currentSoundsFolder, "*.*", SearchOption.AllDirectories)
                                       .Where(f => ConfigManager.AudioExtensions
                                       .Contains(Path.GetExtension(f).ToLower()));

        foreach (string fullPath in soundFilesPaths)
        {
            string fileName = Path.GetFileName(fullPath);

            var existingSound = ConfigManager.ConfigData.SoundsData.FirstOrDefault(s => s.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

            Sound sound;

            if (existingSound != null)
            {
                sound = GetSound(existingSound.Name, existingSound.EnableVolume, existingSound.Volume, existingSound.EnablePitch, existingSound.Pitch, existingSound.RandomPitch, existingSound.Reversed, existingSound.Shortcut);
            }
            else
            {
                sound = GetSound(fileName);
            }

            Sounds.Add(sound);
        }

        ApplySoundFilter();
    }

    partial void OnSelectedSoundChanged(Sound? value)
    {
        PlaySelectedSoundCommand.NotifyCanExecuteChanged();
        RemoveSelectedSoundCommand.NotifyCanExecuteChanged();
    }

    private void OnIsPlayingChanged()
    {
        StopAllSoundsCommand.NotifyCanExecuteChanged();
    }

    private void OnShortcutPressed(string shortcut)
    {
        UiThreadHelper.Run(() =>
        {
            TryApplyQOLShortcuts(shortcut);
            PlaySoundWithShortcut(shortcut);
            TryStopAllSoundsShortcut(shortcut);
            TryPlayRandomSoundShortcut(shortcut);
            TryPlayLatestSoundPitched(shortcut);
            TryPlayLatestSoundReversed(shortcut);

            if (shortcut == ConfigManager.ConfigData.StartStopVoiceRecordingShortcut)
            {
                if (!_isRecording)
                    StartVoiceRecording();
                else
                    StopVoiceRecording();
            }

            TryPlayLatestVoiceRecorded(shortcut);
            TryPlayLatestVoiceRecordedPitched(shortcut);
        });
    }

    #region Play Sound

    private void PlaySound(Sound sound, float customPitch = 1.0f, bool forceReversed = false)
    {
        string soundPath = Path.Combine(_currentSoundsFolder, sound.Name);

        if (!File.Exists(soundPath))
        {
            Sounds.Remove(sound);
            ApplySoundFilter();
            return;
        }

        float volume = sound.EnableVolume ? sound.Volume : ConfigManager.ConfigData.GeneralVolume;
        float pitch = customPitch != 1.0f ? customPitch : (sound.RandomPitch ? GetRandomPitch() : (sound.EnablePitch ? sound.Pitch : ConfigManager.ConfigData.GeneralPitch));
        bool reversed = forceReversed == true || sound.Reversed;

        AudioPlayer.Play(soundPath, volume, pitch, reversed);

        _latestSoundPlayed = sound;
    }

    [RelayCommand(CanExecute = nameof(CanPlaySelectedSound))]
    private void PlaySelectedSound()
    {
        if (SelectedSound == null) return;

        PlaySound(SelectedSound);
    }

    private bool CanPlaySelectedSound()
    {
        return SelectedSound != null;
    }

    private void PlaySoundWithShortcut(string shortcut)
    {
        foreach (Sound sound in Sounds)
        {
            if (sound.Shortcut == shortcut)
            {
                PlaySound(sound);
            }
        }
    }

    private static float GetRandomPitch()
    {
        Random random = new();
        float min = ConfigManager.ConfigData.RandomPitchRangeMin;
        float max = ConfigManager.ConfigData.RandomPitchRangeMax;

        return (float)(random.NextDouble() * (max - min) + min);
    }

    #endregion

    #region Stop All Sounds

    [RelayCommand(CanExecute = nameof(CanStopAllSounds))]
    private void StopAllSounds()
    {
        AudioPlayer.StopAll();
    }

    private static bool CanStopAllSounds()
    {
        return AudioPlayer.IsPlaying;
    }

    #endregion

    #region General Volume and Pitch

    partial void OnGeneralVolumeChanged(float value)
    {
        List<string> excludedSounds = Sounds.Where(sound => sound.EnableVolume)
                                            .Select(sound => sound.Name)
                                            .ToList();

        GeneralVolumeText = $"Volume ({(int)(value * 100)})";

        ConfigManager.ConfigData.GeneralVolume = value;
        AudioPlayer.UpdateAllSoundsVolume(value, excludedSounds);
    }

    partial void OnGeneralPitchChanged(float value)
    {
        List<string> excludedSounds = Sounds.Where(sound => sound.EnablePitch)
                                            .Select(sound => sound.Name)
                                            .ToList();

        GeneralPitchText = $"Pitch ({value.ToString("0.00")})";

        ConfigManager.ConfigData.GeneralPitch = value;
        AudioPlayer.UpdateAllSoundsPitch(value, excludedSounds);
    }

    #endregion

    #region General Shortcuts

    private void TryPlayRandomSoundShortcut(string shortcut)
    {
        if (Sounds.Count == 0) return;

        Random random = new ();

        if (shortcut == ConfigManager.ConfigData.PlayRandomSoundShortcut)
        {
            int randomSoundIndex = random.Next(0, Sounds.Count);

            PlaySound(Sounds[randomSoundIndex]);
        }
    }

    private static void TryStopAllSoundsShortcut(string shortcut)
    {
        if (shortcut == ConfigManager.ConfigData.StopAllSoundsShortcut)
            AudioPlayer.StopAll();
    }

    private void TryPlayLatestSoundPitched(string shortcut)
    {
        if (shortcut != ConfigManager.ConfigData.PlayLatestSoundPitchedShortcut)
            return;

        if (_latestSoundPlayed == null)
            return;

        PlaySound(_latestSoundPlayed, GetRandomPitch());
    }

    private void TryPlayLatestSoundReversed(string shortcut)
    {
        if (shortcut != ConfigManager.ConfigData.PlayLatestSoundReversedShortcut)
            return;

        if (_latestSoundPlayed == null)
            return;

        PlaySound(_latestSoundPlayed, 1.0f, true);
    }

    #endregion

    #region QOL Shortcuts

    private void TryApplyQOLShortcuts(string shortcut)
    {
        if (!AppFocusTracker.IsAppFocused)
            return;

        if (shortcut == ShortcutManager.GetFormattedKey(KeyCode.VcDelete) && SelectedSound != null)
        {
            _ = RemoveSelectedSound();
        }

        if (shortcut == ShortcutManager.GetFormattedKey(KeyCode.VcSpace) && SelectedSound != null)
        {
            PlaySelectedSound();
        }
    }

    #endregion

    #region Top Toolbar

    [RelayCommand]
    private async Task AddSound()
    {
        var soundFileType = new FilePickerFileType("Audio Files")
        {
            Patterns = ["*.mp3", "*.wav", "*.flac", "*.aac", "*.m4a"],
            AppleUniformTypeIdentifiers = ["public.audio"],
            MimeTypes =
            [
                "audio/mpeg",
                "audio/wav",
                "audio/flac",
                "audio/aac",
                "audio/mp4"
            ]
        };

        var options = new FilePickerOpenOptions
        {
            Title = "Choose a sound to add",
            AllowMultiple = false,
            FileTypeFilter = [soundFileType]
        };

        var files = await MainWindow.Instance.StorageProvider.OpenFilePickerAsync(options);

        if (files != null && files.Count > 0)
        {
            var originalPath = files[0].TryGetLocalPath();

            if (!string.IsNullOrEmpty(originalPath))
            {
                var fileName = Path.GetFileName(originalPath);
                var targetPath = Path.Combine(_currentSoundsFolder, fileName);

                if (!File.Exists(targetPath))
                {
                    File.Copy(originalPath, targetPath);
                }

                var sound = GetSound(Path.GetFileName(fileName));

                Sounds.Add(sound);
                SelectedSound = sound;
                ApplySoundFilter();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanRemoveSelectedSound))]
    private async Task RemoveSelectedSound()
    {
        if (SelectedSound == null)
            return;

        var box = MessageBoxManager.GetMessageBoxStandard(
            "Are you sure you want to delete this audio?",
            "This will permanently delete the file and cannot be undone.",
            MsBox.Avalonia.Enums.ButtonEnum.YesNo);

        var result = await box.ShowAsync();

        if (result == MsBox.Avalonia.Enums.ButtonResult.No)
            return;

        if (AudioPlayer.IsAudioBeingPlayed(SelectedSound.Name))
            AudioPlayer.StopAudio(SelectedSound.Name);

        var path = Path.Combine(_currentSoundsFolder, SelectedSound.Name);
        var oldIndex = Sounds.IndexOf(SelectedSound);

        Sounds.Remove(SelectedSound);
        ApplySoundFilter();

        File.Delete(path);

        if (FilteredSounds.Count > 0)
        {
            var newIndex = Math.Min(oldIndex, FilteredSounds.Count - 1);

            SelectedSound = FilteredSounds[newIndex];
        }
        else
        {
            SelectedSound = null;
        }
    }

    private bool CanRemoveSelectedSound()
    {
        return SelectedSound != null;
    }

    [RelayCommand]
    private void StartStopVoiceRecording()
    {
        if (_isRecording)
        {
            StopVoiceRecording();
        }
        else
        {
            StartVoiceRecording();
        }
    }

    [RelayCommand]
    private void RefreshSoundsFolder()
    {
        AudioPlayer.StopAll();

        Sounds.Clear();

        LoadSounds();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySoundFilter();
    }

    private void ApplySoundFilter()
    {
        _filteredSounds.Clear();

        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Sounds
            : Sounds.Where(s => s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var sound in filtered)
        {
            _filteredSounds.Add(sound);
        }
    }

    [RelayCommand]
    private void ResetVolume()
    {
        GeneralVolume = 0.5f;
    }

    [RelayCommand]
    private void ResetPitch()
    {
        GeneralPitch = 1.0f;
    }

    [RelayCommand]
    private async Task OpenConfigWindow()
    {
        ConfigWindow configWindow = new();

        await configWindow.ShowDialog(MainWindow.Instance);

        if (_currentSoundsFolder != ConfigManager.ConfigData.SoundsFolder)
        {
            _currentSoundsFolder = ConfigManager.ConfigData.SoundsFolder;

            AudioPlayer.StopAll();

            Sounds.Clear();

            LoadSounds();
        }
    }

    private void SaveConfig()
    {
        ConfigManager.SaveConfig([.. Sounds]);
    }

    #endregion

    #region Voice Recording

    private void StartVoiceRecording()
    {
        if (_isRecording)
            return;

        if (!ConfigManager.ConfigData.RecordVirtualMic && !ConfigManager.ConfigData.RecordSpeaker)
            return;

        InternalSoundPlayer soundPlayer = new();
        soundPlayer.Play("start_recording.wav");

        _isRecording = true;
        _newSoundName = $"Recording-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.wav";

        RecordingState = "⏹️";

        _recorder?.StartRecording(Path.Combine(_currentSoundsFolder, _newSoundName));
    }

    private void StopVoiceRecording()
    {
        if (!_isRecording) 
            return;

        RecordingState = "🎙️";

        _recorder?.StopRecording();

        var sound = GetSound(_newSoundName);

        Sounds.Add(sound);
        ApplySoundFilter();

        SelectedSound = sound;
        _latestVoiceRecorded = sound;

        _isRecording = false;

        InternalSoundPlayer soundPlayer = new();
        soundPlayer.Play("stop_recording.wav");
    }

    private void TryPlayLatestVoiceRecorded(string shortcut)
    {
        if (shortcut != ConfigManager.ConfigData.PlayLatestVoiceRecordingShortcut)
            return;

        if (_latestVoiceRecorded == null)
            return;

        PlaySound(_latestVoiceRecorded);
    }

    private void TryPlayLatestVoiceRecordedPitched(string shortcut)
    {
        if (shortcut != ConfigManager.ConfigData.PlayLatestVoiceRecordingPitchedShortcut)
            return;

        if (_latestVoiceRecorded == null)
            return;

        PlaySound(_latestVoiceRecorded, GetRandomPitch());
    }

    #endregion

    public void Dispose()
    {
        if (_isRecording)
            StopVoiceRecording();

        AudioPlayer.StopAll();

        _recorder?.StopRecording();
        _recorder = null;

        AudioPlayer.IsPlayingChanged -= OnIsPlayingChanged;
        ShortcutManager.ShortcutPressed -= OnShortcutPressed;
        _shortcutManager.Dispose();

        SaveConfig();

        App.OnAppClosing -= Dispose;
    }
}
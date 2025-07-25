using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using NAudio.CoreAudioApi;
using Soundboard.Core.Audio.External;
using Soundboard.Core.Config;
using Soundboard.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Soundboard.ViewModels
{
    public partial class ConfigViewModel : ObservableObject
    {
        public ObservableCollection<string> MicrophoneDevices { get; set; } = [];
        public ObservableCollection<string> SpeakerDevices { get; set; } = [];

        [ObservableProperty]
        private string selectedMicrophone = string.Empty;

        [ObservableProperty]
        private string selectedSpeaker = string.Empty;

        [ObservableProperty]
        private string soundsFolder = string.Empty;

        [ObservableProperty]
        private string stopAllSoundsShortcut = "None";

        [ObservableProperty]
        private string playRandomSoundShortcut = "None";

        [ObservableProperty]
        private string playLatestSoundPitchedShortcut = "None";

        [ObservableProperty]
        private string playLatestSoundReversedShortcut = "None";

        [ObservableProperty]
        private string   startStopVoiceRecordingShortcut = "None";

        [ObservableProperty]
        private string playLatestVoiceRecordingShortcut = "None";

        [ObservableProperty]
        private string playLatestVoiceRecordingPitchedShortcut = "None";

        [ObservableProperty]
        private bool recordVirtualMic;

        [ObservableProperty]
        private bool recordSpeaker;

        [ObservableProperty]
        private float minRandomPitchRange = 0.5f;

        [ObservableProperty]
        private float maxRandomPitchRange = 2.0f;

        [ObservableProperty]
        private bool exitToTray;

        private string _startingSoundsPath = string.Empty;

        public ConfigViewModel()
        {
            LoadAvailableDevices();
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            ConfigData data = ConfigManager.ConfigData;

            // Apply mic
            foreach (var mic in MicrophoneDevices)
            {
                if (data.VirtualMicName == mic)
                {
                    SelectedMicrophone = MicrophoneDevices[MicrophoneDevices.IndexOf(data.VirtualMicName)];
                }
            }

            // Apply speaker
            foreach (var speaker in SpeakerDevices)
            {
                if (data.SpeakerName == speaker)
                {
                    SelectedSpeaker = SpeakerDevices[SpeakerDevices.IndexOf(data.SpeakerName)];
                }
            }

            // Apply sounds folder
            if (Directory.Exists(data.SoundsFolder))
            {
                SoundsFolder = data.SoundsFolder;
                _startingSoundsPath = SoundsFolder;
            }

            // Apply shortcuts
            StopAllSoundsShortcut = data.StopAllSoundsShortcut == string.Empty ? "None" : data.StopAllSoundsShortcut;
            PlayRandomSoundShortcut = data.PlayRandomSoundShortcut == string.Empty ? "None" : data.PlayRandomSoundShortcut;
            PlayLatestSoundPitchedShortcut = data.PlayLatestSoundPitchedShortcut == string.Empty ? "None" : data.PlayLatestSoundPitchedShortcut;
            PlayLatestSoundReversedShortcut = data.PlayLatestSoundReversedShortcut == string.Empty ? "None" : data.PlayLatestSoundReversedShortcut;

            // Apply audio recording
            StartStopVoiceRecordingShortcut = data.StartStopVoiceRecordingShortcut == string.Empty ? "None" : data.StartStopVoiceRecordingShortcut;
            PlayLatestVoiceRecordingShortcut = data.PlayLatestVoiceRecordingShortcut == string.Empty ? "None" : data.PlayLatestVoiceRecordingShortcut;
            PlayLatestVoiceRecordingPitchedShortcut = data.PlayLatestVoiceRecordingPitchedShortcut == string.Empty ? "None" : data.PlayLatestVoiceRecordingPitchedShortcut;
            RecordVirtualMic = data.RecordVirtualMic;
            RecordSpeaker = data.RecordSpeaker;

            // Apply min and max random range
            MinRandomPitchRange = data.RandomPitchRangeMin;
            MaxRandomPitchRange = data.RandomPitchRangeMax;

            // Apply tray config
            ExitToTray = data.ExitToTray;
        }

        private void LoadAvailableDevices()
        {
            var enumerator = new MMDeviceEnumerator();
            MMDeviceCollection devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

            foreach (var device in devices)
            {
                MicrophoneDevices.Add(device.FriendlyName);
            }

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);

                SpeakerDevices.Add(capabilities.ProductName);
            }

            if (MicrophoneDevices.Count > 0)
            {
                string? cableInput = MicrophoneDevices.FirstOrDefault(mic => mic == "CABLE Input (VB-Audio Virtual Cable)");

                if (cableInput == null)
                    SelectedMicrophone = MicrophoneDevices[0];
                else
                    SelectedMicrophone = MicrophoneDevices[MicrophoneDevices.IndexOf(cableInput)];
            }

            if (SpeakerDevices.Count > 0)
            {
                SelectedSpeaker = SpeakerDevices[0];
            }
        }

        [RelayCommand]
        private async Task ChooseSoundsFolder()
        {
            var options = new FolderPickerOpenOptions
            {
                Title = "Choose sounds folder",
                AllowMultiple = false
            };

            var folders = await ConfigWindow.Instance.StorageProvider.OpenFolderPickerAsync(options);

            if (folders != null && folders.Count > 0)
            {
                var path = folders[0].TryGetLocalPath();

                SoundsFolder = path ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SetStopAllSoundsShortcut()
        {
            ShortcutPopupWindow popupWindow = new();
            ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

            await popupWindow.ShowDialog(MainWindow.Instance);

            if (popupViewModel != null && popupViewModel.Success)
            {
                StopAllSoundsShortcut = popupViewModel.CapturedShortcut ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SetPlayRandomSoundShortcut()
        {
            ShortcutPopupWindow popupWindow = new();
            ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

            await popupWindow.ShowDialog(MainWindow.Instance);

            if (popupViewModel != null && popupViewModel.Success)
            {
                PlayRandomSoundShortcut = popupViewModel.CapturedShortcut ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SetPlayLatestSoundPitchedShortcut()
        {
            ShortcutPopupWindow popupWindow = new();
            ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

            await popupWindow.ShowDialog(MainWindow.Instance);

            if (popupViewModel != null && popupViewModel.Success)
            {
                PlayLatestSoundPitchedShortcut = popupViewModel.CapturedShortcut ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SetPlayLatestSoundReversedShortcut()
        {
            ShortcutPopupWindow popupWindow = new();
            ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

            await popupWindow.ShowDialog(MainWindow.Instance);

            if (popupViewModel != null && popupViewModel.Success)
            {
                PlayLatestSoundReversedShortcut = popupViewModel.CapturedShortcut ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SetStartStopVoiceRecordingShortcut()
        {
            ShortcutPopupWindow popupWindow = new();
            ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

            await popupWindow.ShowDialog(MainWindow.Instance);

            if (popupViewModel != null && popupViewModel.Success)
            {
                StartStopVoiceRecordingShortcut = popupViewModel.CapturedShortcut ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SetPlayLatestVoiceRecordingShortcut()
        {
            ShortcutPopupWindow popupWindow = new();
            ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

            await popupWindow.ShowDialog(MainWindow.Instance);

            if (popupViewModel != null && popupViewModel.Success)
            {
                PlayLatestVoiceRecordingShortcut = popupViewModel.CapturedShortcut ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SetPlayLatestVoiceRecordingPitchedShortcut()
        {
            ShortcutPopupWindow popupWindow = new();
            ShortcutPopupViewModel? popupViewModel = popupWindow.ViewModel;

            await popupWindow.ShowDialog(MainWindow.Instance);

            if (popupViewModel != null && popupViewModel.Success)
            {
                PlayLatestVoiceRecordingPitchedShortcut = popupViewModel.CapturedShortcut ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SaveConfig()
        {
            if (_startingSoundsPath != string.Empty && _startingSoundsPath != SoundsFolder)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Warning", "Changing your sounds folder will remove your previous sounds configuration when you close the application. \nWould you like to continue?", MsBox.Avalonia.Enums.ButtonEnum.YesNo);

                var result = await box.ShowAsync();

                if (result == MsBox.Avalonia.Enums.ButtonResult.No)
                    return;
            }

            ConfigManager.ConfigData.VirtualMicName = SelectedMicrophone;
            ConfigManager.ConfigData.SpeakerName = SelectedSpeaker;

            ConfigManager.ConfigData.SoundsFolder = SoundsFolder;

            ConfigManager.ConfigData.StopAllSoundsShortcut = (string.IsNullOrEmpty(StopAllSoundsShortcut) || StopAllSoundsShortcut == "None") ? string.Empty : StopAllSoundsShortcut;
            ConfigManager.ConfigData.PlayRandomSoundShortcut = (string.IsNullOrEmpty(PlayRandomSoundShortcut) || PlayRandomSoundShortcut == "None") ? string.Empty : PlayRandomSoundShortcut;
            ConfigManager.ConfigData.PlayLatestSoundPitchedShortcut = (string.IsNullOrEmpty(PlayLatestSoundPitchedShortcut) || PlayLatestSoundPitchedShortcut == "None") ? string.Empty : PlayLatestSoundPitchedShortcut;
            ConfigManager.ConfigData.PlayLatestSoundReversedShortcut = (string.IsNullOrEmpty(PlayLatestSoundReversedShortcut) || PlayLatestSoundReversedShortcut == "None") ? string.Empty : PlayLatestSoundReversedShortcut;

            ConfigManager.ConfigData.StartStopVoiceRecordingShortcut = StartStopVoiceRecordingShortcut;
            ConfigManager.ConfigData.PlayLatestVoiceRecordingShortcut = PlayLatestVoiceRecordingShortcut;
            ConfigManager.ConfigData.PlayLatestVoiceRecordingPitchedShortcut = PlayLatestVoiceRecordingPitchedShortcut;
            ConfigManager.ConfigData.RecordVirtualMic = RecordVirtualMic;
            ConfigManager.ConfigData.RecordSpeaker = RecordSpeaker;

            ConfigManager.ConfigData.RandomPitchRangeMin = MinRandomPitchRange;
            ConfigManager.ConfigData.RandomPitchRangeMax = MaxRandomPitchRange;

            ConfigManager.ConfigData.ExitToTray = ExitToTray;

            ConfigWindow.Instance.Close();
        }

        [RelayCommand]
        private static void CancelConfig()
        {
            ConfigWindow.Instance.Close();
        }
    }
}

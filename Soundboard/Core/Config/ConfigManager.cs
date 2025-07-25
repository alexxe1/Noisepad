using Soundboard.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Soundboard.Core.Config
{
    public class ConfigData
    {
        public float GeneralVolume { get; set; } = 0.5f;
        public float GeneralPitch { get; set; } = 1.0f;

        public string SoundsFolder { get; set; } = string.Empty;

        // Devices
        public string VirtualMicName { get; set; } = string.Empty;
        public string SpeakerName { get; set; } = string.Empty;

        // General shortcuts
        public string StopAllSoundsShortcut { get; set; } = string.Empty;
        public string PlayRandomSoundShortcut { get; set; } = string.Empty;
        public string PlayLatestSoundPitchedShortcut { get; set; } = string.Empty;
        public string PlayLatestSoundReversedShortcut { get; set; } = string.Empty;

        // Voice recording
        public string StartStopVoiceRecordingShortcut { get; set; } = string.Empty;
        public string PlayLatestVoiceRecordingShortcut { get; set; } = string.Empty;
        public string PlayLatestVoiceRecordingPitchedShortcut { get; set; } = string.Empty;

        public bool RecordVirtualMic { get; set; } = true;
        public bool RecordSpeaker { get; set; } = true;

        // Random pitch
        public float RandomPitchRangeMin { get; set; } = 0.5f;
        public float RandomPitchRangeMax { get; set; } = 2.0f;

        // Tray
        public bool ExitToTray { get; set; }

        public List<Sound> SoundsData { get; set; } = new();
    }

    public static class ConfigManager
    {
        public static readonly string AppName = "Noisepad";
        public static readonly string AppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

        public static readonly string[] AudioExtensions = [".mp3", ".wav", ".flac", ".aac", ".m4a"];

        public static ConfigData ConfigData { get; private set; } = new();

        public static void LoadConfig()
        {
            string configPath = Path.Combine(AppPath, "config.json");

            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);

                ConfigData = JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
            }

            if (!Directory.Exists(ConfigData.SoundsFolder))
            {
                ConfigData.SoundsFolder = string.Empty;
            }
        }

        public static void SaveConfig(List<Sound> sounds)
        {
            Directory.CreateDirectory(AppPath);

            string configPath = Path.Combine(AppPath, "config.json");

            ConfigData.SoundsData = sounds.FindAll(s =>
                s.EnableVolume ||
                Math.Abs(s.Volume - 0.5f) > 0.001f ||
                s.EnablePitch ||
                Math.Abs(s.Pitch - 1.0f) > 0.001f ||
                s.RandomPitch ||
                s.Reversed ||
                !string.IsNullOrWhiteSpace(s.Shortcut)
            );

            var json = JsonSerializer.Serialize(ConfigData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(configPath, json);
        }
    }
}

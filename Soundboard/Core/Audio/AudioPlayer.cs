using NAudio.CoreAudioApi;
using Soundboard.Core.Audio.External;
using Soundboard.Core.Config;
using Soundboard.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Soundboard.Core.Audio;

public static class AudioPlayer
{
    private static readonly List<PlayingSound> _playingSounds = [];

    public static bool IsPlaying => _playingSounds.Count > 0;

    public static event Action? IsPlayingChanged;

    public static bool Play(string filePath, float volume = 0.5f, float pitch = 1f, bool reverse = false)
    {
        MMDevice? mic = GetVirtualMicDevice();
        int speakersId = GetSpeakersDeviceId();

        if (mic == null && speakersId == -404)
            return false;

        var sound = new PlayingSound(mic, speakersId, filePath, volume, pitch, reverse);

        _playingSounds.Add(sound);

        UiThreadHelper.Run(() => IsPlayingChanged?.Invoke());

        return true;
    }

    public static void StopAll()
    {
        foreach (var sound in _playingSounds.ToList())
        {
            sound.Stop();

            _playingSounds.Clear();

            UiThreadHelper.Run(() => IsPlayingChanged?.Invoke());
        }
    }

    internal static void RemoveSound(PlayingSound sound)
    {
        _playingSounds.Remove(sound);

        UiThreadHelper.Run(() => IsPlayingChanged?.Invoke());
    }

    public static void UpdateSoundVolume(string filePath, float volume)
    {
        foreach (var sound in _playingSounds.Where(s => Path.GetFileName(s.FilePath) == Path.GetFileName(filePath)))
        {
            sound.Volume = volume;
        }
    }

    public static void UpdateSoundPitch(string filePath, float pitch)
    {
        foreach (var sound in _playingSounds.Where(s => Path.GetFileName(s.FilePath) == Path.GetFileName(filePath)))
        {
            sound.Pitch = pitch;
        }
    }

    public static void UpdateAllSoundsVolume(float volume, List<string> excludedSounds)
    {
        foreach (var sound in _playingSounds.Where(s => !excludedSounds.Contains(Path.GetFileName(s.FilePath))))
        {
            sound.Volume = volume;
        }
    }

    public static void UpdateAllSoundsPitch(float pitch, List<string> excludedSounds)
    {
        foreach (var sound in _playingSounds.Where(s => !excludedSounds.Contains(Path.GetFileName(s.FilePath))))
        {
            sound.Pitch = pitch;
        }
    }

    private static MMDevice? GetVirtualMicDevice()
    {
        var enumerator = new MMDeviceEnumerator();

        return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                         .FirstOrDefault(d => d.FriendlyName == ConfigManager.ConfigData.VirtualMicName);
    }

    private static int GetSpeakersDeviceId()
    {
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var capabilities = WaveOut.GetCapabilities(i);

            if (capabilities.ProductName == ConfigManager.ConfigData.SpeakerName)
            {
                return i;
            }
        }

        return -404;
    }

    public static bool IsAudioBeingPlayed(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return _playingSounds.Any(s => Path.GetFileName(s.FilePath) == fileName);
    }

    public static void StopAudio(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var soundsToStop = _playingSounds.Where(s => Path.GetFileName(s.FilePath) == fileName).ToList();

        foreach (var sound in soundsToStop)
        {
            sound.Stop();
            sound.Dispose();
            _playingSounds.Remove(sound);
        }

        if (soundsToStop.Count > 0)
        {
            UiThreadHelper.Run(() => IsPlayingChanged?.Invoke());
        }
    }
}
using Avalonia.Platform;
using NAudio.Wave;
using System;

public class InternalSoundPlayer : IDisposable
{
    private WaveOutEvent? _outputDevice = null!;
    private WaveStream? _waveStream = null!;

    public void Play(string soundName)
    {
        Stop();

        var uri = new Uri($"avares://Soundboard/Assets/Sounds/{soundName}");
        var stream = AssetLoader.Open(uri);

        _waveStream = new WaveFileReader(stream);
        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_waveStream);
        _outputDevice.Play();
    }

    public void Stop()
    {
        _outputDevice?.Stop();
        _waveStream?.Dispose();
        _outputDevice?.Dispose();

        _waveStream = null;
        _outputDevice = null;
    }

    public void Dispose()
    {
        Stop();
    }
}

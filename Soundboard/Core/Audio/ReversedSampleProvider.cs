using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

public class ReverseSampleProvider : ISampleProvider
{
    private readonly float[] _buffer;
    private int _position; 
    public WaveFormat WaveFormat { get; }

    public ReverseSampleProvider(AudioFileReader reader)
    {
        WaveFormat = reader.WaveFormat;

        var wholeBuffer = new float[reader.Length / 4]; // 4 bytes * float (32 bits)
        int samplesRead = reader.Read(wholeBuffer, 0, wholeBuffer.Length);

        _buffer = new float[samplesRead];
        for (int i = 0; i < samplesRead; i++)
        {
            _buffer[i] = wholeBuffer[samplesRead - 1 - i];
        }

        _position = 0;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int available = _buffer.Length - _position;
        int toCopy = Math.Min(count, available);
        Array.Copy(_buffer, _position, buffer, offset, toCopy);
        _position += toCopy;
        return toCopy;
    }
}

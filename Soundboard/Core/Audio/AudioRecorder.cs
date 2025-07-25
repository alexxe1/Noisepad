using Microsoft.VisualBasic;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Soundboard.Core.Config;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Soundboard.Core.Audio;

public class AudioRecorder
{
    private WasapiLoopbackCapture speakerCapture = null!;
    private WasapiLoopbackCapture virtualMicCapture = null!;
    private WaveFileWriter? writer = null!;
    private MixingSampleProvider mixer = null!;

    private BufferedWaveProvider speakerBuffer = null!;
    private BufferedWaveProvider virtualMicBuffer = null!;

    public void StartRecording(string outputPath)
    {
        var enumerator = new MMDeviceEnumerator();
        bool shouldRecordMic = ConfigManager.ConfigData.RecordVirtualMic;
        bool shouldRecordSpeaker = ConfigManager.ConfigData.RecordSpeaker;

        // Obtener dispositivos
        var speakerDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .FirstOrDefault(d => d.FriendlyName.Contains(ConfigManager.ConfigData.SpeakerName, StringComparison.OrdinalIgnoreCase));

        var virtualMicDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .FirstOrDefault(d => d.FriendlyName.Contains(ConfigManager.ConfigData.VirtualMicName, StringComparison.OrdinalIgnoreCase));

        if ((shouldRecordSpeaker && speakerDevice == null) || (shouldRecordMic && virtualMicDevice == null))
            return;

        if (shouldRecordMic && !shouldRecordSpeaker)
            RecordVirtualMic(virtualMicDevice, outputPath);
        else if (shouldRecordSpeaker && !shouldRecordMic)
            RecordSpeaker(speakerDevice, outputPath);
        else if (shouldRecordMic && shouldRecordSpeaker)
            RecordSpeakerAndMic(speakerDevice, virtualMicDevice, outputPath);
    }

    private void RecordVirtualMic(MMDevice? virtualMicDevice, string outputPath)
    {
        virtualMicCapture = new WasapiLoopbackCapture(virtualMicDevice);
        var waveFormat = virtualMicCapture.WaveFormat;

        writer = new WaveFileWriter(outputPath, waveFormat);

        virtualMicCapture.DataAvailable += (s, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            writer.Flush();
        };

        virtualMicCapture.StartRecording();
    }

    private void RecordSpeaker(MMDevice? speakerDevice, string outputPath)
    {
        speakerCapture = new WasapiLoopbackCapture(speakerDevice);
        var waveFormat = speakerCapture.WaveFormat;

        writer = new WaveFileWriter(outputPath, waveFormat);

        speakerCapture.DataAvailable += (s, e) =>
        {
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            writer.Flush();
        };

        speakerCapture.StartRecording();
    }

    private void RecordSpeakerAndMic(MMDevice? speakerDevice, MMDevice? virtualMicDevice, string outputPath)
    {
        speakerCapture = new WasapiLoopbackCapture(speakerDevice);
        virtualMicCapture = new WasapiLoopbackCapture(virtualMicDevice);

        var waveFormat = speakerCapture.WaveFormat;

        if (!waveFormat.Equals(virtualMicCapture.WaveFormat))
            throw new InvalidOperationException("Devices have different audio format!");

        speakerBuffer = new BufferedWaveProvider(waveFormat)
        {
            DiscardOnBufferOverflow = true
        };
        virtualMicBuffer = new BufferedWaveProvider(waveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        mixer = new MixingSampleProvider(new[] {
        speakerBuffer.ToSampleProvider(),
        virtualMicBuffer.ToSampleProvider()});

        writer = new WaveFileWriter(outputPath, waveFormat);

        speakerCapture.DataAvailable += (s, e) => speakerBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        virtualMicCapture.DataAvailable += (s, e) => virtualMicBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);

        speakerCapture.StartRecording();
        virtualMicCapture.StartRecording();

        _ = Task.Run(() =>
        {
            var floatBuffer = new float[waveFormat.SampleRate * waveFormat.Channels / 10]; // 100ms

            int minBufferBytes = waveFormat.AverageBytesPerSecond / 10; // 100ms

            while (speakerCapture.CaptureState == CaptureState.Capturing || virtualMicCapture.CaptureState == CaptureState.Capturing)
            {
                if (speakerBuffer.BufferedBytes >= minBufferBytes || virtualMicBuffer.BufferedBytes >= minBufferBytes)
                {
                    int samplesRead = mixer.Read(floatBuffer, 0, floatBuffer.Length);
                    if (samplesRead > 0)
                    {
                        writer.WriteSamples(floatBuffer, 0, samplesRead);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        });
    }

    public void StopRecording()
    {
        speakerCapture?.StopRecording();
        virtualMicCapture?.StopRecording();

        speakerCapture?.Dispose();
        virtualMicCapture?.Dispose();

        writer?.Flush();
        writer?.Dispose();
        writer = null;
    }
}
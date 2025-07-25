using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace Soundboard.Core.Audio
{
    public class PlayingSound : IDisposable
    {
        private WasapiOut? virtualMicOutputDevice;
        private WaveOutEvent? speakersOutputDevice;

        private AudioFileReader? virtualMicAudioFile;
        private ISampleProvider? virtualMicSampleProvider;

        private AudioFileReader? speakersAudioFile;
        private ISampleProvider? speakersSampleProvider;

        private bool disposed;

        public string FilePath { get; }

        public float Volume
        {
            get => speakersAudioFile?.Volume ?? 1f;
            set
            {
                if (speakersAudioFile != null)
                    speakersAudioFile.Volume = value;

                if (virtualMicAudioFile != null)
                    virtualMicAudioFile.Volume = value;
            }
        }

        public float Pitch
        {
            get => (speakersSampleProvider as SmbPitchShiftingSampleProvider)?.PitchFactor ?? 1f;
            set
            {
                if (speakersSampleProvider is SmbPitchShiftingSampleProvider sp)
                    sp.PitchFactor = value;

                if (virtualMicSampleProvider is SmbPitchShiftingSampleProvider vp)
                    vp.PitchFactor = value;
            }
        }

        public PlayingSound(MMDevice? mic, int speakersId, string filePath, float volume, float pitch, bool reverse = false)
        {
            FilePath = filePath;

            if (mic == null && speakersId == -404)
                return;

            if (mic != null)
                PlayThroughMic(mic, volume, pitch, reverse);

            if (speakersId != -404)
                PlayThroughSpeakers(speakersId, volume, pitch, reverse);

            if (speakersId == -404)
            {
                virtualMicOutputDevice!.PlaybackStopped += OnPlaybackStopped;
            }
            else
            {
                speakersOutputDevice!.PlaybackStopped += OnPlaybackStopped;
            }
        }

        private void PlayThroughMic(MMDevice mic, float volume, float pitch, bool reverse)
        {
            virtualMicAudioFile = new(FilePath)
            {
                Volume = volume
            };

            ISampleProvider sourceProvider = reverse
                ? new ReverseSampleProvider(virtualMicAudioFile)
                : virtualMicAudioFile;

            virtualMicSampleProvider = new SmbPitchShiftingSampleProvider(sourceProvider, 4096, 4, pitch);

            virtualMicOutputDevice = new WasapiOut(mic, AudioClientShareMode.Shared, false, 20);
            virtualMicOutputDevice.Init(new SampleToWaveProvider16(virtualMicSampleProvider));
            virtualMicOutputDevice.Play();
        }

        private void PlayThroughSpeakers(int deviceIndex, float volume, float pitch, bool reverse)
        {
            speakersAudioFile = new(FilePath)
            {
                Volume = volume
            };

            ISampleProvider sourceProvider = reverse
                ? new ReverseSampleProvider(speakersAudioFile)
                : speakersAudioFile;

            speakersSampleProvider = new SmbPitchShiftingSampleProvider(sourceProvider, 4096, 4, pitch);

            speakersOutputDevice = new WaveOutEvent
            {
                DeviceNumber = deviceIndex
            };

            speakersOutputDevice.Init(new SampleToWaveProvider16(speakersSampleProvider));
            speakersOutputDevice.Play();
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs args)
        {
            Dispose();
            AudioPlayer.RemoveSound(this);
        }

        public void Stop()
        {
            virtualMicOutputDevice?.Stop();
            speakersOutputDevice?.Stop();

            Dispose();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            GC.SuppressFinalize(this);

            virtualMicOutputDevice?.Dispose();
            virtualMicAudioFile?.Dispose();

            speakersOutputDevice?.Dispose();
            speakersAudioFile?.Dispose();

            virtualMicOutputDevice = null;
            virtualMicAudioFile = null;
            virtualMicSampleProvider = null;

            speakersOutputDevice = null;
            speakersAudioFile = null;
            speakersSampleProvider = null;
        }
    }
}
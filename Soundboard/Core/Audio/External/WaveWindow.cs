using NAudio.Wave;

namespace Soundboard.Core.Audio.External
{
    class WaveWindowNative
    {
        private WaveInterop.WaveCallback waveCallback;

        public WaveWindowNative(WaveInterop.WaveCallback waveCallback)
        {
            this.waveCallback = waveCallback;
        }
    }

    class WaveWindow
    {
        private WaveInterop.WaveCallback waveCallback;

        public WaveWindow(WaveInterop.WaveCallback waveCallback)
        {
            this.waveCallback = waveCallback;
        }
    }
}

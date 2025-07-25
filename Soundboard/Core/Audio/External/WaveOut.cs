#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NAudio.Wave;
using System.Threading;
using NAudio;

namespace Soundboard.Core.Audio.External
{
    public class WaveOut : IWavePlayer, IWavePosition
    {
        private IntPtr hWaveOut;
        private WaveOutBuffer[]? buffers;
        private IWaveProvider? waveStream;
        private volatile PlaybackState playbackState;
        private readonly WaveInterop.WaveCallback callback;
        private readonly WaveCallbackInfo callbackInfo;
        private readonly object waveOutLock;
        private int queuedBuffers;
        private readonly SynchronizationContext? syncContext;

        public event EventHandler<StoppedEventArgs>? PlaybackStopped;

        public static WaveOutCapabilities GetCapabilities(int devNumber)
        {
            var caps = new WaveOutCapabilities();
            var structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveOutGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveOutGetDevCaps");
            return caps;
        }

        public static int DeviceCount => WaveInterop.waveOutGetNumDevs();

        public int DesiredLatency { get; set; }

        public int NumberOfBuffers { get; set; }

        public int DeviceNumber { get; set; } = -1;

        public WaveOut()
            : this(SynchronizationContext.Current == null ? WaveCallbackInfo.FunctionCallback() : WaveCallbackInfo.NewWindow())
        {
        }

        public WaveOut(IntPtr windowHandle)
            : this(WaveCallbackInfo.ExistingWindow(windowHandle))
        {
        }

        public WaveOut(WaveCallbackInfo callbackInfo)
        {
            DesiredLatency = 300;
            NumberOfBuffers = 2;
            callback = Callback;
            waveOutLock = new object();
            this.callbackInfo = callbackInfo;
            syncContext = SynchronizationContext.Current;
        }

        public void Init(IWaveProvider waveProvider)
        {
            waveStream = waveProvider;
            int bufferSize = waveProvider.WaveFormat.ConvertLatencyToByteSize((DesiredLatency + NumberOfBuffers - 1) / NumberOfBuffers);

            MmResult result;
            lock (waveOutLock)
            {
                result = callbackInfo.WaveOutOpen(out hWaveOut, DeviceNumber, waveStream.WaveFormat, callback);
            }
            MmException.Try(result, "waveOutOpen");

            buffers = new WaveOutBuffer[NumberOfBuffers];
            playbackState = PlaybackState.Stopped;
            for (int n = 0; n < NumberOfBuffers; n++)
            {
                buffers[n] = new WaveOutBuffer(hWaveOut, bufferSize, waveStream, waveOutLock);
            }
        }

        public void Play()
        {
            if (playbackState == PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Playing;
                Debug.Assert(queuedBuffers == 0, "Buffers already queued on play");
                EnqueueBuffers();
            }
            else if (playbackState == PlaybackState.Paused)
            {
                EnqueueBuffers();
                Resume();
                playbackState = PlaybackState.Playing;
            }
        }

        private void EnqueueBuffers()
        {
            if (buffers == null) return;

            for (int n = 0; n < NumberOfBuffers; n++)
            {
                if (!buffers[n].InQueue)
                {
                    if (buffers[n].OnDone())
                    {
                        Interlocked.Increment(ref queuedBuffers);
                    }
                    else
                    {
                        playbackState = PlaybackState.Stopped;
                        break;
                    }
                }
            }
        }

        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
            {
                MmResult result;
                playbackState = PlaybackState.Paused;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutPause(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutPause");
                }
            }
        }

        public void Resume()
        {
            if (playbackState == PlaybackState.Paused)
            {
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutRestart(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutRestart");
                }
                playbackState = PlaybackState.Playing;
            }
        }

        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                MmResult result;
                lock (waveOutLock)
                {
                    result = WaveInterop.waveOutReset(hWaveOut);
                }
                if (result != MmResult.NoError)
                {
                    throw new MmException(result, "waveOutReset");
                }

                if (callbackInfo.Strategy == WaveCallbackStrategy.FunctionCallback)
                {
                    RaisePlaybackStoppedEvent(null);
                }
            }
        }

        public long GetPosition() => WaveOutUtils.GetPositionBytes(hWaveOut, waveOutLock);

        public WaveFormat OutputWaveFormat => waveStream!.WaveFormat;

        public PlaybackState PlaybackState => playbackState;

        public float Volume
        {
            get => WaveOutUtils.GetWaveOutVolume(hWaveOut, waveOutLock);
            set => WaveOutUtils.SetWaveOutVolume(value, hWaveOut, waveOutLock);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            Stop();

            if (disposing && buffers != null)
            {
                for (int n = 0; n < buffers.Length; n++)
                {
                    buffers[n]?.Dispose();
                }

                buffers = null;
            }

            lock (waveOutLock)
            {
                WaveInterop.waveOutClose(hWaveOut);
            }
        }

        ~WaveOut()
        {
            Debug.Assert(false, "WaveOut device was not closed");
            Dispose(false);
        }

        private void Callback(IntPtr hWaveOut, WaveInterop.WaveMessage uMsg, IntPtr dwInstance, WaveHeader wavhdr, IntPtr dwReserved)
        {
            if (uMsg == WaveInterop.WaveMessage.WaveOutDone)
            {
                GCHandle hBuffer = (GCHandle)wavhdr.userData;

                if (hBuffer.Target is not WaveOutBuffer buffer)
                    return;

                Interlocked.Decrement(ref queuedBuffers);
                Exception? exception = null;

                if (PlaybackState == PlaybackState.Playing)
                {
                    lock (waveOutLock)
                    {
                        try
                        {
                            if (buffer.OnDone())
                            {
                                Interlocked.Increment(ref queuedBuffers);
                            }
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                    }
                }

                if (queuedBuffers == 0)
                {
                    if (callbackInfo.Strategy == WaveCallbackStrategy.FunctionCallback && playbackState == PlaybackState.Stopped)
                    {
                        return;
                    }
                    else
                    {
                        playbackState = PlaybackState.Stopped;
                        RaisePlaybackStoppedEvent(exception);
                    }
                }
            }
        }

        private void RaisePlaybackStoppedEvent(Exception? e)
        {
            var handler = PlaybackStopped;
            if (handler != null)
            {
                if (syncContext == null)
                {
                    handler(this, new StoppedEventArgs(e));
                }
                else
                {
                    syncContext.Post(_ => handler(this, new StoppedEventArgs(e)), null);
                }
            }
        }
    }
}

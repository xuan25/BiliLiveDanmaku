using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using System.Xml.Linq;
using Wave;
using Wave.Filters;

namespace Speech
{
    class SpeechUtil
    {
        private static readonly Synthesizer Synthesizer;
        private static readonly List<IWaveFilter> WaveFilters;
        private static readonly VolumeFilter volumeFilter;

        private static WaveOut CurrentWaveOut;
        private static bool IsPlaying;
        private static readonly ManualResetEvent PlayCompletedEvent;

        public static event EventHandler<int> QueueChanged;

        public static bool IsAvalable { 
            get 
            { 
                return Synthesizer != null;
            }
        }

        public static int OutputDeviceId;

        public static float Volume
        {
            get
            {
                if (volumeFilter != null)
                    return volumeFilter.Volume;
                return -1;
            }
            set
            {
                if (volumeFilter != null)
                    volumeFilter.Volume = value;
            }
        }

        public static void Speak(string ssmlDoc)
        {
            Synthesizer.Speak(ssmlDoc);
        }

        public static void ClearQueue()
        {
            if (Synthesizer != null)
                Synthesizer.ClearQueue();
        }

        static SpeechUtil()
        {
            bool ttsEnable = (bool)Application.Current.FindResource("TTSEnable");
            if (!ttsEnable)
                return;

            string endpointUri = (string)Application.Current.FindResource("TTSEndpointUri");
            if (endpointUri == null)
                return;

            string tokenUri = (string)Application.Current.FindResource("TTSTokenUri");
            string key = (string)Application.Current.FindResource("TTSKey");
            AuthClient authenticationClient = null;
            if (tokenUri != null)
            {
                authenticationClient = new AuthClient(tokenUri, key);
            }

            Synthesizer = new Synthesizer(new Uri(endpointUri), Synthesizer.OutputFormats.Raw24Khz16BitMonoPcm, authenticationClient);
            Synthesizer.OnAudioAvailable += Synthesizer_OnAudioAvailable;
            Synthesizer.OnError += Synthesizer_OnError;

            Synthesizer.QueueChanged += Synthesizer_QueueChanged;

            WaveFilters = new List<IWaveFilter>();
            volumeFilter = new VolumeFilter();
            WaveFilters.Add(volumeFilter);

            IsPlaying = false;
            PlayCompletedEvent = new ManualResetEvent(false);
        }

        private static void Synthesizer_QueueChanged(object sender, int e)
        {
            QueueChanged?.Invoke(sender, e);
        }

        private static void Synthesizer_OnError(object sender, Exception e)
        {
            Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
        }

        private static void Synthesizer_OnAudioAvailable(object sender, Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            stream.Dispose();
            memoryStream.Position = 0;

            if (IsPlaying)
            {
                PlayCompletedEvent.WaitOne();
            }
            if (CurrentWaveOut != null)
            {
                // If the interval between two WaveOuts is short, make sure that the previous WaveOut has been disposed.
                CurrentWaveOut.Dispose();
            }

            IsPlaying = true;
            PlayCompletedEvent.Reset();

            CurrentWaveOut = new WaveOut();
            CurrentWaveOut.DeviceNumber = OutputDeviceId;
            CurrentWaveOut.WaveFilters = WaveFilters;
            CurrentWaveOut.PlaybackStopped += (object s, StoppedEventArgs e) =>
            {
                IsPlaying = false;
                PlayCompletedEvent.Set();

                memoryStream.Dispose();
                // This event may occurs in the system callback.
                // Therefore, the resource cannot be released before the callback returns.
                // If you try to release resources in the current thread, it may cause a deadlock.
                Task.Factory.StartNew(() =>
                {
                    CurrentWaveOut.Dispose();
                });
            };
            CurrentWaveOut.Init(memoryStream, new WaveFormat(24000, 16, 1));
            CurrentWaveOut.Play();
        }
    }
}

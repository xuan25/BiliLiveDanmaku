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
        public static readonly Synthesizer Synthesizer;
        private static readonly List<IWaveFilter> WaveFilters;
        private static readonly VolumeFilter volumeFilter;

        private static WaveOut CurrentWaveOut = null;
        private static bool IsPlaying;
        private static readonly ManualResetEvent PlayCompletedEvent;


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
                return volumeFilter.Volume;
            }
            set
            {
                volumeFilter.Volume = value;
            }
        }

        public static void Speak(string ssmlDoc)
        {
            Synthesizer.Speak(ssmlDoc);
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

            WaveFilters = new List<IWaveFilter>();
            volumeFilter = new VolumeFilter();
            WaveFilters.Add(volumeFilter);

            IsPlaying = false;
            PlayCompletedEvent = new ManualResetEvent(false);
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

            if (IsPlaying) { 
                PlayCompletedEvent.WaitOne();
            }
            if (CurrentWaveOut != null)
            {
                CurrentWaveOut.Dispose();
            }
            IsPlaying = true;
            PlayCompletedEvent.Reset();

            CurrentWaveOut = new WaveOut();
            CurrentWaveOut.DeviceNumber = OutputDeviceId;
            CurrentWaveOut.WaveFilters = WaveFilters;
            CurrentWaveOut.PlaybackStopped += (object s, StoppedEventArgs e) =>
            {
                memoryStream.Dispose();
                IsPlaying = false;
                PlayCompletedEvent.Set();
            };
            CurrentWaveOut.Init(memoryStream, new WaveFormat(24000, 16, 1));
            CurrentWaveOut.Play();
        }
    }
}

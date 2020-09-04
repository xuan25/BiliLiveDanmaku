using CognitiveServicesTTS;
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

namespace BiliLiveDanmaku.Speech
{
    class TTSUtil
    {
        public static readonly Synthesizer Synthesizer;
        private static List<IWaveFilter> WaveFilters;
        private static VolumeFilter volumeFilter;


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

        static TTSUtil()
        {
            bool ttsEnable = (bool)Application.Current.FindResource("TTSEnable");
            if (!ttsEnable)
                return;

            string endpointUri = (string)Application.Current.FindResource("TTSEndpointUri");
            if (endpointUri == null)
                return;

            string tokenUri = (string)Application.Current.FindResource("TTSTokenUri");
            string key = (string)Application.Current.FindResource("TTSKey");
            AuthenticationClient authenticationClient = null;
            if (tokenUri != null)
            {
                authenticationClient = new AuthenticationClient(tokenUri, key);
            }

            Synthesizer = new Synthesizer(new Uri(endpointUri), Synthesizer.OutputFormats.Raw24Khz16BitMonoPcm, authenticationClient);
            Synthesizer.OnAudioAvailable += Synthesizer_OnAudioAvailable;
            Synthesizer.OnError += Synthesizer_OnError;

            WaveFilters = new List<IWaveFilter>();
            volumeFilter = new VolumeFilter();
            WaveFilters.Add(volumeFilter);
        }

        private static void Synthesizer_OnError(object sender, Exception e)
        {
            Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
        }

        private static void Synthesizer_OnAudioAvailable(object sender, Stream stream)
        {
            using(MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                stream.Dispose();
                memoryStream.Position = 0;
                using(WaveOut waveOut = new WaveOut())
                {
                    waveOut.DeviceNumber = OutputDeviceId;
                    waveOut.WaveFilters = WaveFilters;
                    ManualResetEvent playCompletedEvent = new ManualResetEvent(false);
                    waveOut.PlaybackStopped += (object s, StoppedEventArgs e) =>
                    {
                        playCompletedEvent.Set();
                    };
                    waveOut.Init(memoryStream, new WaveFormat(24000, 16, 1));
                    waveOut.Play();
                    playCompletedEvent.WaitOne();
                }
            }
        }
    }
}

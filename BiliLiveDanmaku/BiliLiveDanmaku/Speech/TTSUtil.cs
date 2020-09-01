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

namespace BiliLiveDanmaku.Speech
{
    class TTSUtil
    {
        static readonly bool ttsEnable;
        static readonly string endpointUri;
        static readonly Authentication auth = null;
        static readonly Synthesize synthesize = new Synthesize();

        public static bool IsAvalable { 
            get 
            { 
                return ttsEnable && endpointUri != null;
            } 
        }

        public static void Speak(string ssmlDoc)
        {
            SpeakWithVoice(AudioOutputFormat.Riff24Khz16BitMonoPcm, ssmlDoc);
        }

        static TTSUtil()
        {
            ttsEnable = (bool)Application.Current.FindResource("TTSEnable");
            endpointUri = (string)Application.Current.FindResource("TTSEndpointUri");

            string tokenUri = (string)Application.Current.FindResource("TTSTokenUri");
            string key = (string)Application.Current.FindResource("TTSKey");
            if (tokenUri != null)
            {
                auth = new Authentication(tokenUri, key);
            }

            synthesize.OnAudioAvailable += PlayAudio;
            synthesize.OnError += ErrorHandler;
        }

        private static void PlayAudio(object sender, GenericEventArgs<Stream> args)
        {
            Console.WriteLine(args.EventData);
            SoundPlayer player = new SoundPlayer(args.EventData);
            player.PlaySync();
            args.EventData.Dispose();
        }

        private static void ErrorHandler(object sender, GenericEventArgs<Exception> e)
        {
            Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
        }

        private static void SpeakWithVoice(AudioOutputFormat format, string ssmlDoc)
        {
            if (!IsAvalable)
                throw new InvalidOperationException("TTS not avaliable");

            string accessToken = string.Empty;

            if (auth != null)
            {
                try
                {
                    accessToken = auth.GetAccessToken();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed authentication.");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(ex.Message);
                    return;
                }
            }

            synthesize.Speak(CancellationToken.None, new Uri(endpointUri), format, accessToken, ssmlDoc).Wait();
        }
    }
}

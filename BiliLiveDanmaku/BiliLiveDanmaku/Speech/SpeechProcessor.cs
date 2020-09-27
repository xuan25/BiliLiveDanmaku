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
    public class SpeechProcessor
    {
        private Synthesizer Synthesizer;
        private List<IWaveFilter> WaveFilters;
        private VolumeFilter volumeFilter;

        private WaveOut CurrentWaveOut;
        private static bool IsPlaying = false;
        private static ManualResetEvent PlayCompletedEvent = new ManualResetEvent(false);

        public event EventHandler<int> QueueChanged;

        public bool IsAvalable { 
            get 
            { 
                return Synthesizer != null;
            }
        }

        public int OutputDeviceId;

        public float Volume
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

        public void Speak(string ssmlDoc)
        {
            Synthesizer.Speak(ssmlDoc);
        }

        public void ClearQueue()
        {
            if (Synthesizer != null)
                Synthesizer.ClearQueue();
        }

        public void Init(string tokenEndpoint, string ttsEndpoint, string key)
        {
            if (tokenEndpoint == null || ttsEndpoint == null || key == null)
                throw new Exception("Config Error");

            AuthClient authenticationClient = null;
            if (tokenEndpoint != null)
            {
                authenticationClient = new AuthClient(tokenEndpoint, key);
            }

            Synthesizer = new Synthesizer(new Uri(ttsEndpoint), Synthesizer.OutputFormats.Raw24Khz16BitMonoPcm, authenticationClient);
            Synthesizer.AudioAvailabled += Synthesizer_OnAudioAvailable;
            Synthesizer.Failed += Synthesizer_OnError;

            Synthesizer.QueueChanged += Synthesizer_QueueChanged;

            WaveFilters = new List<IWaveFilter>();
            volumeFilter = new VolumeFilter();
            WaveFilters.Add(volumeFilter);

            //IsPlaying = false;
            //PlayCompletedEvent = new ManualResetEvent(false);
        }

        public SpeechProcessor()
        {
            //IsPlaying = false;
        }

        private void Synthesizer_QueueChanged(object sender, int e)
        {
            QueueChanged?.Invoke(sender, e);
        }

        public void OnError(Exception e)
        {
            Synthesizer_OnError(null, e);
        }

        private void Synthesizer_OnError(object sender, Exception e)
        {
            Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
        }

        public void OnAudioAvailable(Stream stream)
        {
            Synthesizer_OnAudioAvailable(null, stream);
        }

        private void Synthesizer_OnAudioAvailable(object sender, Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            try
            {
                stream.CopyTo(memoryStream);
            }
            catch(IOException ex)
            {
                Console.WriteLine(ex);
                return;
            }
            finally
            {
                stream.Dispose();
            }
            
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

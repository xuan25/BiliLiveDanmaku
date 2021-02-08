using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Speech
{
    public class Synthesizer
    {
        static Thread SynthesizeThread;

        class SynthesizeTask
        {
            public bool IsCleared;
            public Synthesizer Handler;
            public string Ssml;

            public SynthesizeTask(Synthesizer handler, string ssml)
            {
                IsCleared = false;
                handler.QueueCount++;
                Handler = handler;
                Ssml = ssml;
            }

            public void Clear()
            {
                lock (this)
                {
                    if (!IsCleared)
                    {
                        IsCleared = true;
                        Handler.QueueCount--;
                    }
                }
            }
        }

        static List<SynthesizeTask> SynthesizeQueue;

        static Synthesizer()
        {
            SynthesizeQueue = new List<SynthesizeTask>();
        }

        Uri EndpointUri;
        string OutputFormat;
        AuthClient AuthorizationClient;

        public event EventHandler<Stream> AudioAvailabled;
        public event EventHandler<Exception> Failed;
        public event EventHandler<int> QueueChanged;

        private void OnAudioAvailabled(Stream stream)
        {
            AudioAvailabled?.Invoke(this, stream);
        }

        private void OnFailed(Exception ex)
        {
            Failed?.Invoke(this, ex);
        }

        private void OnQueueChanged()
        {
            QueueChanged?.Invoke(this, QueueCount);
        }

        public enum OutputFormats
        {
            [Description("raw-8khz-8bit-mono-mulaw")]
            Raw8Khz8BitMonoMULaw,

            [Description("raw-16khz-16bit-mono-pcm")]
            Raw16Khz16BitMonoPcm,

            [Description("riff-8khz-8bit-mono-mulaw")]
            Riff8Khz8BitMonoMULaw,

            [Description("riff-16khz-16bit-mono-pcm")]
            Riff16Khz16BitMonoPcm,

            [Description("ssml-16khz-16bit-mono-silk")]
            Ssml16Khz16BitMonoSilk,

            [Description("raw-16khz-16bit-mono-truesilk")]
            Raw16Khz16BitMonoTrueSilk,

            [Description("ssml-16khz-16bit-mono-tts")]
            Ssml16Khz16BitMonoTts,

            [Description("audio-16khz-128kbitrate-mono-mp3")]
            Audio16Khz128KBitRateMonoMp3,

            [Description("audio-16khz-64kbitrate-mono-mp3")]
            Audio16Khz64KBitRateMonoMp3,

            [Description("audio-16khz-32kbitrate-mono-mp3")]
            Audio16Khz32KBitRateMonoMp3,

            [Description("audio-16khz-16kbps-mono-siren")]
            Audio16Khz16KbpsMonoSiren,

            [Description("riff-16khz-16kbps-mono-siren")]
            Riff16Khz16KbpsMonoSiren,

            [Description("raw-24khz-16bit-mono-truesilk")]
            Raw24Khz16BitMonoTrueSilk,

            [Description("raw-24khz-16bit-mono-pcm")]
            Raw24Khz16BitMonoPcm,

            [Description("riff-24khz-16bit-mono-pcm")]
            Riff24Khz16BitMonoPcm,

            [Description("audio-24khz-48kbitrate-mono-mp3")]
            Audio24Khz48KBitRateMonoMp3,

            [Description("audio-24khz-96kbitrate-mono-mp3")]
            Audio24Khz96KBitRateMonoMp3,

            [Description("audio-24khz-160kbitrate-mono-mp3")]
            Audio24Khz160KBitRateMonoMp3
        }

        public int QueueCount = 0;

        public Synthesizer(Uri endpointUri, OutputFormats outputFormats, AuthClient authorization)
        {
            SynthesizeQueue = new List<SynthesizeTask>();

            EndpointUri = endpointUri;

            DescriptionAttribute[] attributes = (DescriptionAttribute[])outputFormats.GetType().GetField(outputFormats.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            string description = attributes.Length > 0 ? attributes[0].Description : string.Empty;
            OutputFormat = description;

            AuthorizationClient = authorization;
        }

        public void Speak(string ssml)
        {
            SynthesizeQueue.Add(new SynthesizeTask(this, ssml));
            OnQueueChanged();

            if (SynthesizeThread == null)
            {
                SynthesizeThread = new Thread(() =>
                {
                    Stream speechStream = null;
                    while (true)
                    {
                        int i = 0;
                        while (SynthesizeQueue.Count == 0)
                        {
                            Thread.Sleep(10);
                            i++;
                            if (i == 30 * 1000 / 10)
                                break;
                        }
                        if (SynthesizeQueue.Count == 0)
                            break;
                        SynthesizeTask synthesizeTask = SynthesizeQueue[0];
                        speechStream = null;
                        try
                        {
                            speechStream = synthesizeTask.Handler.RequestSpeech(synthesizeTask.Ssml);
                        }
                        catch (Exception ex)
                        {
                            synthesizeTask.Handler.OnFailed(ex);
                        }
                        if (speechStream != null)
                        {
                            synthesizeTask.Handler.OnAudioAvailabled(speechStream);
                        }
                        if (SynthesizeQueue.Count > 0)
                        {
                            SynthesizeQueue.RemoveAt(0);
                            synthesizeTask.Clear();
                            synthesizeTask.Handler.OnQueueChanged();
                        }
                    }
                    SynthesizeThread = null;
                })
                {
                    IsBackground = true,
                    Name = "TTSClient.SynthesizeThread"
                };
                SynthesizeThread.Start();
            }
        }

        public Stream RequestSpeech(string ssmlDoc)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(EndpointUri);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/ssml+xml";
            httpWebRequest.UserAgent = "TTSClient";
            httpWebRequest.Headers.Add("X-Microsoft-OutputFormat", OutputFormat);
            if (AuthorizationClient != null)
            {
                string authorizationToken = AuthorizationClient.Token;
                httpWebRequest.Headers.Add("Authorization", authorizationToken);
            }
            //httpWebRequest.Headers.Add("X-Search-AppId", "07D3234E49CE426DAA29772419F436CA");
            //httpWebRequest.Headers.Add("X-Search-ClientID", "1ECFAE91408841A480F00935DC390960");

            using (Stream stream = httpWebRequest.GetRequestStream())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(ssmlDoc);
                stream.Write(buffer, 0, buffer.Length);
            }

            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Stream responseStream = httpWebResponse.GetResponseStream();
            return responseStream;
        }

        public void ClearQueue()
        {
            for (int i = 0; i < SynthesizeQueue.Count; i++)
            {
                SynthesizeTask synthesizeTask = SynthesizeQueue[i];
                if (synthesizeTask.Handler == this)
                {
                    SynthesizeQueue.RemoveAt(i);
                    synthesizeTask.Clear();
                    i--;
                }
            }
            OnQueueChanged();
        }
    }

}

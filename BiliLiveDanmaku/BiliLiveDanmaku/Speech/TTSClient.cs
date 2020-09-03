//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-Speech-TTS
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CognitiveServicesTTS
{
    /// <summary>
    /// This class demonstrates how to get a valid O-auth token
    /// </summary>
    public class AuthenticationClient
    {

        private const int TokenValidDuration = 9;   //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private string TokenUri { get; set; }
        private string ApiKey { get; set; }
        private string LastToken { get; set; }
        
        private DateTime TokenValidTo { get; set; }

        public string Token
        {
            get
            {
                if (DateTime.Now > TokenValidTo)
                {
                    LastToken = RequestToken(TokenUri, this.ApiKey);
                    TokenValidTo = DateTime.Now.AddMinutes(TokenValidDuration);
                }
                return LastToken;
            }
        }

        public AuthenticationClient(string tokenUri, string apiKey)
        {
            TokenUri = tokenUri;
            ApiKey = apiKey;
            TokenValidTo = new DateTime();
        }

        private string RequestToken(string tokenUri, string apiKey)
        {
            WebRequest webRequest = WebRequest.Create(tokenUri);
            webRequest.Method = "POST";
            webRequest.ContentLength = 0;
            webRequest.Headers["Ocp-Apim-Subscription-Key"] = apiKey;

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using(StreamReader streamReader = new StreamReader(stream))
                    {
                        string token = streamReader.ReadToEnd();
                        return token;
                    }
                }
            }
        }
    }


    public class Synthesizer
    {
        Thread SynthesizeThread;
        Queue<string> SsmlDocQueue;

        Uri EndpointUri;
        string OutputFormat;
        AuthenticationClient AuthorizationClient;

        public event EventHandler<Stream> OnAudioAvailable;
        public event EventHandler<Exception> OnError;
        public event EventHandler<int> QueueChanged;

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

        public Synthesizer(Uri endpointUri, OutputFormats outputFormats, AuthenticationClient authorization)
        {
            SsmlDocQueue = new Queue<string>();

            EndpointUri = endpointUri;

            DescriptionAttribute[] attributes = (DescriptionAttribute[])outputFormats.GetType().GetField(outputFormats.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            string description = attributes.Length > 0 ? attributes[0].Description : string.Empty;
            OutputFormat = description;

            AuthorizationClient = authorization;
        }

        public void Speak(string ssmlDoc)
        {
            SsmlDocQueue.Enqueue(ssmlDoc);
            QueueChanged?.Invoke(this, SsmlDocQueue.Count);

            if (SynthesizeThread == null)
            {
                SynthesizeThread = new Thread(() =>
                {
                    Stream speechStream = null;
                    while (true)
                    {
                        int i = 0;
                        while (SsmlDocQueue.Count == 0)
                        {
                            Thread.Sleep(10);
                            i++;
                            if (i == 30 * 1000 / 10)
                                break;
                        }
                        if (SsmlDocQueue.Count == 0)
                            break;
                        ssmlDoc = SsmlDocQueue.Peek();
                        try
                        {
                            speechStream = RequestSpeech(ssmlDoc);
                        }
                        catch (Exception ex)
                        {
                            OnError?.Invoke(this, ex);
                        }
                        if (speechStream != null)
                        {
                            OnAudioAvailable?.Invoke(this, speechStream);
                        }
                        if (SsmlDocQueue.Count > 0)
                        {
                            SsmlDocQueue.Dequeue();
                            QueueChanged?.Invoke(this, SsmlDocQueue.Count);
                        }
                    }
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
            httpWebRequest.Headers.Add("X-Search-AppId", "07D3234E49CE426DAA29772419F436CA");
            httpWebRequest.Headers.Add("X-Search-ClientID", "1ECFAE91408841A480F00935DC390960");

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
            SsmlDocQueue.Clear();
            QueueChanged?.Invoke(this, SsmlDocQueue.Count);
        }
    }

}
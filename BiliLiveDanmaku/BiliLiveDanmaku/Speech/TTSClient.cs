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
    public class Authentication
    {
        private string AccessUri;
        private string apiKey;
        private string accessToken;
        private Timer accessTokenRenewer;

        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;

        public Authentication(string issueTokenUri, string apiKey)
        {
            this.AccessUri = issueTokenUri;
            this.apiKey = apiKey;

            this.accessToken = HttpPost(issueTokenUri, this.apiKey);

            // renew the token every specfied minutes
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                           this,
                                           TimeSpan.FromMinutes(RefreshTokenDuration),
                                           TimeSpan.FromMilliseconds(-1));
        }

        public string GetAccessToken()
        {
            return this.accessToken;
        }

        private void RenewAccessToken()
        {
            string newAccessToken = HttpPost(AccessUri, this.apiKey);
            //swap the new token with old one
            //Note: the swap is thread unsafe
            this.accessToken = newAccessToken;
            Console.WriteLine(string.Format("Renewed token for user: {0} is: {1}",
                              this.apiKey,
                              this.accessToken));
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private string HttpPost(string accessUri, string apiKey)
        {
            // Prepare OAuth request
            WebRequest webRequest = WebRequest.Create(accessUri);
            webRequest.Method = "POST";
            webRequest.ContentLength = 0;
            webRequest.Headers["Ocp-Apim-Subscription-Key"] = apiKey;

            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream stream = webResponse.GetResponseStream())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] waveBytes = null;
                        int count = 0;
                        do
                        {
                            byte[] buf = new byte[1024];
                            count = stream.Read(buf, 0, 1024);
                            ms.Write(buf, 0, count);
                        } while (stream.CanRead && count > 0);

                        waveBytes = ms.ToArray();

                        return Encoding.UTF8.GetString(waveBytes);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Generic event args
    /// </summary>
    /// <typeparam name="T">Any type T</typeparam>
    public class GenericEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericEventArgs{T}" /> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public GenericEventArgs(T eventData)
        {
            this.EventData = eventData;
        }

        /// <summary>
        /// Gets the event data.
        /// </summary>
        public T EventData { get; private set; }
    }

    /// <summary>
    /// Gender of the voice.
    /// </summary>
    public enum Gender
    {
        Female,
        Male
    }

    /// <summary>
    /// Voice output formats.
    /// </summary>
    public enum AudioOutputFormat
    {
        /// <summary>
        /// raw-8khz-8bit-mono-mulaw request output audio format type.
        /// </summary>
        Raw8Khz8BitMonoMULaw,

        /// <summary>
        /// raw-16khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Raw16Khz16BitMonoPcm,

        /// <summary>
        /// riff-8khz-8bit-mono-mulaw request output audio format type.
        /// </summary>
        Riff8Khz8BitMonoMULaw,

        /// <summary>
        /// riff-16khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Riff16Khz16BitMonoPcm,

        // <summary>
        /// ssml-16khz-16bit-mono-silk request output audio format type.
        /// It is a SSML with audio segment, with audio compressed by SILK codec
        /// </summary>
        Ssml16Khz16BitMonoSilk,

        /// <summary>
        /// raw-16khz-16bit-mono-truesilk request output audio format type.
        /// Audio compressed by SILK codec
        /// </summary>
        Raw16Khz16BitMonoTrueSilk,

        /// <summary>
        /// ssml-16khz-16bit-mono-tts request output audio format type.
        /// It is a SSML with audio segment, and it needs tts engine to play out
        /// </summary>
        Ssml16Khz16BitMonoTts,

        /// <summary>
        /// audio-16khz-128kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio16Khz128KBitRateMonoMp3,

        /// <summary>
        /// audio-16khz-64kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio16Khz64KBitRateMonoMp3,

        /// <summary>
        /// audio-16khz-32kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio16Khz32KBitRateMonoMp3,

        /// <summary>
        /// audio-16khz-16kbps-mono-siren request output audio format type.
        /// </summary>
        Audio16Khz16KbpsMonoSiren,

        /// <summary>
        /// riff-16khz-16kbps-mono-siren request output audio format type.
        /// </summary>
        Riff16Khz16KbpsMonoSiren,

        /// <summary>
        /// raw-24khz-16bit-mono-truesilk request output audio format type.
        /// </summary>
        Raw24Khz16BitMonoTrueSilk,

        /// <summary>
        /// raw-24khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Raw24Khz16BitMonoPcm,

        /// <summary>
        /// riff-24khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Riff24Khz16BitMonoPcm,

        /// <summary>
        /// audio-24khz-48kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio24Khz48KBitRateMonoMp3,

        /// <summary>
        /// audio-24khz-96kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio24Khz96KBitRateMonoMp3,

        /// <summary>
        /// audio-24khz-160kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio24Khz160KBitRateMonoMp3
    }

    /// <summary>
    /// Sample synthesize request
    /// </summary>
    public class Synthesize
    {

        private HttpClient client;
        private HttpClientHandler handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="Synthesize"/> class.
        /// </summary>
        public Synthesize()
        {
            var cookieContainer = new CookieContainer();
            handler = new HttpClientHandler() { CookieContainer = new CookieContainer(), UseProxy = false };
            client = new HttpClient(handler);
        }

        ~Synthesize()
        {
            client.Dispose();
            handler.Dispose();
        }

        /// <summary>
        /// Called when a TTS request has been completed and audio is available.
        /// </summary>
        public event EventHandler<GenericEventArgs<Stream>> OnAudioAvailable;

        /// <summary>
        /// Called when an error has occured. e.g this could be an HTTP error.
        /// </summary>
        public event EventHandler<GenericEventArgs<Exception>> OnError;

        
        private IEnumerable<KeyValuePair<string, string>> GenHeaders(AudioOutputFormat outputFormat, string authorizationToken)
        {
            List<KeyValuePair<string, string>> toReturn = new List<KeyValuePair<string, string>>();
            toReturn.Add(new KeyValuePair<string, string>("Content-Type", "application/ssml+xml"));

            string outputFormatStr;

            switch (outputFormat)
            {
                case AudioOutputFormat.Raw16Khz16BitMonoPcm:
                    outputFormatStr = "raw-16khz-16bit-mono-pcm";
                    break;
                case AudioOutputFormat.Raw8Khz8BitMonoMULaw:
                    outputFormatStr = "raw-8khz-8bit-mono-mulaw";
                    break;
                case AudioOutputFormat.Riff16Khz16BitMonoPcm:
                    outputFormatStr = "riff-16khz-16bit-mono-pcm";
                    break;
                case AudioOutputFormat.Riff8Khz8BitMonoMULaw:
                    outputFormatStr = "riff-8khz-8bit-mono-mulaw";
                    break;
                case AudioOutputFormat.Ssml16Khz16BitMonoSilk:
                    outputFormatStr = "ssml-16khz-16bit-mono-silk";
                    break;
                case AudioOutputFormat.Raw16Khz16BitMonoTrueSilk:
                    outputFormatStr = "raw-16khz-16bit-mono-truesilk";
                    break;
                case AudioOutputFormat.Ssml16Khz16BitMonoTts:
                    outputFormatStr = "ssml-16khz-16bit-mono-tts";
                    break;
                case AudioOutputFormat.Audio16Khz128KBitRateMonoMp3:
                    outputFormatStr = "audio-16khz-128kbitrate-mono-mp3";
                    break;
                case AudioOutputFormat.Audio16Khz64KBitRateMonoMp3:
                    outputFormatStr = "audio-16khz-64kbitrate-mono-mp3";
                    break;
                case AudioOutputFormat.Audio16Khz32KBitRateMonoMp3:
                    outputFormatStr = "audio-16khz-32kbitrate-mono-mp3";
                    break;
                case AudioOutputFormat.Audio16Khz16KbpsMonoSiren:
                    outputFormatStr = "audio-16khz-16kbps-mono-siren";
                    break;
                case AudioOutputFormat.Riff16Khz16KbpsMonoSiren:
                    outputFormatStr = "riff-16khz-16kbps-mono-siren";
                    break;
                case AudioOutputFormat.Raw24Khz16BitMonoPcm:
                    outputFormatStr = "raw-24khz-16bit-mono-pcm";
                    break;
                case AudioOutputFormat.Riff24Khz16BitMonoPcm:
                    outputFormatStr = "riff-24khz-16bit-mono-pcm";
                    break;
                case AudioOutputFormat.Audio24Khz48KBitRateMonoMp3:
                    outputFormatStr = "audio-24khz-48kbitrate-mono-mp3";
                    break;
                case AudioOutputFormat.Audio24Khz96KBitRateMonoMp3:
                    outputFormatStr = "audio-24khz-96kbitrate-mono-mp3";
                    break;
                case AudioOutputFormat.Audio24Khz160KBitRateMonoMp3:
                    outputFormatStr = "audio-24khz-160kbitrate-mono-mp3";
                    break;
                default:
                    outputFormatStr = "riff-16khz-16bit-mono-pcm";
                    break;
            }

            toReturn.Add(new KeyValuePair<string, string>("X-Microsoft-OutputFormat", outputFormatStr));
            // authorization Header
            toReturn.Add(new KeyValuePair<string, string>("Authorization", authorizationToken));
            // Refer to the doc
            toReturn.Add(new KeyValuePair<string, string>("X-Search-AppId", "07D3234E49CE426DAA29772419F436CA"));
            // Refer to the doc
            toReturn.Add(new KeyValuePair<string, string>("X-Search-ClientID", "1ECFAE91408841A480F00935DC390960"));
            // The software originating the request
            toReturn.Add(new KeyValuePair<string, string>("User-Agent", "TTSClient"));

            return toReturn;
        }

        public Task Speak(CancellationToken cancellationToken, Uri requestUri, AudioOutputFormat outputFormat, string authorizationToken, string ssmlDoc)
        {
            client.DefaultRequestHeaders.Clear();
            IEnumerable<KeyValuePair<string, string>> headers = GenHeaders(outputFormat, authorizationToken);
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(ssmlDoc)
            };

            var httpTask = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            Console.WriteLine("Response status code: [{0}]", httpTask.Result.StatusCode);

            var saveTask = httpTask.ContinueWith(
                async (responseMessage, token) =>
                {
                    try
                    {
                        if (responseMessage.IsCompleted && responseMessage.Result != null && responseMessage.Result.IsSuccessStatusCode)
                        {
                            var httpStream = await responseMessage.Result.Content.ReadAsStreamAsync().ConfigureAwait(false);
                            this.AudioAvailable(new GenericEventArgs<Stream>(httpStream));
                        }
                        else
                        {
                            this.Error(new GenericEventArgs<Exception>(new Exception(String.Format("Service returned {0}", responseMessage.Result.StatusCode))));
                        }
                    }
                    catch (Exception e)
                    {
                        this.Error(new GenericEventArgs<Exception>(e.GetBaseException()));
                    }
                    finally
                    {
                        responseMessage.Dispose();
                        request.Dispose();
                    }
                },
                TaskContinuationOptions.AttachedToParent,
                cancellationToken);

            return saveTask;
        }

        /// <summary>
        /// Called when a TTS requst has been successfully completed and audio is available.
        /// </summary>
        private void AudioAvailable(GenericEventArgs<Stream> e)
        {
            EventHandler<GenericEventArgs<Stream>> handler = this.OnAudioAvailable;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Error handler function
        /// </summary>
        /// <param name="e">The exception</param>
        private void Error(GenericEventArgs<Exception> e)
        {
            EventHandler<GenericEventArgs<Exception>> handler = this.OnError;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
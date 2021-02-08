using System;
using System.IO;
using System.Net;

namespace Speech
{
    /// <summary>
    /// This class demonstrates how to get a valid O-auth token
    /// </summary>
    public class AuthClient
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

        public AuthClient(string tokenUri, string apiKey)
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
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        string token = streamReader.ReadToEnd();
                        return token;
                    }
                }
            }
        }
    }

}

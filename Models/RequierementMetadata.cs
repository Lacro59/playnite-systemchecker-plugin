using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SystemChecker.Models
{
    public abstract class RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        internal Game game;
        internal GameRequierements gameRequierements = new GameRequierements
        {
            Minimum = new Requirement(),
            Recommanded = new Requirement()
        };
        internal readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };


        public bool isFind()
        {
            return gameRequierements.Minimum.Os.Count > 0;
        }

        internal string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        internal async Task<string> DonwloadStringData(string Url)
        {
            string result = "";
            var client = new HttpClient();

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(Url),
                Method = HttpMethod.Get
            };

            HttpResponseMessage response = client.SendAsync(request).Result;
            var statusCode = (int)response.StatusCode;

            // We want to handle redirects ourselves so that we can determine the final redirect Location (via header)
            if (statusCode >= 300 && statusCode <= 399)
            {
                var redirectUri = response.Headers.Location;
                if (!redirectUri.IsAbsoluteUri)
                {
                    redirectUri = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + redirectUri);
                }
                logger.Debug(string.Format("SystemChecker - Redirecting to {0}", redirectUri));

                result = await DonwloadStringData(redirectUri.ToString());
            }
            else
            {
                result = await response.Content.ReadAsStringAsync();
            }

            return result;
        }


        public abstract GameRequierements GetRequirements();

        public abstract GameRequierements GetRequirements(string url);
    }
}

using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SystemChecker.Models
{
    public abstract class RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        internal Game _game;
        internal GameRequierements gameRequierements = new GameRequierements
        {
            Minimum = new Requirement(),
            Recommanded = new Requirement()
        };


        public bool IsFind()
        {
            return gameRequierements.Minimum.Os.Count > 0;
        }

        public static string SizeSuffix(Int64 value)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0" + CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator + "0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0} {1}", adjustedSize.ToString("0.0", CultureInfo.CurrentUICulture), SizeSuffixes[mag]);
        }

        internal async Task<string> DownloadStringData(string url)
        {
            string result = string.Empty;
            var client = new HttpClient();

            try
            {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
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

                    result = await DownloadStringData(redirectUri.ToString());
                }
                else
                {
                    result = await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Failed to load from {url}");
            }

            return result;
        }


        public abstract GameRequierements GetRequirements();

        public abstract GameRequierements GetRequirements(string url);
    }
}

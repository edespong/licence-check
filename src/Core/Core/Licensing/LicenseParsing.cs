using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LicenseInspector
{
    /// <summary>
    /// Responsible for finding out a license id based on different data.
    /// </summary>
    public class LicenseParsing
    {
        private readonly string licenceFileDirectory;
        private readonly IDictionary<string, License> knownLicenseUrls = new Dictionary<string, License>();
        private readonly LicenseDetectorExecutable licenseDetector;
        private readonly Config config;

        public LicenseParsing(Config config)
        {
            this.licenceFileDirectory = Path.Combine(config.DiskCache.CacheRoot, "licenseFiles");
            this.licenseDetector = new LicenseDetectorExecutable(config);
            this.config = config;

            if (!File.Exists(config.LicenseInfo))
            {
                return;
            }

            string licensesStr = File.ReadAllText(config.LicenseInfo);
            LicenseInfo[] licenses = JsonConvert.DeserializeObject<LicenseInfo[]>(licensesStr);
            foreach (var license in licenses)
            {
                foreach (var url in license.KnownUrls)
                {
                    this.knownLicenseUrls[Normalize(url)] = new License(license.License);
                }
            }
        }

        /// <summary>
        /// Tries to find the license identifier based on the text in the 
        /// license file.
        /// </summary>
        public async Task<(License?, string)> TryGetLicenseFromLicenseFile(Uri licenseUrl)
        {
            string path = Path.Combine(licenceFileDirectory, Normalize(licenseUrl), "LICENSE");
            if (!DiskCache.TryGetValue(path, this.config.DiskCache.LicenseFiles, out string? licenseText))
            {
                try
                {
                    licenseText = await SharedHttpClient.GetStringAsync(licenseUrl).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    string message = $"Error getting license file from {licenseUrl}: {e.Message}";
                    Log.Warning(message);
                    return (null, message);
                }

                DiskCache.CacheData(path, licenseText);
            }

            return await this.licenseDetector.Detect(Path.GetDirectoryName(path)).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to find the license identifier based on the URL.
        /// </summary>
        /// <remarks>
        /// The list of known URLs for different licenses, e.g.
        /// http://www.apache.org/licenses/LICENSE-2.0.html for Apache-2.0,
        /// can be set and extended via configuration.
        /// </remarks>
        public bool TryGetLicenseFromKnownUrl(Uri licenseUrl, out License license)
        {
            return this.knownLicenseUrls.TryGetValue(Normalize(licenseUrl), out license);
        }

        private static string Normalize(Uri uri)
        {
            string url = uri.ToString().ToLower();
            url = url.Replace("https://", "");
            url = url.Replace("http://", "");
            if (url.StartsWith("www."))
            {
                url = url.Substring(4);
            }

            url = url.Replace("/", "");
            url = url.Replace(":", "_");
            url = url.Replace("?", "_");
            url = url.Replace("=", "_");

            url = url.Replace(".html", "");
            url = url.Replace(".htm", "");
            url = url.Replace(".txt", "");

            return Path.GetFileName(url);
        }
    }
}

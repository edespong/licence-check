using Newtonsoft.Json;
using Serilog;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseInspector
{
    /// <summary>
    /// Wrapper class for the license-detector executable.
    /// </summary>
    internal class LicenseDetectorExecutable
    {
        private readonly double minimumConfidence;

        public LicenseDetectorExecutable(Config config)
        {
            this.minimumConfidence = config.MinimumLicenseConfidenceThreshold;
        }

        /// <summary>
        /// Detects the license in the given directory.
        /// </summary>
        /// <returns>The license id or null, with an optional error message.</returns>
        public async Task<(License?, string)> Detect(string directory)
        {
            const string fullPath = "license-detector.exe";
            string args = $"--format json {directory}";
            var (exitCode, output) = await ProcessHelper.RunProcessAsync(fullPath, args).ConfigureAwait(false);

            var results = JsonConvert.DeserializeObject<LicenseDetectorResult[]>(output);
            if (results.Length > 1)
            {
                Log.Error("Got more than two folders searched when looking for licenses.");
            }

            var result = results.First();
            if (!string.IsNullOrEmpty(result.Error))
            {
                return (null, $"license-detector: {result.Error}");
            }

            if (!result.Matches.Any())
            {
                return (null, "Found no license");
            }

            var bestMatch = result.Matches.OrderByDescending(x => x.Confidence).FirstOrDefault();
            if (bestMatch.Confidence < this.minimumConfidence)
            {
                return (null, $"Best match {bestMatch.License} had too low confidence ({bestMatch.Confidence:f3})");
            }

            return (new License(bestMatch.License), string.Empty);
        }

        /// <summary>
        /// Data structure on the format of the output of license-detector.
        /// </summary>
        private class LicenseDetectorResult
        {
            [JsonProperty]
            public string Project { get; }

            [JsonProperty]
            public string Error { get; }

            [JsonProperty]
            public LicenseDetectorMatch[] Matches { get; }

            public LicenseDetectorResult(string project, string error, LicenseDetectorMatch[] matches)
            {
                Project = project;
                Error = error;
                Matches = matches;
            }
        }

        /// <summary>
        /// Sub-data structure on the format of the output of license-detector.
        /// </summary>
        private class LicenseDetectorMatch
        {
            [JsonProperty]
            public string License { get; }

            [JsonProperty]
            public double Confidence { get; }

            public LicenseDetectorMatch(string license, double confidence)
            {
                License = license;
                Confidence = confidence;
            }
        }
    }
}

using SemVer;
using Serilog;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LicenseInspector.JavaScript
{
    internal class NpmVersionResolver : IPackageVersionResolver
    {
        private static string npmOverviewPathFormatStr = Path.Combine("npmOverview", "{0}");

        private readonly INpm npm;

        public NpmVersionResolver(INpm npm, DiskCacheConfig config)
        {
            this.npm = npm;
            npmOverviewPathFormatStr = Path.Combine(config.CacheRoot, "npmOverview", "{0}");
        }

        public Task<Package?> GetSingleVersion(IPackageRange package)
        {
            return GetSingleVersion(package.Id, package.VersionRange);
        }

        private async Task<Package?> GetSingleVersion(string id, string versionRange)
        {
            versionRange = versionRange.Trim();
            if (versionRange.Contains("||"))
            {
                int i = versionRange.IndexOf("||");
                return await GetSingleVersion(id, versionRange[(i + 2)..^0]);
            }

            if (versionRange.StartsWith("^"))
            {
                string? version1 = await GetFullVersion(id, versionRange.Substring(1));
                if (version1 == null) { return null; }
                return new Package(id, version1);
            }

            if (versionRange.StartsWith("~"))
            {
                string? version2 = await GetFullVersion(id, versionRange.Substring(1));
                if (version2 == null) { return null; }
                return new Package(id, version2);
            }

            if (versionRange.StartsWith(">="))
            {
                string result = versionRange.Substring(2);

                // ">= 1.2.3 < 2"
                int lessIndex = result.IndexOf("<");
                if (lessIndex >= 0)
                {
                    result = result.Substring(0, lessIndex);
                }

                string? version3 = await GetFullVersion(id, result.Trim());
                if (version3 == null) { return null; }
                return new Package(id, version3);
            }

            string? version = await GetFullVersion(id, versionRange);
            if (version == null)
            {
                return null;
            }

            return new Package(id, version);
        }

        private async Task<string?> GetFullVersion(string packageId, string version)
        {
            string filePath = GetPageCachePath(packageId, version);

            Regex r = new Regex(@"(?<major>\d+)(?<minor>\.\d+)?(?<patch>.\d+)?");
            Match m = r.Match(version);
            if (!m.Success && version != "*")
            {
                Log.Warning($"Could not match version {version}. This will likely cause issues down the line.");
                return null;
            }

            if (m.Groups["minor"].Success && m.Groups["patch"].Success)
            {
                // Example: 1.2.3 or 1.2.3-preview1
                return version;
            }

            var packageOverview = await npm.GetPackageOverview(packageId).ContinueWith(CachePackageOverview);
            if (packageOverview == null)
            {
                Log.Warning($"Could not get package overview for {packageId}. Version was: {version}");
                return null;
            }

            var versions = packageOverview.Versions.Select(x => new Version(x.Key));
            var packageVersionRange = new Range(version);
            return versions.LastOrDefault(v => packageVersionRange.IsSatisfied(v))?.ToString();

            NpmPackageOverview? CachePackageOverview(Task<NpmPackageOverview?> i)
            {
                if (i.Result == null)
                {
                    return null;
                }

                DiskCache.Cache(filePath, i.Result);
                return i.Result;
            }
        }

        internal static string GetPageCachePath(string packageId, string version)
        {
            string normalizedVersion = version
                .Replace("*", "all")
                .Replace(">=", "gte")
                .Replace("<", "lt")
                .Replace("||", "or")
                .Replace(" ", "");
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                normalizedVersion = normalizedVersion.Replace(c, '_');
            }

            string filename = $"{packageId}_{normalizedVersion}.json";
            return string.Format(npmOverviewPathFormatStr, filename);
        }
    }
}

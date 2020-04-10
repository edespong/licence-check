using SemVer;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseInspector.JavaScript
{
    internal class NpmVersionResolver : IPackageVersionResolver
    {
        private static string npmOverviewPathFormatStr = Path.Combine("npmOverview", "{0}");

        private readonly INpm npm;

        private readonly DiskCacheItem cachePolicy;

        public NpmVersionResolver(INpm npm, DiskCacheConfig config)
        {
            this.npm = npm;
            npmOverviewPathFormatStr = Path.Combine(config.CacheRoot, "npmOverview", "{0}");
            this.cachePolicy = config.NpmPackages;
        }

        public Task<Package?> GetSingleVersion(IPackageRange package)
        {
            return GetSingleVersion(package.Id, package.VersionRange, package.OriginProject);
        }

        private async Task<Package?> GetSingleVersion(string id, string versionRange, string originProject)
        {
            versionRange = versionRange.Trim();
            string? version = await GetSingleVersion(id, versionRange);
            if (version == null)
            {
                return null;
            }

            return new Package(id, version, originProject);
        }

        private async Task<string?> GetSingleVersion(string packageId, string versionRange)
        {
            NpmPackageOverview? packageOverview = null;
            string filePath = GetPageCachePath(packageId, versionRange);
            if (DiskCache.TryGetValue(filePath, this.cachePolicy, out packageOverview))
            {
                Log.Debug($"Got package overview from {filePath}");
            }
            else
            {
                packageOverview = await npm.GetPackageOverview(packageId).ContinueWith(CachePackageOverview);
                Log.Debug($"Got package overview for {packageId}");
            }

            if (packageOverview == null)
            {
                return null;
            }

            var versions = packageOverview.Versions.Select(x => new Version(x.Key));
            var packageVersionRange = new Range(versionRange);
            string? result = versions.LastOrDefault(v => packageVersionRange.IsSatisfied(v))?.ToString();
            Log.Verbose($"Resolved version for {packageId} {versionRange} -> {result ?? "<null>"}");
            return result;

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
                .Replace(">", "gt")
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

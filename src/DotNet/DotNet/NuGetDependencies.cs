using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseInspector.DotNet
{
    /// <summary>
    /// Responsible for finding dependencies in NuGet packages.
    /// </summary>
    internal class NuGetDependencies
    {
        private readonly string detailsPathFormatStr;
        private readonly string resolvedPathFormatStr;
        private static readonly IEnumerable<PackageDependencyGroup> EmptyDependencyGroups = Enumerable.Empty<PackageDependencyGroup>();
        private static readonly IEnumerable<PackageDependency> EmptyDependencies = Enumerable.Empty<PackageDependency>();

        private static readonly DependencyChainComparer<AnalyzedPackage> DependencyComparer = new DependencyChainComparer<AnalyzedPackage>();

        private readonly InMemoryIndex index;
        private readonly DiskCacheConfig config;
        private readonly IPackageVersionResolver versionResolver;
        private readonly PackagePolicies packagePolicies;
        private readonly Dictionary<string, CatalogPage> pageCache = new Dictionary<string, CatalogPage>();

        private readonly bool ignoreDuplicatePackages;
        private readonly HashSet<string> alreadySeenPackages = new HashSet<string>();

        public NuGetDependencies(InMemoryIndex index, Config config)
        {
            this.index = index;
            this.config = config.DiskCache;
            this.ignoreDuplicatePackages = config.IgnoreDuplicatePackages;
            this.versionResolver = new PackageVersionResolver(index);
            this.detailsPathFormatStr = Path.Combine(this.config.CacheRoot, "packageDetails", "{0}_{1}.json");
            this.resolvedPathFormatStr = Path.Combine(this.config.CacheRoot, "resolvedDependencies", "{0}_{1}.json");
            this.packagePolicies = PackagePolicies.LoadFrom(config.PackagePolicies);
        }

        public Task<IList<DependencyChain<AnalyzedPackage>>> FindPackageDependencies(IEnumerable<IPackageRange> packages)
        {
            return FindPackageDependenciesAux(packages, true);
        }

        public async Task<(PackageDetailsResultEnum, PackageDetails?)> GetPackageDetails(IPackage package)
        {
            if (this.index.TryGetDetails(package, out var indexedResult))
            {
                return (PackageDetailsResultEnum.Success, indexedResult);
            }

            string path = string.Format(this.detailsPathFormatStr, package.Id, package.Version);
            if (DiskCache.TryGetValue(path, this.config.PackageDetails, out PackageDetails? cachedResult))
            {
                this.index.AddPackageDetails(cachedResult);
                return (PackageDetailsResultEnum.Success, cachedResult);
            }

            if (this.packagePolicies.GetLocation(package.Id) != null)
            {
                // Internal package. We will not find it on NuGet
                return (PackageDetailsResultEnum.InternalPackage, null);
            }

            var pagePaths = this.index.GetPagePaths(package.Id);
            var packageMatches = new List<CatalogPackage>();
            bool wasPackageSeen = false;
            foreach (var pagePath in pagePaths)
            {
                if (!this.pageCache.TryGetValue(pagePath, out CatalogPage page))
                {
                    string pageStr = File.ReadAllText(pagePath);
                    page = JsonConvert.DeserializeObject<CatalogPage>(pageStr);
                    this.pageCache.Add(pagePath, page);
                }

                foreach (var item in page.Items)
                {
                    if (item.Id != package.Id)
                    {
                        continue;
                    }

                    wasPackageSeen = true;
                    if (item.Version != package.Version &&
                        !AddingZeroSegmentIsMatch(package.Version, item.Version) &&
                        !RemovingZeroBuildNumberIsMatch(package.Version, item.Version) &&
                        !PrefixMatches(package.Version, item.Version))
                    {
                        continue;
                    }

                    packageMatches.Add(item);
                }
            }

            if (packageMatches.Any())
            {
                var catalogPackage = packageMatches.OrderBy(x => x.Version).First();
                var result = await DownloadPackageDetails(catalogPackage).ContinueWith(CacheDetails);
                return (PackageDetailsResultEnum.Success, result);
            }

            if (wasPackageSeen)
            {
                Log.Error($"No package found with specific version: {package}");
            }
            else
            {
                Log.Error($"No package found with id: {package.Id}");
            }

            return (PackageDetailsResultEnum.NoPackageFound, null);

            PackageDetails CacheDetails(Task<PackageDetails> detailsTask)
            {
                if (!this.config.PackageDetails.DoCache)
                {
                    return detailsTask.Result;
                }

                string cachePath = string.Format(this.detailsPathFormatStr, detailsTask.Result.Id, detailsTask.Result.Version);
                this.index.AddPackageDetails(detailsTask.Result);
                return DiskCache.Cache(cachePath, detailsTask);
            }
        }

        private bool AddingZeroSegmentIsMatch(string version, string versionToMatch)
        {
            return version + ".0" == versionToMatch;
        }

        /// <summary>
        /// Checks if two version match if a build number of 0 is removed from
        /// the first, e.g. parameters 1.2.3.0 and 1.2.3 will return true.
        /// </summary>
        private bool RemovingZeroBuildNumberIsMatch(string version, string versionToMatch)
        {
            return BuildNumberIsZero(version) && versionToMatch == version.Substring(0, version.Length - 2);

            bool BuildNumberIsZero(string v)
            {
                int elements = v.Count(c => c == '.');
                if (elements != 3)
                {
                    return false;
                }

                return v.EndsWith(".0");
            }
        }

        /// <summary>
        /// Checks if two version match when the first is a prefix of the
        /// second, i.e. 1.2.3 will match 1.2.3.64564-beta
        /// </summary>
        private bool PrefixMatches(string version, string versionToMatch)
        {
            return versionToMatch.StartsWith(version);
        }

        private async Task<IList<DependencyChain<AnalyzedPackage>>> FindPackageDependenciesAux(IEnumerable<IPackageRange> packages, bool clearCache)
        {
            if (clearCache)
            {
                this.alreadySeenPackages.Clear();
            }

            var result = new List<DependencyChain<AnalyzedPackage>>();

            foreach (var package in packages)
            {
                if (this.packagePolicies.IgnorePackage(package.Id))
                {
                    continue;
                }

                Package? resolvedPackage = await this.versionResolver.GetSingleVersion(package);
                if (resolvedPackage == null)
                {
                    result.Add(new InvalidDependencyChain(package, $"Could not find a package with version {package.VersionRange}."));
                    alreadySeenPackages.Add(package.ToString());
                    continue;
                }

                if (ignoreDuplicatePackages && alreadySeenPackages.Contains(resolvedPackage.ToString()))
                {
                    continue;
                }

                DependencyChain<AnalyzedPackage> dependencies = await FindPackageDependencies(resolvedPackage).ContinueWith(ds => CacheDependencies(resolvedPackage, ds));

                result.Add(dependencies);

                alreadySeenPackages.Add(resolvedPackage.ToString());
            }

            return result;
        }

        private async Task<DependencyChain<AnalyzedPackage>> FindPackageDependencies(Package package)
        {
            string resolvedDependenciesPath = string.Format(this.resolvedPathFormatStr, package.Id, package.Version);
            if (DiskCache.TryGetValue(resolvedDependenciesPath, this.config.ResolvedDependencies, out DependencyChain<AnalyzedPackage>? cached))
            {
                return cached;
            }

            var (status, packageDetails) = await GetPackageDetails(package);
            if (status == PackageDetailsResultEnum.InternalPackage)
            {
                var analyzedPackage = new AnalyzedPackage(package.Id, package.Version, package.OriginProject, AnalysisState.Ok, string.Empty);
                return new DependencyChain<AnalyzedPackage>(analyzedPackage, DependencyChain<AnalyzedPackage>.EmptyList);
            }
            else if (status != PackageDetailsResultEnum.Success)
            {
                return new InvalidDependencyChain(package, "Could not find any package information.");
            }
            else if (packageDetails == null)
            {
                throw new InvalidOperationException($"Got a null result even though status indicated success: {package}");
            }

            List<DependencyChain<AnalyzedPackage>> result = new List<DependencyChain<AnalyzedPackage>>();
            foreach (var group in packageDetails.DependencyGroups ?? EmptyDependencyGroups)
            {
                var dependencies = await FindPackageDependenciesAux(group.Dependencies ?? EmptyDependencies, false);
                result.AddRange(dependencies);
            }

            var uniqueDependencies = result.Distinct(DependencyComparer).ToList();
            if (uniqueDependencies.Any())
            {
                Log.Debug($"{package}: dependencies - {string.Join(", ", uniqueDependencies.Select(y => y.Package.ToString()))}");
            }
            else
            {
                Log.Debug($"{package}: no dependencies");
            }

            return new DependencyChain<AnalyzedPackage>(new AnalyzedPackage(package), uniqueDependencies);
        }

        private static async Task<PackageDetails> DownloadPackageDetails(CatalogPackage package)
        {
            var pageString = await SharedHttpClient.GetStringAsync(package.Url);
            return JsonConvert.DeserializeObject<PackageDetails>(pageString);
        }

        private DependencyChain<AnalyzedPackage> CacheDependencies(IPackage package, Task<DependencyChain<AnalyzedPackage>> dependencies)
        {
            if (!this.config.PackageDetails.DoCache)
            {
                return dependencies.Result;
            }

            return DiskCache.Cache(string.Format(this.resolvedPathFormatStr, package.Id, package.Version), dependencies);
        }
    }
}

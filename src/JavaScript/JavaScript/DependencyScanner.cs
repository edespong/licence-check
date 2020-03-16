using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

[assembly: InternalsVisibleTo("LicenseInspector.JavaScript.Tests")]

namespace LicenseInspector.JavaScript
{
    internal class DependencyScanner : IDependencyScanner
    {
        private readonly IFileAccess fileSystem;
        private readonly INpm npm;
        private readonly IPackageVersionResolver versionResolver;
        private readonly DiskCacheConfig config;
        private readonly string detailsPathFormatStr;
        private readonly string resolvedPathFormatStr;
        private readonly PackagePolicies packagePolicies;

        public DependencyScanner(INpm npm, IFileAccess fileSystem, PackagePolicies packagePolicies, DiskCacheConfig config)
        {
            this.fileSystem = fileSystem;
            this.npm = npm;
            this.versionResolver = new NpmVersionResolver(npm, config);
            this.packagePolicies = packagePolicies;
            this.config = config;

            this.detailsPathFormatStr = Path.Combine(this.config.CacheRoot, "packageDetails", "{0}_{1}.json");
            this.resolvedPathFormatStr = Path.Combine(this.config.CacheRoot, "resolvedDependencies", "{0}_{1}.json");
        }

        public async Task<IList<DependencyChain<AnalyzedPackage>>> FindPackageDependencies(string root)
        {
            var topLevelDependencies = GetTopLevelDependencies(root);
            return await FindPackageDependencies(topLevelDependencies);
        }

        private IList<PackageRange> GetTopLevelDependencies(string root)
        {
            string[] packageJsons = this.fileSystem.GetFiles(root, "package.json");
            return packageJsons.SelectMany(GetFromPackageJson).ToList();
        }

        private IEnumerable<PackageRange> GetFromPackageJson(string path)
        {
            string text = this.fileSystem.ReadAllText(path);
            var packageJson = JsonConvert.DeserializeObject<PackageJson>(text);
            return packageJson.Dependencies.Select(x => new PackageRange(x.Key, x.Value));
        }

        internal async Task<IList<DependencyChain<AnalyzedPackage>>> FindPackageDependencies(IEnumerable<PackageRange> packages)
        {
            var packagesToResolved = packages
                .Where(p => !this.packagePolicies.IgnorePackage(p.Id))
                .Select(p => (Package: p, Resolved: this.versionResolver.GetSingleVersion(p)))
                .ToList();

            List<DependencyChain<AnalyzedPackage>> result = packagesToResolved
                .Where(tuple => tuple.Resolved == null)
                .Select(tuple => new InvalidDependencyChain(tuple.Package, $"Could not find a package with version {tuple.Package.VersionRange}."))
                .Cast<DependencyChain<AnalyzedPackage>>()
                .ToList();

            var validPackages = packagesToResolved.Select(t => t.Resolved);
            var dependencies = validPackages.Select(async packageTask =>
            {
                var package = await packageTask;
                if (package == null)
                {
                    return null;
                }
                return await FindPackageDependencies(package);
            }).ToArray();

            await Task.WhenAll(dependencies);

            result.AddRange(dependencies.Where(x => x.Result != null).Select(x => x.Result!));

            return result;
        }

        internal async Task<DependencyChain<AnalyzedPackage>> FindPackageDependencies(Package package)
        {
            string resolvedDependenciesPath = string.Format(this.resolvedPathFormatStr, package.Id, package.Version);
            if (DiskCache.TryGetValue(resolvedDependenciesPath, this.config.ResolvedDependencies, out DependencyChain<AnalyzedPackage>? cached))
            {
                return cached;
            }

            string path = string.Format(this.detailsPathFormatStr, package.Id, package.Version);
            NpmPackage? npmPackage;
            if (!DiskCache.TryGetValue(path, this.config.PackageDetails, out npmPackage))
            {
                npmPackage = await this.npm.GetPackage(package).ContinueWith(CachePackage);
            }

            if (npmPackage == null)
            {
                Log.Warning($"Could not find information about {package.Id} {package.Version} from npm");
                return new InvalidDependencyChain(package, "Could not find information from npm");
            }

            var analyzedPackage = new AnalyzedPackage(package);

            var packageDependencies = npmPackage.Dependencies ?? new Dictionary<string, string>();
            var dependencies = await FindPackageDependencies(packageDependencies.Select(x => new PackageRange(x.Key, x.Value)));

            var result = new DependencyChain<AnalyzedPackage>(analyzedPackage, dependencies);
            CacheDependencies(package, result);

            return result;

            NpmPackage? CachePackage(Task<(PackageDetailsResultEnum, NpmPackage?)> package)
            {
                if (!this.config.PackageDetails.DoCache)
                {
                    return package.Result.Item2;
                }

                if (package.Result.Item1 != PackageDetailsResultEnum.Success)
                {
                    return null;
                }

                NpmPackage p = package.Result.Item2!;
                string cachePath = string.Format(this.detailsPathFormatStr, p.Id, p.Version);
                DiskCache.Cache(cachePath, p);
                return p;
            }
        }

        private void CacheDependencies(IPackage package, DependencyChain<AnalyzedPackage> dependencies)
        {
            if (!this.config.ResolvedDependencies.DoCache)
            {
                return;
            }

            if (dependencies == null)
            {
                return;
            }

            DiskCache.Cache(string.Format(this.resolvedPathFormatStr, package.Id, package.Version), dependencies);
        }
    }
}

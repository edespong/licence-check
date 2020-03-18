using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
[assembly: InternalsVisibleTo("LicenseInspector.Core.Tests")]

namespace LicenseInspector
{
    public class LicenseScanner : ILicenseScanner
    {
        private readonly Func<IPackage, Task<PackageDetailsResult>> getDetails;
        private readonly Config config;
        private readonly IExplicitLicenseProvider licenseProvider;
        private readonly LicenseParsing licenseParsing;
        private readonly string licensePathFormatStr;

        public LicenseScanner(Func<IPackage, Task<PackageDetailsResult>> getDetails, IExplicitLicenseProvider licenseProvider, Config config)
        {
            this.getDetails = getDetails;
            this.licenseProvider = licenseProvider;
            this.config = config;
            this.licenseParsing = new LicenseParsing(config);
            this.licensePathFormatStr = Path.Combine(config.DiskCache.CacheRoot, "resolvedLicenses", "{0}_{1}.json");
        }

        public IList<DependencyChain<LicensedPackage>> FindLicenses(ICollection<DependencyChain<AnalyzedPackage>> packages)
        {
            var tasks = packages.Select(GetLicensesAsync).ToArray();
            Task.WaitAll(tasks);
            return tasks.Select(t => t.Result).ToList();
        }

        /// <summary>
        /// Recursively try to find licenses for all packages in a dependency
        /// chain.
        /// </summary>
        public async Task<DependencyChain<LicensedPackage>> GetLicensesAsync(DependencyChain<AnalyzedPackage> chain)
        {
            var rootLicense = await GetLicenseAsync(chain.Package);
            string path = string.Format(this.licensePathFormatStr, chain.Package.Id, chain.Package.Version);
            DiskCache.Cache(path, new ResolvedLicense(rootLicense.License, rootLicense.State, rootLicense.Messages));

            var dependencyLicensesTasks = chain.Dependencies.Select(GetLicensesAsync).ToArray();
            Task.WaitAll(dependencyLicensesTasks);
            var dependencyLicenses = dependencyLicensesTasks.Select(x => x.Result).ToList();
            return new DependencyChain<LicensedPackage>(rootLicense, dependencyLicenses);
        }

        private async Task<LicensedPackage> GetLicenseAsync(AnalyzedPackage package)
        {
            if (this.licenseProvider.TryGetLicense(package.Id, out string explicitLicense))
            {
                return package.With(AnalysisState.Ok, "Given by config.").Attach(new License(explicitLicense));
            }

            if (package.State == AnalysisState.Error)
            {
                return package.Attach(License.NonEvaluated);
            }

            string path = string.Format(this.licensePathFormatStr, package.Id, package.Version);
            if (DiskCache.TryGetValue(path, this.config.DiskCache.ResolvedLicenses, out ResolvedLicense? cachedLicense))
            {
                return new LicensedPackage(package.Id, package.Version, package.OriginProject, cachedLicense.License, cachedLicense.State, cachedLicense.Messages);
            }

            var details = await this.getDetails(package);
            if (details.Status == PackageDetailsResultEnum.InternalPackage)
            {
                return package
                    .With(AnalysisState.Ok, "Internal package. No explicit license given.")
                    .Attach(License.Internal);
            }
            else if (details.Status == PackageDetailsResultEnum.NoPackageFound)
            {
                Log.Error($"Could not find information on package {package}");
                return package
                    .With(AnalysisState.Error, $"Could not find information on package")
                    .Attach(License.NonEvaluated);
            }

            var packageDetails = details.Package!;
            if (packageDetails.License != null)
            {
                return new LicensedPackage(package.Id, package.Version, package.OriginProject, packageDetails.License);
            }

            string deprecated = "https://aka.ms/deprecateLicenseUrl";
            if (packageDetails.LicenseUrl == null || packageDetails.LicenseUrl.ToString() == deprecated)
            {
                Log.Information($"{package} has no valid license URL (will try to fetch from repository)");
                return await GetLicenseFromProject(package, packageDetails.PackageUrl);
            }

            if (this.licenseParsing.TryGetLicenseFromKnownUrl(packageDetails.LicenseUrl, out License? license))
            {
                return package.Attach(license);
            }

            Uri licenseUrl = ChangeGithubLicenseUrl(packageDetails.LicenseUrl);

            string error;
            (license, error) = await this.licenseParsing.TryGetLicenseFromLicenseFile(licenseUrl);
            if (license != null)
            {
                return package.Attach(license);
            }

            Log.Debug($"Could not find license for package {package}: " + error);
            return package.With(AnalysisState.Ok, error ?? string.Empty).Attach(License.UnknownLicense);
        }

        private async Task<LicensedPackage> GetLicenseFromProject(AnalyzedPackage package, Uri? projectUrl)
        {
            var (license, _) = await this.TryGetLicenseFromProject(projectUrl);
            if (license != null)
            {
                return package.Attach(license);
            }

            return package
                .With(AnalysisState.Error, $"Package has no valid license URL and the license could not be guessed.")
                .Attach(License.NonEvaluated);
        }

        /// <summary>
        /// Tries to find the license id from the project itself.
        /// </summary>
        private async Task<(License?, string)> TryGetLicenseFromProject(Uri? projectUrl)
        {
            // A random sample of packages shows that many are missing a license
            // URL, but having a github project URL shows that around 20% will
            // have a LICENSE file.
            if (projectUrl == null)
            {
                return (null, string.Empty);
            }

            var licenseUrls = GetPotentialLicenseUrls(projectUrl);
            if (!licenseUrls.Any())
            {
                return (null, string.Empty);
            }

            Uri? validUri = null;
            foreach (var licenseUrl in licenseUrls)
            {
                try
                {
                    var _ = await SharedHttpClient.GetStringAsync(licenseUrl);
                    validUri = licenseUrl;
                }
                catch
                {
                    // Expected. Only one in five will statistically have a license
                    // at the guessed URL.
                }
            }

            if (validUri == null)
            {
                return (null, string.Empty);
            }

            return await this.licenseParsing.TryGetLicenseFromLicenseFile(validUri);
        }

        internal static IEnumerable<Uri> GetPotentialLicenseUrls(Uri projectUrl)
        {
            var uri = projectUrl.AbsoluteUri;
            if (!uri.Contains("github.com"))
            {
                return Enumerable.Empty<Uri>();
            }

            if (uri.StartsWith("git://"))
            {
                uri = uri.Replace("git://", "https://");
            }
            else if (uri.StartsWith("git+"))
            {
                uri = uri.Replace("git+", string.Empty);
            }

            if (uri.EndsWith(".git"))
            {
                uri = uri[0..^4];
            }

            if (!uri.EndsWith("/"))
            {
                uri += "/";
            }

            uri = uri.Replace("github.com", "raw.githubusercontent.com");

            // Final url should be like: https://raw.githubusercontent.com/USER/PROJECT/master/LICENSE.md
            return new[] {
                new Uri(uri + "master/LICENSE"),
                new Uri(uri + "master/LICENSE.md"),
                new Uri(uri + "master/License.md")
            };
        }

        private static Uri ChangeGithubLicenseUrl(Uri licenseUrl)
        {
            var url = licenseUrl.AbsoluteUri;
            if (!url.Contains("github.com"))
            {
                return licenseUrl;
            }

            // Do not change if on formats:
            // https://github.com/USER/PROJECT/raw/master/LICENSE
            // https://raw.githubusercontent.com/USER/PROJECT/master/LICENSE
            if (url.Contains("raw.githubusercontent.com") || url.Contains("/raw/"))
            {
                return licenseUrl;
            }

            if (url.Contains("raw.github.com"))
            {
                // Use new subdomain instead of old, redirecting, one.
                return new Uri(url.Replace("raw.github.com", "raw.githubusercontent.com"));
            }

            if (url.Contains("/blob/"))
            {
                return new Uri(url.Replace("/blob/", "/raw/"));
            }

            Log.Debug($"Got unknown github.com liense url format: {url}");
            return new Uri(url);
        }
    }

    /// <summary>
    /// Used for caching purposes.
    /// </summary>
    internal class ResolvedLicense
    {
        public string License { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AnalysisState State { get; }

        public IList<string> Messages { get; }

        [JsonConstructor]
        public ResolvedLicense(string license, AnalysisState state, IList<string> messages)
        {
            License = license;
            State = state;
            Messages = messages;
        }
    }
}

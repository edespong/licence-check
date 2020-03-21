using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LicenseInspector.Core.Tests
{
    public class LicenseScannerTests
    {
        [Fact]
        public void NoPackageFound_HasValidPackagePolicy_GivesCorrectLicense()
        {
            var config = new Config();
            var packagePolicies = new PackagePolicies(new[] { new PackagePolicy { Package = "test-id", License = "test-license" } });
            var scanner = new LicenseScanner(_ => Task.FromResult(new PackageDetailsResult(PackageDetailsResultEnum.NoPackageFound)), packagePolicies, config);

            var package = new AnalyzedPackage("test-id", "1.0.4", string.Empty, AnalysisState.Error, "test error");
            var dependencies = DependencyChain<AnalyzedPackage>.EmptyList;
            var packages = new[] { new DependencyChain<AnalyzedPackage>(package, dependencies) };

            var result = scanner.FindLicenses(packages);

            Assert.Equal(1, result.Count);
            Assert.Equal("test-license", result.First().Package.License);
        }

        [Fact]
        public void LicenseDefaultNotAllowed_InternalProject_NoViolation()
        {
            var config = new Config { DiskCache = new DiskCacheConfig { ResolvedLicenses = new DiskCacheItem { DoCache = false } } };
            var packagePolicies = new PackagePolicies(new PackagePolicy[] { });
            var licensePolicies = new[] { new LicensePolicy { License = "no-distribution-allowed", Allow = false, AllowInternal = true } };

            var pd = new PackageDetails
            {
                Id = "test-id",
                Version = "1.0.4",
                License = "no-distribution-allowed",
            };

            var package = new AnalyzedPackage(pd.Id, pd.Version, "path-to-origin-project");
            var dependencies = DependencyChain<AnalyzedPackage>.EmptyList;
            var packages = new[] { new DependencyChain<AnalyzedPackage>(package, dependencies) };

            var scanner = new LicenseScanner(_ => Task.FromResult(new PackageDetailsResult(pd)), packagePolicies, config);
            var licensedDependencies = scanner.FindLicenses(packages);

            LicensePolicies licensing = new LicensePolicies(licensePolicies, packagePolicies, new Projects(new[] { "path-to-origin-project" }));
            var result = licensing.Apply(licensedDependencies);

            Assert.Equal(1, result.Count);
            Assert.Equal(Evaluation.Ok, result.First().Package.Result);
        }

        private class PackageDetails : IPackageDetails
        {
            public string Id { get; set; }

            public string Version { get; set; }

            public string? License { get; set; }

            public Uri? PackageUrl { get; set; }

            public Uri? LicenseUrl { get; set; }
        }
    }
}

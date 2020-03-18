using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LicenseInspector.Core.Tests
{
    public class LicenseScannerTests
    {
        [Fact]
        public void x()
        {
            var config = new Config();
            var packagePolicies = new PackagePolicies(new[] { new PackagePolicy { Package = "test-id", License = "test-license" } });
            var scanner = new LicenseScanner(p => Task.FromResult(new PackageDetailsResult(PackageDetailsResultEnum.NoPackageFound)), packagePolicies, config);

            var package = new AnalyzedPackage("test-id", "1.0.4", string.Empty, AnalysisState.Error, "test error");
            var dependencies = DependencyChain<AnalyzedPackage>.EmptyList;
            var packages = new[] { new DependencyChain<AnalyzedPackage>(package, dependencies) };

            var result = scanner.FindLicenses(packages);

            Assert.Equal(1, result.Count);
            Assert.Equal("test-license", result.First().Package.License);
        }
    }
}

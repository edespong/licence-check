using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace LicenseInspector.DotNet.Tests
{
    public class PackageVersionResolverTests
    {
        private static readonly MockVersionProvider emptyProvider =
            new MockVersionProvider(Enumerable.Empty<string>());

        [Fact]
        public void ExactVersion_ResolvesCorrectVersion()
        {
            var resolver = new PackageVersionResolver(emptyProvider);
            var result = resolver.GetSingleVersion(new PackageRange("t", "[1.0]", "")).Result;

            Assert.Equal("1.0", result.Version);
        }

        [Fact]
        public void ExactVersionStrangeFormatting_ResolvesCorrectVersion()
        {
            var resolver = new PackageVersionResolver(emptyProvider);
            var result = resolver.GetSingleVersion(new PackageRange("t", "[ 1.0 ]", "")).Result;

            Assert.Equal("1.0", result.Version);
        }

        [Fact]
        public void ExclusiveRange_ResolvesCorrectVersion()
        {
            var provider = new MockVersionProvider(new[] { "1.0.0", "1.1.0", "2.0.0" });
            var resolver = new PackageVersionResolver(provider);
            var result = resolver.GetSingleVersion(new PackageRange("t", "(1.0.0,2.0.0)", "")).Result;

            Assert.True(result.Version == "1.1.0" || result.Version == "2.0.0");
        }

        private class MockVersionProvider : IPackageVersionProvider
        {
            private readonly IEnumerable<string> versions;

            public MockVersionProvider(IEnumerable<string> versions)
            {
                this.versions = versions;
            }

            public IEnumerable<string> GetVersions(string packageId)
            {
                return this.versions;
            }
        }
    }
}

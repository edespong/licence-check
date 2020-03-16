using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace LicenseInspector.JavaScript.Tests
{
    public class NpmVersionResolverTests
    {
        private readonly INpm npm = new FakeNpm();
        private readonly DiskCacheConfig config = new DiskCacheConfig();

        [Fact]
        public void FullVersion_CorrectResult()
        {
            var resolver = new NpmVersionResolver(npm, config);
            const string version = "1.2.3";
            const string expected = "1.2.3";

            var result = resolver.GetSingleVersion(new PackageRange("test", version)).Result;

            Assert.Equal(expected, result.Version);
        }

        // Versions not resolved:
        // 
        // *

        [Fact]
        public void NoMinorVersion_CorrectResult()
        {
            var resolver = new NpmVersionResolver(npm, config);
            const string version = "1";
            const string expected = "1.9.1";

            var result = resolver.GetSingleVersion(new PackageRange("test", version)).Result;

            Assert.Equal(expected, result.Version);
        }

        [Fact]
        public void OrVersions_CorrectResult()
        {
            var resolver = new NpmVersionResolver(npm, config);
            const string version = "0.2.1 || ^1.9";
            const string expected = "1.9.1";

            var result = resolver.GetSingleVersion(new PackageRange("test", version)).Result;

            Assert.Equal(expected, result.Version);
        }

        [Fact]
        public void NoPatchVersion_CorrectResult()
        {
            var resolver = new NpmVersionResolver(npm, config);
            const string version = "1.9";
            const string expected = "1.9.1";

            var result = resolver.GetSingleVersion(new PackageRange("test", version)).Result;

            Assert.Equal(expected, result.Version);
        }

        [Fact]
        public void VersionWithSuffix_CorrectResult()
        {
            var resolver = new NpmVersionResolver(npm, config);
            const string version = "1.0.0-preview2-final";
            const string expected = "1.0.0-preview2-final";

            var result = resolver.GetSingleVersion(new PackageRange("test", version)).Result;

            Assert.Equal(expected, result.Version);
        }

        private class FakeNpm : INpm
        {
            public Task<(PackageDetailsResultEnum, NpmPackage)> GetPackage(IPackage package)
            {
                return Task.FromResult((PackageDetailsResultEnum.NoPackageFound, (NpmPackage)null));
            }

            public Task<NpmPackageOverview> GetPackageOverview(string packageId)
            {
                if (packageId == "test")
                {
                    var overview = new NpmPackageOverview
                    {
                        Id = packageId,
                        Versions = new Dictionary<string, object> {
                            { "1.1.0", new object() },
                            { "1.2.4", new object() },
                            { "1.9.1", new object() }
                        }
                    };

                    return Task.FromResult(overview);
                }

                throw new ArgumentException();
            }
        }
    }
}

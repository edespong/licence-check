using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace LicenseInspector.JavaScript.Tests
{
    public class DependencyScannerTests
    {
        [Fact]
        public void TypeLicense_InvalidRepositoryFormat_CorrectResult()
        {
            var config = NoCacheConfig();

            var packagePolicies = new PackagePolicies(new PackagePolicy[] { });

            var scanner = new DependencyScanner(new FakeNpm(), new FakeFileSystem(), packagePolicies, config.DiskCache);
            var x = scanner.FindPackageDependencies(new[] { new PackageRange("test-package", "^1.0.0") }).Result;

            Assert.Equal(1, x[0].Dependencies.Count);
        }

        private class FakeFileSystem : IFileAccess
        {
            public bool FileExists(string path)
            {
                throw new NotImplementedException();
            }

            public string[] GetFiles(string path, string searchPattern)
            {
                throw new NotImplementedException();
            }

            public string ReadAllText(string path)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeNpm : INpm
        {
            public Task<(PackageDetailsResultEnum, NpmPackage)> GetPackage(IPackage package)
            {
                return Task.FromResult((PackageDetailsResultEnum.Success, GetPackageAux(package)));
            }

            public NpmPackage GetPackageAux(IPackage package)
            {
                if (package.Id == "test-package")
                {
                    return new NpmPackage
                    {
                        Id = "test-package",
                        License = "MIT",
                        Dependencies = new Dictionary<string, string> { { "test-dep", ">=1.2.3 < 2" } },
                        Version = "1.0.0"
                    };
                }
                else if (package.Id == "test-dep")
                {
                    return new NpmPackage
                    {
                        Id = "test-package",
                        License = "BSD",
                        Dependencies = new Dictionary<string, string> { },
                        Version = "1.2.3"
                    };
                }
                else
                {
                    throw new ArgumentException($"Unknown package: {package.Id}");
                }
            }

            public Task<NpmPackageOverview> GetPackageOverview(string packageId)
            {
                throw new NotImplementedException();
            }
        }

        private static Config NoCacheConfig()
        {
            return new Config
            {
                PackagePolicies = null,
                LicensePolicies = null,
                LicenseInfo = null,
                IgnoreDuplicatePackages = true,
                MinimumLicenseConfidenceThreshold = 1.0,
                DiskCache = new DiskCacheConfig
                {
                    CacheRoot = string.Empty,
                    NuGetCatalogIndex = DiskCacheItem.NoCache,
                    NuGetIndex = DiskCacheItem.NoCache,
                    PackageDetails = DiskCacheItem.NoCache,
                    ResolvedDependencies = DiskCacheItem.NoCache,
                    LicenseFiles = DiskCacheItem.NoCache,
                    ResolvedLicenses = DiskCacheItem.NoCache
                }
            };
        }
    }
}

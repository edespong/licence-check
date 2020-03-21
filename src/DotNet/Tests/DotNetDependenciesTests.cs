using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace LicenseInspector.DotNet.Tests
{
    public class DotNetDependenciesTest : IDisposable
    {
        private string packagePoliciesPath;
        private string licensePoliciesPath;

        private void SetUp()
        {
            this.packagePoliciesPath = Path.GetTempFileName();
            this.licensePoliciesPath = Path.GetTempFileName();
            File.Create(packagePoliciesPath).Dispose();
            File.Create(licensePoliciesPath).Dispose();
            File.WriteAllText(this.packagePoliciesPath, "[]");
            File.WriteAllText(this.licensePoliciesPath, "[]");
        }

        public void Dispose()
        {
            File.Delete(packagePoliciesPath);
            File.Delete(licensePoliciesPath);
        }

        [Fact]
        public async void Test1()
        {
            SetUp();
            var config = NoCacheConfig(this.packagePoliciesPath, this.licensePoliciesPath);

            var l1p1 = GetDetails("Other1.Level1", "1.0");
            var l2p0 = GetDetails("Other0.Level2", "1.0");
            var l1p0 = GetDetails("Other0.Level1", "1.0", l2p0);
            var root = GetDetails("MyCompany.Level0", "1.0", l1p0, l1p1);

            var packages = new[]
            {
                new CatalogPackage("MyCompany.Level0", "1.0"),
                new CatalogPackage("Other0.Level1", "1.0"),
                new CatalogPackage("Other1.Level1", "1.0"),
                new CatalogPackage("Other0.Level2", "1.0"),
            };

            var page = new CatalogPage()
            {
                Url = "https://www.test.org/v3/catalog0/page8163.json",
                Items = packages
            };

            var index = new InMemoryIndex();
            index.Add(page);
            index.AddPackageDetails(root);
            index.AddPackageDetails(l1p0);
            index.AddPackageDetails(l1p1);
            index.AddPackageDetails(l2p0);

            var policies = new PackagePolicies(new List<PackagePolicy>());
            var scanner = new DependencyScanner(index, config, FileAccess.GetAccessor(), policies);
            var topLevelPackages = new[]
            {
                new PackageRange("MyCompany.Level0", "[1.0]", "")
            };

            var dependencies = await scanner.FindPackageDependencies(topLevelPackages, new string[] { });

            Assert.Single(dependencies);
            Assert.True(PackagesEqual(root, dependencies[0].Package));
        }

        private static bool PackagesEqual(IPackage expected, IPackage actual)
        {
            return expected.Id.Equals(actual.Id) && expected.Version.Equals(actual.Version);
        }

        private static Config NoCacheConfig(string packagePoliciesPath, string licensePoliciesPath)
        {
            return new Config
            {
                PackagePolicies = packagePoliciesPath,
                LicensePolicies = licensePoliciesPath,
                LicenseInfo = null,
                ProjectsInfo = null,
                IgnoreDuplicatePackages = true,
                FollowLocations = false,
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

        private static PackageDetails GetDetails(string id, string version)
        {
            return GetDetails(id, version, new PackageDetails[] { });
        }

        private static PackageDetails GetDetails(string id, string version, params PackageDetails[] packages)
        {
            return new PackageDetails
            {
                Id = id,
                OriginalVersion = version,
                DependencyGroups = CreateGroup(packages)
            };
        }

        private static List<PackageDependencyGroup> CreateGroup(params PackageDetails[] packages)
        {
            var dependencies = packages.Select(p => new PackageDependency
            {
                Id = p.Id,
                VersionRange = $"[{p.Version}]",
                Url = p.Url
            }).ToList();

            return new List<PackageDependencyGroup>
                {
                    new PackageDependencyGroup
                    {
                        Dependencies = dependencies
                    }
            };
        }
    }

    public class NullExplicitLiceseProvider : IExplicitLicenseProvider
    {
        public bool TryGetLicense(string packageId, out string license)
        {
            license = null;
            return false;
        }
    }
}

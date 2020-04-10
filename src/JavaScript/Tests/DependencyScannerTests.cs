using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LicenseInspector.JavaScript.Tests
{
    public class DependencyScannerTests
    {
        [Fact]
        public void TypeLicense_InvalidRepositoryFormat_CorrectResult()
        {
            var config = Config.WithoutCache(new Config());

            var packagePolicies = new PackagePolicies(new PackagePolicy[] { });

            var scanner = new DependencyScanner(new FakeNpm(), new FakeFileSystem(), packagePolicies, config.DiskCache);
            var x = scanner.FindPackageDependencies(new[] { new PackageRange("test-package", "^1.0.0", "") }, new HashSet<string>()).Result;

            Assert.Equal(1, x[0].Dependencies.Count);
        }

        /// <summary>
        /// When a cyclic dependency is found, it simply returns no dependencies.
        /// </summary>
        [Fact]
        public void CyclincDependency_IsIgnored()
        {
            var config = Config.WithoutCache(new Config());

            var packagePolicies = new PackagePolicies(new PackagePolicy[] { });

            var scanner = new DependencyScanner(new FakeNpm(), new FakeFileSystem(), packagePolicies, config.DiskCache);
            var x = scanner.FindPackageDependencies(new[] { new PackageRange("test-package", "^2.0.0", "") }, new HashSet<string>()).Result;

            Assert.Equal(1, x[0].Dependencies.Count);
            Assert.Equal(0, x[0].Dependencies.First().Dependencies.Count);
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
            private readonly Dictionary<(string, string), NpmPackage> packages = new Dictionary<(string, string), NpmPackage>
            {
                { ("test-package", "1.0.0"), new NpmPackage
                        {
                            Id = "test-package",
                            License = "MIT",
                            Dependencies = new Dictionary<string, string> { { "test-dep", ">=1.2.3 < 2" } },
                            Version = "1.0.0"
                        }
                },
                { ("test-package", "2.0.0"), new NpmPackage
                        {
                            Id = "test-package",
                            License = "MIT",
                            Dependencies = new Dictionary<string, string> { { "test-dep", "^3.0.0" } },
                            Version = "1.0.0"
                        }
                },
                { ("test-dep", "1.2.3"), new NpmPackage
                        {
                            Id = "test-dep",
                            License = "MIT",
                            Dependencies = new Dictionary<string, string>(),
                            Version = "1.2.3"
                        }
                },
                { ("test-dep", "3.0.0"), new NpmPackage
                        {
                            Id = "test-dep",
                            License = "MIT",
                            Dependencies = new Dictionary<string, string> { { "test-package", "^2.0.0" } },
                            Version = "3.0.0"
                        }
                }
            };

            public Task<(PackageDetailsResultEnum, NpmPackage)> GetPackage(IPackage package)
            {
                return Task.FromResult((PackageDetailsResultEnum.Success, GetPackageAux(package)));
            }

            public NpmPackage GetPackageAux(IPackage package)
            {
                if (!packages.ContainsKey((package.Id, package.Version)))
                {
                    throw new ArgumentException($"Unknown package: {package.Id} {package.Version}");
                }

                return packages[(package.Id, package.Version)];
            }

            public Task<NpmPackageOverview> GetPackageOverview(string packageId)
            {
                if (packageId == "test-package")
                {
                    return Task.FromResult(new NpmPackageOverview
                    {
                        Id = packageId,
                        Versions = new Dictionary<string, object>
                        {
                        { "1.0.0", null },
                        { "2.0.0", null },
                        }
                    });
                }
                else if (packageId == "test-dep")
                {
                    return Task.FromResult(new NpmPackageOverview
                    {
                        Id = packageId,
                        Versions = new Dictionary<string, object>
                        {
                        { "1.2.3", null },
                        { "3.0.0", null },
                        }
                    });
                }
                else
                {
                    throw new NotImplementedException(packageId);
                }

            }
        }
    }
}

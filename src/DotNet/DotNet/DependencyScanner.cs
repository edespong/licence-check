using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Serilog;

namespace LicenseInspector.DotNet
{
    internal class DependencyScanner : IDependencyScanner
    {
        private readonly IPackageVersionResolver versionResolver;
        private readonly NuGetDependencies registry;
        private readonly IFileAccess fileSystem;
        private readonly PackagePolicies packagePolicies;

        public async static Task<DependencyScanner> Create(Config config, IFileAccess fileSystem, PackagePolicies packagePolicies)
        {
            var catalog = new NuGetCatalog(config.DiskCache);
            var index = await catalog.GetIndex();
            return new DependencyScanner(index, config, fileSystem, packagePolicies);
        }

        internal DependencyScanner(InMemoryIndex index, Config config, IFileAccess fileSystem, PackagePolicies packagePolicies)
        {
            this.versionResolver = new PackageVersionResolver(index);
            this.registry = new NuGetDependencies(index, config);
            this.fileSystem = fileSystem;
            this.packagePolicies = packagePolicies;
        }

        public async Task<IList<DependencyChain<AnalyzedPackage>>> FindPackageDependencies(string root)
        {
            var topLevelDependencies = GetTopLevelDependencies(root);

            // Filter out any package where the version is set by the build system etc.
            List<DependencyChain<AnalyzedPackage>> result = topLevelDependencies
                .Where(d => d.VersionRange.Contains("$"))
                .Where(d => !packagePolicies.IgnorePackage(d.Id))
                .Select(d => new InvalidDependencyChain(d, "Version set by variable is not supported."))
                .Cast<DependencyChain<AnalyzedPackage>>()
                .ToList();

            if (result.Any())
            {
                Log.Warning($"There are {result.Count} dependencies (of {topLevelDependencies.Count}) with version set by a variable. This is currently not supported. Will continue with others..");
            }

            // The real work is done here.
            var dependencies = await FindPackageDependencies(topLevelDependencies.Where(d => !d.VersionRange.Contains("$")));
            result.AddRange(dependencies);

            return result;
        }

        internal async Task<IList<DependencyChain<AnalyzedPackage>>> FindPackageDependencies(IEnumerable<PackageRange> packages)
        {
            var packagesToResolved = packages
                .Select(p => (Package: p, Resolved: this.versionResolver.GetSingleVersion(p)))
                .ToList();

            List<DependencyChain<AnalyzedPackage>> result = packagesToResolved
                .Where(tuple => tuple.Resolved == null)
                .Select(tuple => new InvalidDependencyChain(tuple.Package, $"Could not find a package with version {tuple.Package.VersionRange}."))
                .Cast<DependencyChain<AnalyzedPackage>>()
                .ToList();

            var validPackages = packagesToResolved.Select(t => t.Resolved).ToArray();
            Task.WaitAll(validPackages);

            var valid = validPackages.Where(x => x.Result != null).Select(x => x.Result!);

            // Separate the packages into those who have a location and those
            // who do not. The former we will look up on disk, the others in
            // nuget.
            var packagesAndLocations = valid.Select(package => (package, location: packagePolicies.GetLocation(package.Id)));
            var packagesWithLocations = packagesAndLocations.Where(p => p.location != null);
            var packagesWithoutLocations = packagesAndLocations.Except(packagesWithLocations).Select(p => p.package);
            {
                var dependencies = await this.registry.FindPackageDependencies(packagesWithoutLocations);
                result.AddRange(dependencies);
            }
            {
                foreach (var (package, location) in packagesWithLocations)
                {
                    var x = new AnalyzedPackage(package);
                    var dependencies = await this.FindPackageDependencies(location!);
                    var y = new DependencyChain<AnalyzedPackage>(x, dependencies);
                    result.Add(y);
                }
            }


            return result;
        }

        private IList<PackageRange> GetTopLevelDependencies(string root)
        {
            string[] csProjFiles = this.fileSystem.GetFiles(root, "*.csproj");
            return csProjFiles.SelectMany(SearchProjectFile).ToList();
        }

        private string? GetAttributeOrChild(XmlNode n, string attrName)
        {
            if (n.Attributes[attrName] != null)
            {
                return n.Attributes[attrName].Value;
            }

            return n.SelectSingleNode($"*[local-name()='{attrName}']")?.InnerText;
        }

        private IEnumerable<PackageRange> SearchProjectFile(string csProjPath)
        {
            List<PackageRange> topLevelDependencies = new List<PackageRange>();
            string csProjText = this.fileSystem.ReadAllText(csProjPath);
            XmlDocument csProj = new XmlDocument();
            csProj.LoadXml(csProjText);

            var packageReferenceNodes = csProj.SelectNodes("//*[local-name()='PackageReference']");
            foreach (XmlNode packageReferenceNode in packageReferenceNodes)
            {
                string? package = GetAttributeOrChild(packageReferenceNode, "Include");
                string? version = GetAttributeOrChild(packageReferenceNode, "Version");
                if (version == null || package == null)
                {
                    continue;
                }

                topLevelDependencies.Add(new PackageRange(package, version));
            }

            var noneIncludeNodes = csProj.SelectNodes("//*[local-name()='None'][@Include]");
            foreach (XmlNode node in noneIncludeNodes)
            {
                if (node.Attributes["Include"].Value == "packages.config")
                {
                    topLevelDependencies.AddRange(GetFromPackageConfig(csProjPath));
                    break;
                }
            }

            return topLevelDependencies;
        }

        private IEnumerable<PackageRange> GetFromPackageConfig(string csProjPath)
        {
            string dir = Path.GetDirectoryName(csProjPath); // Note: This will not work for URLs
            string path = Path.Combine(dir, "packages.config");
            if (!this.fileSystem.FileExists(path))
            {
                Log.Error($"Could not find packages.config at {path}");
                return new PackageRange[] { };
            }

            string packagesStr = this.fileSystem.ReadAllText(path);
            XmlDocument packagesXml = new XmlDocument();
            packagesXml.LoadXml(packagesStr);

            var packages = packagesXml.SelectNodes("//*[local-name()='package'][@id][@version]");
            List<PackageRange> result = new List<PackageRange>();
            foreach (XmlNode node in packages)
            {
                string id = node.Attributes["id"].Value;
                string version = node.Attributes["version"].Value;
                result.Add(new PackageRange(id, version));
            }

            return result;
        }
    }
}

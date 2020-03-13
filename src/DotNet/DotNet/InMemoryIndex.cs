using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;

namespace LicenseInspector.DotNet
{
    internal class InMemoryIndex : IPackageVersionProvider
    {
        private readonly IDictionary<string, HashSet<string>> packageIdToPagePaths;
        private readonly IDictionary<string, HashSet<string>> packageIdToVersions;
        private readonly IDictionary<string, PackageDetails> pathToPackageDetails;

        private readonly object pathToPackageDetailsLock = new object();

        public InMemoryIndex()
        {
            this.packageIdToPagePaths = new Dictionary<string, HashSet<string>>();
            this.packageIdToVersions = new Dictionary<string, HashSet<string>>();
            this.pathToPackageDetails = new Dictionary<string, PackageDetails>();
        }

        internal InMemoryIndex(Content content)
        {
            this.packageIdToPagePaths = content.PackageIdToPagePaths;
            this.packageIdToVersions = content.PackageIdToVersions;
            this.pathToPackageDetails = content.PathToPackageDetails;
        }

        public void Add(CatalogPage page)
        {
            foreach (var item in page.Items)
            {
                if (!packageIdToPagePaths.ContainsKey(item.Id))
                {
                    packageIdToPagePaths.Add(item.Id, new HashSet<string>());
                    packageIdToVersions.Add(item.Id, new HashSet<string>());
                }

                string filePath = NuGetCatalog.GetPageCachePath(page.Url);
                packageIdToPagePaths[item.Id].Add(filePath);

                packageIdToVersions[item.Id].Add(item.Version);
            }
        }

        public void AddPackageDetails(PackageDetails package)
        {
            lock (pathToPackageDetailsLock)
            {
                string key = $"{package.Id}_{package.Version}";
                if (!this.pathToPackageDetails.ContainsKey(key))
                {
                    this.pathToPackageDetails.Add(key, package);
                }
            }
        }

        public HashSet<string> GetPagePaths(string packageId)
        {
            if (!this.packageIdToPagePaths.ContainsKey(packageId))
            {
                Log.Debug($"Found no information about {packageId}");
                return new HashSet<string>();
            }

            return this.packageIdToPagePaths[packageId];
        }

        public IEnumerable<string> GetVersions(string packageId)
        {
            return this.packageIdToVersions[packageId];
        }

        public bool TryGetDetails(IPackage package, out PackageDetails details)
        {
            return this.pathToPackageDetails.TryGetValue($"{package.Id}_{package.Version}", out details);
        }

        public string Serialized()
        {
            var content = new Content
            {
                PackageIdToPagePaths = packageIdToPagePaths,
                PackageIdToVersions = packageIdToVersions,
                PathToPackageDetails = pathToPackageDetails
            };

            return JsonConvert.SerializeObject(content);
        }

        public class Content
        {
#pragma warning disable CS8618
            public IDictionary<string, HashSet<string>> PackageIdToPagePaths { get; set; }
            public IDictionary<string, HashSet<string>> PackageIdToVersions { get; set; }
            public IDictionary<string, PackageDetails> PathToPackageDetails { get; set; }
#pragma warning restore CS8618
        }
    }
}

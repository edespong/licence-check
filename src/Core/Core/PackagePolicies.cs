using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LicenseInspector
{
    public class PackagePolicies : IExplicitLicenseProvider
    {
        private readonly ICollection<PackagePolicy> policies;

        public PackagePolicies(ICollection<PackagePolicy> policies)
        {
            this.policies = policies.Select(r => !r.Package.EndsWith("*") ? r : new PrefixPackagePolicy(r)).ToList();
        }

        public bool IgnorePackage(string packageId)
        {
            return this.policies.FirstOrDefault(r => r.IsMatch(packageId))?.Ignore ?? false;
        }

        public bool AllowLicense(string packageId)
        {
            return this.policies.FirstOrDefault(r => r.IsMatch(packageId))?.AllowLicense ?? false;
        }

        public bool TryGetLicense(string packageId, out string license)
        {
            license = this.policies.FirstOrDefault(r => r.IsMatch(packageId))?.License ?? string.Empty;
            return !string.IsNullOrEmpty(license);
        }

        public string? GetLocation(string packageId)
        {
            return this.policies
                .FirstOrDefault(policy => policy.IsMatch(packageId) && policy.Location != null)?
                .Location;
        }

        public static PackagePolicies LoadFrom(string path)
        {
            string str = File.ReadAllText(path);
            ICollection<PackagePolicy> policies = JsonConvert.DeserializeObject<List<PackagePolicy>>(str)
                .Where(p => p != null)
                .ToList();
            return new PackagePolicies(policies);
        }
    }
}

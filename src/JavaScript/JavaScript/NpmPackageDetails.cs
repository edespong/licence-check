using Serilog;
using System;
using System.Linq;

namespace LicenseInspector.JavaScript
{
    internal class NpmPackageDetails : IPackageDetails
    {
        private readonly NpmPackage package;

        public NpmPackageDetails(NpmPackage package)
        {
            this.package = package;
            PackageUrl = GetPackageUrl(package);
            (License, LicenseUrl) = GetLicense(package);
        }

        public string Id => this.package.Id;

        public string Version => this.package.Version;

        public string? License { get; }

        public Uri? PackageUrl { get; }

        public Uri? LicenseUrl { get; }

        private static Uri? GetPackageUrl(NpmPackage package)
        {
            string? url = package.Repository?.Url;
            if (url == null)
            {
                return null;
            }
            else if (url == NpmPackage.UnknownUrl)
            {
                return null;
            }

            if (!url.Contains("+"))
            {
                // 
                // git://github.com/zertosh/loose-envify.git
                // https://github.com/then/promise.git
                return new Uri(url);
            }
            else
            {
                // Example:
                // git+https://github.com/facebook/fbjs.git
                return new Uri(url.Substring(url.IndexOf("+") + 1));
            }
        }

        private (string?, Uri?) GetLicense(NpmPackage package)
        {
            if (package.Licenses?.Count > 0)
            {
                foreach (var l in package.Licenses.Skip(1))
                {
                    Log.Warning($"Found multiple licenses for {package.Id} {package.Version}. Will NOT include {l.Type}");
                }

                if (package.Licenses[0].Url == null)
                {
                    return (package.Licenses[0].Type, null);
                }
                else
                {
                    return (package.Licenses[0].Type, new Uri(package.Licenses[0].Url));
                }
            }

            if (package.License != null)
            {
                return (package.License, null);
            }

            return (null, null);
        }
    }
}

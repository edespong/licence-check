using System;

namespace LicenseInspector.DotNet
{
    internal class NuGetPackageDetails : IPackageDetails
    {
        private readonly PackageDetails details;

        public NuGetPackageDetails(PackageDetails details)
        {
            this.details = details;
        }

        public string Id => this.details.Id;

        public string Version => this.details.Version;

        public string? License => this.details.LicenseExpression;

        public Uri? PackageUrl => this.details.ProjectUrl != null ? new Uri(this.details.ProjectUrl) : null;

        public Uri? LicenseUrl => this.details.LicenseUrl;
    }
}

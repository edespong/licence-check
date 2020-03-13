using System;

namespace LicenseInspector
{
    public class PackagePolicy
    {
        /// <summary>
        /// The id of the package or a prefix followed by an * for wildcard end
        /// matching.
        /// </summary>
        /// <example>
        /// The value 'MyCompany.*' will match any package where the id starts
        /// with MyCompany., e.g. 'MyCompany.Utils', 'MyCompany.Web.Http'.
        /// </example>
        public string Package { get; set; } = string.Empty;

        /// <summary>
        /// An explicit license for the package, which can be used if the
        /// automatic mechanisms for determening the license fails.
        /// </summary>
        public string License { get; set; } = string.Empty;

        /// <summary>
        /// Whether the license of the package should be allowed or not,
        /// regardless of which license it is or even if it can be found.
        /// </summary>
        public bool AllowLicense { get; set; }

        /// <summary>
        /// Indicates if the package should be completely ignored or not. If
        /// ignored, there will be no attempt to find the dependencies of the
        /// package.
        /// </summary>
        public bool Ignore { get; set; }

        /// <summary>
        /// A location on disk where the source of the package can be found.
        /// </summary>
        /// <remarks>
        /// Use for internal packages where you want to recursively
        /// crawl the dependencies of those packages as well.
        /// </remarks>
        public string? Location { get; set; }

        public virtual bool IsMatch(string packageId)
        {
            return Package.Equals(packageId);
        }
    }

    internal class PrefixPackagePolicy : PackagePolicy
    {
        public PrefixPackagePolicy(PackagePolicy policy)
        {
            if (!policy.Package.EndsWith("*", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Expected the package id to end with an asterix '*'");
            }

            Package = policy.Package.TrimEnd(new[] { '*' });
            AllowLicense = policy.AllowLicense;
            Ignore = policy.Ignore;
            License = policy.License;
        }

        public override bool IsMatch(string packageId)
        {
            return packageId.StartsWith(Package, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}

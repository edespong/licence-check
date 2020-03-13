using System;

namespace LicenseInspector
{
    /// <summary>
    /// Connects a license to data regarding the license.
    /// </summary>
    internal class LicenseInfo
    {
        private static readonly Uri[] NoUrls = new Uri[] { };

        /// <summary>
        /// The SPDX identifier of the license.
        /// </summary>
        public string License { get; set; } = string.Empty;

        /// <summary>
        /// Known URLs pointing to the license.
        /// </summary>
        public Uri[] KnownUrls { get; set; } = NoUrls;
    }

    /// <summary>
    /// Represents a license that can be connected to a package.
    /// </summary>
    public class License
    {
        public const string UnknownLicenseStr = "<unknown>";
        public const string NonEvaluatedLicenseStr = "<not evaluated>";
        public const string InternalLicenseStr = "<internal>";

        /// <summary>
        /// The SPDX identifier of the license.
        /// </summary>
        public string Id { get; set; }

        public License(string id)
        {
            Id = id;
        }

        /// <summary>
        /// Represents a license where an evaluation has taken place but the
        /// real license could not be determined.
        /// </summary>
        public static readonly License UnknownLicense = new License(UnknownLicenseStr);

        /// <summary>
        /// Represents the license of a package where no evaluation of the
        /// license has been tried.
        /// </summary>
        public static readonly License NonEvaluated = new License(NonEvaluatedLicenseStr);

        /// <summary>
        /// Represents the license of a package belonging to a company internal package.
        /// </summary>
        public static readonly License Internal = new License(InternalLicenseStr);
    }
}

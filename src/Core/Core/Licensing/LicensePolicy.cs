using System;

namespace LicenseInspector
{
    /// <summary>
    /// Represents a policy for a license, e.g. whether it should be allowed or
    /// not.
    /// </summary>
    internal class LicensePolicy
    {
        /// <summary>
        /// Can be either a SPDX Identifier, see https://spdx.org/licenses but
        /// also supports an ending '*' for prefix matching, e.g. 'Apache-*'
        /// will match all of 'Apache-1.0', 'Apache-1.1' and 'Apache-2.0'.
        /// </summary>
        public string License { get; set; } = string.Empty;

        /// <summary>
        /// Specifies whether the license is okay to use or not.
        /// </summary>
        public bool Allow { get; set; }

        public virtual bool IsMatch(string licenseId)
        {
            return License.Equals(licenseId);
        }
    }

    internal class PrefixLicensePolicy : LicensePolicy
    {
        public PrefixLicensePolicy(LicensePolicy policy)
        {
            if (!policy.License.EndsWith("*"))
            {
                throw new ArgumentException("Expected the license id to end with an asterix '*'");
            }

            License = policy.License.TrimEnd(new[] { '*' });
            Allow = policy.Allow;
        }

        public override bool IsMatch(string licenseId)
        {
            return licenseId.StartsWith(License);
        }
    }
}

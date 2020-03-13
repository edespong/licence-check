using SemVersion;
using SemVersion.Parser;
using Serilog;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LicenseInspector
{
    /// <summary>
    /// Responisble for getting a single, valid version of a referenced
    /// package.
    /// </summary>
    public class PackageVersionResolver : IPackageVersionResolver
    {
        private readonly IPackageVersionProvider versionProvider;

        public PackageVersionResolver(IPackageVersionProvider versionProvider)
        {
            this.versionProvider = versionProvider;
        }

        public Task<Package?> GetSingleVersion(IPackageRange package)
        {
            Package? result = GetSingleVersionAux(package);
            return Task.FromResult(result);
        }

        private Package? GetSingleVersionAux(IPackageRange package)
        {
            // https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-ranges-and-wildcards
            string range = package.VersionRange;

            // Exact version match: "[x.y.z]"
            if (!range.Contains(",") && range.StartsWith("[") && range.EndsWith("]"))
            {
                int length = range.Length - 2;
                return new Package(package.Id, range.Substring(1, length).Trim());
            }

            // Minimum version, inclusive: "x.y.z"
            if (!range.Contains(","))
            {
                return new Package(package.Id, range.Trim());
            }

            // Inclusive minimal version: "[x.y.z, ..."
            if (range.StartsWith("["))
            {
                int length = range.IndexOf(",") - 1;
                return new Package(package.Id, range.Substring(1, length).Trim());
            }

            // Inclusive maximal version: "..., x.y.z]"
            if (range.StartsWith("]"))
            {
                int length = range.IndexOf(",") - 1;
                return new Package(package.Id, range.Substring(1, length).Trim());
            }

            // Exclusive range: "(x.y.z, a.b.c)", "(x.y.z,)", "(, x.y.z)"
            if (range.StartsWith("(") && range.EndsWith(")"))
            {
                // We cannot pick a specific version because we do not know which exist
                var versions = this.versionProvider.GetVersions(package.Id);
                var compare = GetExlusiveVersionComparer(range);
                var version = versions.Select(SemanticVersion.Parse).FirstOrDefault(compare);
                if (version == null)
                {
                    Log.Error("Could not find valid version for range: " + package.VersionRange);
                    return null;
                }

                return new Package(package.Id, version.ToString());
            }

            Log.Error("Unhandled version format: " + package.VersionRange);
            return null;
        }

        private Func<SemanticVersion, bool> GetExlusiveVersionComparer(string range)
        {
            // Range should be one of the following formats: "(x.y.z, a.b.c)", "(x.y.z,)", "(, x.y.z)"
            Regex r = new Regex(@"\((?<low>.*)?,(?<high>.*)?\)", RegexOptions.Compiled);
            var match = r.Match(range);
            if (!match.Success)
            {
                Log.Error($"Could not find semantic version comparer for version range {range}");
                return v => false;
            }

            string low = match.Groups["low"].Value.Trim();
            string high = match.Groups["high"].Value.Trim();

            string expression = string.Empty;
            if (!string.IsNullOrEmpty(low))
            {
                expression = $"> {low}";
            }

            if (!string.IsNullOrEmpty(high))
            {
                if (string.IsNullOrEmpty(expression))
                {
                    expression = $"< {high}";
                }
                else
                {
                    expression += $" && < {high}";
                }
            }

            RangeParser parser = new RangeParser();
            return parser.Evaluate(expression);
        }
    }
}

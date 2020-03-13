using Newtonsoft.Json;
using System;
using System.Diagnostics;

namespace LicenseInspector
{
    public interface IPackage
    {
        string Id { get; }
        string Version { get; }
    }

    public interface IPackageRange
    {
        string Id { get; }
        string VersionRange { get; }
    }

    [DebuggerDisplay("[PackageRange] {Id} {VersionRange}")]
    public class PackageRange : IPackageRange
    {
        public string Id { get; }
        public string VersionRange { get; }

        public PackageRange(string id, string versionRange)
        {
            Id = id;
            VersionRange = versionRange;
        }

        public override string ToString()
        {
            return $"{Id} {VersionRange}";
        }
    }

    [DebuggerDisplay("[Package] {Id}")]
    public class Package : IPackage, IPackageRange
    {
        [JsonProperty(Order = 0)]
        public string Id { get; }

        [JsonProperty(Order = 1)]
        public string Version { get; }

        [JsonIgnore]
        public string VersionRange => "[" + Version + "]";

        public Package(string id, string version)
        {
            Id = id;
            Version = version;
        }

        public override string ToString()
        {
            return $"{Id} {Version}";
        }
    }

    public interface IPackageDetails
    {
        string Id { get; }
        string Version { get; }
        string? License { get; }
        Uri? PackageUrl { get; }
        Uri? LicenseUrl { get; }
    }
}

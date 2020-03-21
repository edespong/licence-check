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
        string OriginProject { get; }
    }

    [DebuggerDisplay("[PackageRange] {Id} {VersionRange}")]
    public class PackageRange : IPackageRange
    {
        public string Id { get; }
        public string VersionRange { get; }
        public string OriginProject { get; }

        public PackageRange(string id, string versionRange, string originProject)
        {
            Id = id;
            VersionRange = versionRange;
            OriginProject = originProject;
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

        [JsonProperty(Order = 2)]
        public string OriginProject { get; }

        [JsonIgnore]
        public string VersionRange => "[" + Version + "]";

        public Package(string id, string version, string originProject)
        {
            Id = id;
            Version = version;
            OriginProject = originProject;
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

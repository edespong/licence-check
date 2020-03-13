using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
[assembly: InternalsVisibleTo("LicenseInspector.DotNet.Tests")]

#pragma warning disable CS8618

namespace LicenseInspector.DotNet
{
    [DebuggerDisplay("[CatalogEntity] {Url}")]
    internal abstract class CatalogEntity
    {
        [JsonProperty("@id")]
        public string Url { get; set; }

        public DateTime CommitTimeStamp { get; set; }
    }

    internal class CatalogIndex : CatalogEntity
    {
        public List<CatalogPageIdentifier> Items { get; set; }
    }

    internal class CatalogPageIdentifier : CatalogEntity
    {
    }

    [DebuggerDisplay("[CatalogPage] {Url} Items: {Items.Count}")]
    internal class CatalogPage : CatalogPageIdentifier
    {
        public IList<CatalogPackage> Items { get; set; }
    }

    [DebuggerDisplay("[CatalogPackage] {Id} {Version}")]
    internal class CatalogPackage : CatalogEntity
    {
        [JsonProperty("nuget:id")]
        public string Id { get; set; }

        [JsonProperty("nuget:version")]
        public string OriginalVersion { get; set; }

        [JsonProperty("@type")]
        public string Type { get; set; }

        [JsonIgnore]
        public string Version => !OriginalVersion.Contains("+") ? OriginalVersion : OriginalVersion.Substring(0, OriginalVersion.IndexOf("+"));  // Never null

        public CatalogPackage() { }

        public CatalogPackage(string id, string version, string type = "nuget:PackageDetails")
        {
            Id = id;
            OriginalVersion = version;
            Type = type;
        }
    }

    [DebuggerDisplay("[PackageDetails] {Id} {Version}")]
    internal class PackageDetails : IPackage
    {
        [JsonProperty("@id")]
        public string Url { get; set; }

        public string Id { get; set; }
        public string? ProjectUrl { get; set; }

        [JsonProperty("version")]
        public string OriginalVersion { get; set; }

        public string? LicenseExpression { get; set; }
        public Uri? LicenseUrl { get; set; }
        public List<PackageDependencyGroup>? DependencyGroups { get; set; }

        [JsonIgnore]
        public string Version => !OriginalVersion.Contains("+") ? OriginalVersion : OriginalVersion.Substring(0, OriginalVersion.IndexOf("+"));
    }

    internal class PackageDependencyGroup
    {
        [JsonProperty("@id")]
        public string Url { get; set; }

        public string? TargetFramework { get; set; }

        public List<PackageDependency>? Dependencies { get; set; }
    }

    [DebuggerDisplay("[PackageDependency] {Id} {VersionRange}")]
    internal class PackageDependency : IPackageRange
    {
        [JsonProperty("@id")]
        public string Url { get; set; }

        public string Id { get; set; }

        [JsonProperty("range")]
        public string VersionRange { get; set; }
    }
}

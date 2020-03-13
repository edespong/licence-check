using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Diagnostics;

namespace LicenseInspector
{
    /// <summary>
    /// Represents the state of analysis. If Ok, it should continue otherwise
    /// not.
    /// </summary>
    public enum AnalysisState
    {
        Ok = 1,
        Error
    }

    /// <summary>
    /// Represents the final evaluation of a license for a package.
    /// </summary>
    public enum Evaluation
    {
        Ok = 1,
        Violation,
        Ignored
    }

    [DebuggerDisplay("[AnalyzedPackage] {Id}")]
    public class AnalyzedPackage : IPackage, IPackageRange
    {
        public string Id { get; }
        public string Version { get; }
        public IList<string> Messages { get; } = new List<string>();

        [JsonConverter(typeof(StringEnumConverter))]
        public AnalysisState State { get; }

        [JsonIgnore]
        public string VersionRange => "[" + Version + "]";

        public AnalyzedPackage(string id, string version)
        {
            Id = id;
            Version = version;
            State = AnalysisState.Ok;
        }

        public AnalyzedPackage(Package package)
        {
            Id = package.Id;
            Version = package.Version;
            State = AnalysisState.Ok;
        }

        public AnalyzedPackage(string id, string version, AnalysisState state, string message)
        {
            Id = id;
            Version = version;
            State = state;
            Messages.Add(message);
        }

        [JsonConstructor]
        public AnalyzedPackage(string id, string version, AnalysisState state, IList<string> messages)
        {
            Id = id;
            Version = version;
            State = state;
            Messages = messages;
        }

        /// <summary>
        /// Created returns a new package with the added state and message.
        /// </summary>
        public AnalyzedPackage With(AnalysisState state, string message)
        {
            return new AnalyzedPackage(Id, Version, state, message);
        }

        /// <summary>
        /// Attached a license the the package, returning the result.
        /// </summary>
        public LicensedPackage Attach(License license)
        {
            return new LicensedPackage(Id, Version, license.Id, State, Messages);
        }

        public override string ToString()
        {
            return $"{Id} {Version}";
        }
    }

    /// <summary>
    /// Represents a package with an attached license, but where the license
    /// status has not been checked.
    /// </summary>
    [DebuggerDisplay("[LicensedPackage] {Id} {License}")]
    public class LicensedPackage : AnalyzedPackage
    {
        [JsonProperty(Order = 2)]
        public string License { get; }

        public LicensedPackage(string packageId, string version, string license) : base(packageId, version)
        {
            License = license;
        }

        public LicensedPackage(string packageId, string version, string license, AnalysisState state, IList<string> messages)
            : base(packageId, version, state, messages)
        {
            License = license;
        }

        public LicensedPackage(LicensedPackage other) : base(other.Id, other.Version)
        {
            License = other.License;
        }

        public override string ToString()
        {
            return base.ToString() + $" {License}";
        }
    }

    /// <summary>
    /// Represents a package that has been fully evaluated.
    /// </summary>
    [DebuggerDisplay("[EvaluatedPackage] {Id} {Result} {License}")]
    public class EvaluatedPackage : LicensedPackage
    {
        [JsonProperty(Order = 3)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Evaluation Result { get; }

        [JsonProperty(Order = 4)]
        public string Remark { get; }

        public EvaluatedPackage(string packageId, string version, string license, Evaluation result, string remark = "") : base(packageId, version, license)
        {
            Result = result;
            Remark = remark;
        }

        public EvaluatedPackage(LicensedPackage package, Evaluation result, string remark = "") : base(package)
        {
            Result = result;
            Remark = remark;
        }

        public override string ToString()
        {
            return base.ToString() + $" {Result}";
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics;

namespace LicenseInspector
{
    [DebuggerDisplay("[DependencyChain] {Package.Id} ({Dependencies.Count})")]
    public class DependencyChain<TPackage> where TPackage : AnalyzedPackage
    {
        public static readonly IList<DependencyChain<TPackage>> EmptyList = new List<DependencyChain<TPackage>>();

        public TPackage Package { get; }
        public ICollection<DependencyChain<TPackage>> Dependencies { get; }

        public DependencyChain(TPackage package, ICollection<DependencyChain<TPackage>> dependencies)
        {
            Package = package;
            Dependencies = dependencies;
        }
    }

    public class InvalidDependencyChain : DependencyChain<AnalyzedPackage>
    {
        public InvalidDependencyChain(Package package, string message)
            : base(new AnalyzedPackage(package.Id, package.Version, AnalysisState.Error, message), EmptyList)
        {
        }

        public InvalidDependencyChain(IPackageRange package, string message)
            : base(new AnalyzedPackage(package.Id, package.VersionRange, AnalysisState.Error, message), EmptyList)
        {
        }
    }

    public class DependencyChainComparer<TPackage> : IEqualityComparer<DependencyChain<TPackage>> where TPackage : AnalyzedPackage
    {
        public bool Equals(DependencyChain<TPackage> x, DependencyChain<TPackage> y)
        {
            return x.Package.Id.Equals(y.Package.Id) && x.Package.Version.Equals(y.Package.Version);
        }

        public int GetHashCode(DependencyChain<TPackage> obj)
        {
            return obj.Package.Id.GetHashCode() ^ obj.Package.Version.GetHashCode();
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;

namespace LicenseInspector
{
    public interface IDependencyScanner
    {
        Task<IList<DependencyChain<AnalyzedPackage>>> FindPackageDependencies(string root);
    }

    public interface ILicenseScanner
    {
        IList<DependencyChain<LicensedPackage>> FindLicenses(ICollection<DependencyChain<AnalyzedPackage>> packages);
    }

    public interface IPackageVersionProvider
    {
        IEnumerable<string> GetVersions(string packageId);
    }

    public interface IExplicitLicenseProvider
    {
        bool TryGetLicense(string packageId, out string license);
    }

    public interface IPackageVersionResolver
    {
        Task<Package?> GetSingleVersion(IPackageRange package);
    }
}

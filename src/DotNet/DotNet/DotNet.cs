using System;
using System.Threading.Tasks;

namespace LicenseInspector.DotNet
{
    public static class DotNet
    {
        public static async Task<(IDependencyScanner, ILicenseScanner)> Create(IFileAccess fileSystem, Config config)
        {
            var packagePolicies = PackagePolicies.LoadFrom(config.PackagePolicies);
            var scanner = await DependencyScanner.Create(config, fileSystem, packagePolicies);

            var catalog = new NuGetCatalog(config.DiskCache);
            var index = await catalog.GetIndex();
            var registry = new NuGetDependencies(index, config);

            Func<IPackage, Task<PackageDetailsResult>> getDetails =
                p => registry.GetPackageDetails(p).ContinueWith(ConvertPackageDetails);

            ILicenseScanner licenseScanner = new LicenseScanner(getDetails, packagePolicies, config);

            return (scanner, licenseScanner);
        }

        private static PackageDetailsResult ConvertPackageDetails(Task<(PackageDetailsResultEnum, PackageDetails?)> pd)
        {
            if (pd.Result.Item1 != PackageDetailsResultEnum.Success)
            {
                return new PackageDetailsResult(pd.Result.Item1);
            }

            return new PackageDetailsResult(new NuGetPackageDetails(pd.Result.Item2!) as IPackageDetails);
        }
    }
}

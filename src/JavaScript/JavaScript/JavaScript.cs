using System;
using System.Threading.Tasks;

namespace LicenseInspector.JavaScript
{
    public static class JavaScript
    {
        public static Task<(IDependencyScanner, ILicenseScanner)> Create(IFileAccess fileSystem, Config config)
        {
            var npm = new Npm();
            var packagePolicies = PackagePolicies.LoadFrom(config.PackagePolicies);
            IDependencyScanner scanner = new DependencyScanner(npm, fileSystem, packagePolicies, config.DiskCache);

            Func<IPackage, Task<PackageDetailsResult>> getDetails =
                p => npm.GetPackage(p).ContinueWith(ConvertPackageDetails);

            ILicenseScanner licenseScanner = new LicenseScanner(getDetails, packagePolicies, config);

            return Task.FromResult((scanner, licenseScanner));
        }

        private static PackageDetailsResult ConvertPackageDetails(Task<(PackageDetailsResultEnum, NpmPackage?)> pd)
        {
            if (pd.Result.Item1 != PackageDetailsResultEnum.Success)
            {
                return new PackageDetailsResult(pd.Result.Item1);
            }

            return new PackageDetailsResult(new NpmPackageDetails(pd.Result.Item2!) as IPackageDetails);
        }
    }
}

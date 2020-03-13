using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LicenseInspector
{
    /// <summary>
    /// Main class for inspecting packages.
    /// </summary>
    public class LicenseInspector
    {
        private readonly IDependencyScanner dependencyScanner;
        private readonly ILicenseScanner licenseScanner;
        private readonly Config config;

        public LicenseInspector(IDependencyScanner dependencyScanner, ILicenseScanner licenseScanner, Config config)
        {
            this.dependencyScanner = dependencyScanner;
            this.licenseScanner = licenseScanner;
            this.config = config;
        }

        /// <summary>
        /// Evaluate all packages, including dependencies, found in the given
        /// directory.
        /// </summary>
        /// <remarks>
        /// The evaluation is done in three steps:
        /// 1. Find all packages and sub-dependencies in the given folder
        /// 2. Find licenses for all the packages
        /// 3. Evaluate whether the licenses according to the policies given
        ///    in config
        /// </remarks>
        public async Task<IList<DependencyChain<EvaluatedPackage>>> EvaluateDirectory(string root)
        {
            Log.Debug("Scanning for dependencies..");
            var allDependencies = await this.dependencyScanner.FindPackageDependencies(root).ConfigureAwait(false);
            Log.Debug($"Done");

            if (!allDependencies.Any())
            {
                Log.Information("Did not find any dependencies");
                return DependencyChain<EvaluatedPackage>.EmptyList;
            }

            Log.Debug("Finding licenses..");
            var licensedDependencies = this.licenseScanner.FindLicenses(allDependencies);
            Log.Debug($"Done");

            Log.Debug("Applying license and package policies..");
            LicensePolicies licensePolicies = LicensePolicies.LoadFrom(this.config.LicensePolicies, this.config.PackagePolicies);
            return licensePolicies.Apply(licensedDependencies);
        }
    }
}

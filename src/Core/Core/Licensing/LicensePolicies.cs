using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LicenseInspector
{
    /// <summary>
    /// Responsible for the application of license policies to packages.
    /// </summary>
    internal class LicensePolicies
    {
        private readonly IEnumerable<LicensePolicy> policies;
        private readonly PackagePolicies packagePolicies;
        private readonly Projects projects;

        public LicensePolicies(IEnumerable<LicensePolicy> policies, PackagePolicies packagePolicies, Projects projects)
        {
            this.policies = policies
                .Select(r => !r.License.EndsWith("*") ? r : new PrefixLicensePolicy(r))
                .ToList();
            this.packagePolicies = packagePolicies;
            this.projects = projects;
        }

        public IList<DependencyChain<EvaluatedPackage>> Apply(IEnumerable<DependencyChain<LicensedPackage>> packages)
        {
            return packages.Select(Apply).ToList();
        }

        public DependencyChain<EvaluatedPackage> Apply(DependencyChain<LicensedPackage> chain)
        {
            var root = Apply(chain.Package);
            var dependencies = Apply(chain.Dependencies);
            return new DependencyChain<EvaluatedPackage>(root, dependencies);
        }

        public EvaluatedPackage Apply(LicensedPackage package)
        {
            if (this.packagePolicies.IgnorePackage(package.Id))
            {
                Log.Warning($"Package {package.Id} is ignored but is still evaluated for license.");
                return new EvaluatedPackage(package, Evaluation.Ignored);
            }

            if (this.packagePolicies.AllowLicense(package.Id))
            {
                return new EvaluatedPackage(package, Evaluation.Ok, "Package license explicitly allowed");
            }

            LicensePolicy policy = this.policies.FirstOrDefault(r => r.IsMatch(package.License));
            if (policy == null)
            {
                return new EvaluatedPackage(package, Evaluation.Violation, "No policy found");
            }

            if (policy.License.Equals(License.UnknownLicenseStr) && !policy.Allow)
            {
                return new EvaluatedPackage(package, Evaluation.Violation, "Could not find license");
            }

            if (policy.AllowInternal && this.projects.Contains(package.OriginProject))
            {
                return new EvaluatedPackage(package, Evaluation.Ok);
            }

            return new EvaluatedPackage(package, policy.Allow ? Evaluation.Ok : Evaluation.Violation);
        }

        public static LicensePolicies LoadFrom(string licensePoliciesPath, string packagePoliciesPath, string projectsInfoPath)
        {
            var packagePolicies = PackagePolicies.LoadFrom(packagePoliciesPath);

            string str = File.ReadAllText(licensePoliciesPath);
            ICollection<LicensePolicy> policies = JsonConvert.DeserializeObject<List<LicensePolicy>>(str);

            var projects = Projects.LoadFrom(projectsInfoPath);

            return new LicensePolicies(policies, packagePolicies, projects);
        }
    }
}

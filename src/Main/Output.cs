using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LicenseInspector
{
    internal static class Output
    {
        public static string Generate(IList<DependencyChain<EvaluatedPackage>> packages, bool prettyPrint)
        {
            if (!prettyPrint)
            {
                return JsonConvert.SerializeObject(packages, Formatting.Indented);
            }

            StringBuilder sb = new StringBuilder();
            foreach (var d in packages.OrderBy(p => p.Package.Id))
            {
                sb.Append(DependencyChainPrinter.Print(d));
            }

            var flattenedDependencies = packages.SelectMany(GetDependencies).Select(p => p.Package);

            sb.AppendLine();
            sb.AppendLine("Summary - licenses:");
            var licenses = flattenedDependencies.GroupBy(x => x.License);
            foreach (var l in licenses.OrderBy(l => l.Key))
            {
                sb.AppendLine($"{l.Key,-25}{l.Count(),4}");
            }

            sb.AppendLine();
            sb.AppendLine("Summary - results:");
            var evaluationResults = flattenedDependencies.GroupBy(x => x.Result);
            foreach (var r in evaluationResults)
            {
                sb.AppendLine($"{r.Key,-25}{r.Count(),4}");
            }

            return sb.ToString();
        }

        private static ICollection<DependencyChain<EvaluatedPackage>> GetDependencies(DependencyChain<EvaluatedPackage> p)
        {
            List<DependencyChain<EvaluatedPackage>> result = new List<DependencyChain<EvaluatedPackage>>();
            result.Add(new DependencyChain<EvaluatedPackage>(p.Package, DependencyChain<EvaluatedPackage>.EmptyList));
            result.AddRange(p.Dependencies.SelectMany(GetDependencies));
            return result;
        }
    }
}

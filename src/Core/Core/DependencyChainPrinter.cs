using System;
using System.Text;

namespace LicenseInspector
{
    public static class DependencyChainPrinter
    {
        /// <summary>
        /// Pretty-print the given dependency chain.
        /// </summary>
        public static string Print(DependencyChain<EvaluatedPackage> package, int level = 0)
        {
            string indent = new string(' ', level);

            string packageInfo = $"{package.Package.Id} {package.Package.Version}";
            string result = $"{indent}- {packageInfo}";
            int pad = Math.Max(0, 70 - result.Length);
            result += package.Package.License.PadLeft(pad, ' ');
            pad = Math.Max(0, 90 - result.Length);
            result += package.Package.Result.ToString().PadLeft(pad, ' ');

            StringBuilder sb = new StringBuilder($"{result}{Environment.NewLine}");
            foreach (var d in package.Dependencies)
            {
                sb.Append(indent).Append(Print(d, level + 1));
            }

            return sb.ToString();
        }
    }
}

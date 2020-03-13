using CommandLine;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace LicenseInspector
{
    public static class Program
    {
        private static readonly string[] SupportedPlatforms = new[] { "dotnet", "js" };

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => MainAsync(options).GetAwaiter().GetResult());
        }

        public static async Task MainAsync(Options options)
        {
            InitializeLogging(options.LogPath, options.Quiet, options.Verbose);

            if (!File.Exists(options.ConfigPath))
            {
                if (Path.IsPathRooted(options.ConfigPath))
                {
                    Log.Error($"Cannot find config file at: {options.ConfigPath}");
                }
                else
                {
                    Log.Error($"Cannot find config file at: {options.ConfigPath} (full path: {Path.GetFullPath(options.ConfigPath)})");
                }
                return;
            }

            Log.Information("Starting processing");
            if (Path.IsPathRooted(options.Root))
            {
                Log.Information($"Scanning {options.Root}");
            }
            else
            {
                Log.Information($"Scanning {options.Root} (full path: {Path.GetFullPath(options.Root)})");
            }
            Log.Information($"Using config from {new FileInfo(options.ConfigPath).FullName}");

            string configStr = File.ReadAllText(options.ConfigPath);
            Config config = JsonConvert.DeserializeObject<Config>(configStr);
            if (!ValidateConfig(config))
            {
                return;
            }

            if (options.NoCache)
            {
                config = Config.WithoutCache(config);
            }

            string? platform = GetPlatformScannersType(options.Platform);
            if (platform == null)
            {
                return;
            }

            Log.Information($"Using package policies from {config.PackagePolicies}");
            Log.Information($"Using license policies from {config.LicensePolicies}");
            Log.Information($"Using license information from {config.LicenseInfo}");

            IFileAccess fileAccess = FileAccess.GetAccessor();
            var (dependencyScanner, licenseScanner) = await GetPlatformScanners(platform, fileAccess, config);

            Log.Debug($"Scanning for {platform} dependencies");

            LicenseInspector pi = new LicenseInspector(dependencyScanner, licenseScanner, config);
            var evaluatedPackages = await pi.EvaluateDirectory(options.Root);

            Log.Debug($"Done with {platform} dependencies");

            string output = GetOutput(evaluatedPackages, options.PrettyPrint);

            if (!string.IsNullOrWhiteSpace(options.Output))
            {
                File.WriteAllText(options.Output, output);
                Log.Information($"All done. Result written to {output}");
            }
            else
            {
                Console.WriteLine(output);
                Log.Information($"All done. Result written to stdout");
            }
        }

        private static string? GetPlatformScannersType(string platform)
        {
            if (!SupportedPlatforms.Contains(platform.ToLower()))
            {
                Log.Error($"Will not scan. Got unsupported values for --platform: {platform}");
                return null;
            }

            return platform.ToLower();
        }

        private static async Task<(IDependencyScanner, ILicenseScanner)> GetPlatformScanners(string platform, IFileAccess fileAccess, Config config)
        {
            return platform switch
            {
                "dotnet" => await DotNet.DotNet.Create(fileAccess, config),
                _ => throw new ArgumentException($"Invalid platform: {platform}")
            };
        }

        private static string GetOutput(IList<DependencyChain<EvaluatedPackage>> packages, bool prettyPrint)
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

            return sb.ToString();
        }

        private static void InitializeLogging(string path, bool quiet, bool verbose)
        {
            if (quiet)
            {
                return;
            }

            var config = new LoggerConfiguration().MinimumLevel.Information();
            if (verbose)
            {
                config = config.MinimumLevel.Debug();
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                Log.Logger = config
                    .WriteTo.Console()
                    .CreateLogger();
                return;
            }
            else if (Directory.Exists(path))
            {
                path = Path.Combine(path, "license-inspector-.log");
            }

            Log.Logger = config
                .WriteTo.File(path,
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        private static bool ValidateConfig(Config config)
        {
            bool success = true;

            if (config.DiskCache.CacheRoot == null)
            {
                Log.Error("Configuration error. DiskCache->CacheRoot must be set");
                success = false;
            }

            success = success && OkOrDefaultPathConfig(() => config.PackagePolicies, p => config.PackagePolicies = p, nameof(config.PackagePolicies), "packagePolicies.json");
            success = success && OkOrDefaultPathConfig(() => config.LicensePolicies, p => config.LicensePolicies = p, nameof(config.LicensePolicies), "licensePolicies.json");
            success = success && OkOrDefaultPathConfig(() => config.LicenseInfo, p => config.LicenseInfo = p, nameof(config.LicenseInfo), "licenses.json");

            return success;
        }

        private static bool OkOrDefaultPathConfig(Func<string> getPath, Action<string> setPath, string configName, string defaultFilename)
        {
            if (string.IsNullOrEmpty(getPath()))
            {
                string? codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                if (codeBase == null)
                {
                    return false;
                }

                string? executingDir = Path.GetDirectoryName(new Uri(codeBase).LocalPath);
                if (executingDir == null)
                {
                    return false;
                }

                setPath(Path.Combine(executingDir, defaultFilename));
            }

            if (!File.Exists(getPath()))
            {
                Log.Error($"Configuration error. The specified path for {configName} does not exist: {getPath()}");
                return false;
            }

            return true;
        }

        private class Scanner
        {
            public string Name { get; }
            public IDependencyScanner DependencyScanner { get; }

            public ILicenseScanner LicenseScanner { get; }

            public Scanner(string name, IDependencyScanner dependencyScanner, ILicenseScanner licenseScanner)
            {
                Name = name;
                DependencyScanner = dependencyScanner;
                LicenseScanner = licenseScanner;
            }
        }
    }
}

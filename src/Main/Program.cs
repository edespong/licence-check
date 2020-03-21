using CommandLine;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
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

            if (!Directory.Exists(options.Root))
            {
                Log.Error($"Cannot find directory given by -r: {options.Root}");
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
            Log.Information($"Using project information from {config.ProjectsInfo}");

            IFileAccess fileAccess = FileAccess.GetAccessor();
            var (dependencyScanner, licenseScanner) = await GetPlatformScanners(platform, fileAccess, config);

            Log.Debug($"Scanning for {platform} dependencies");

            LicenseInspector pi = new LicenseInspector(dependencyScanner, licenseScanner, config);
            var evaluatedPackages = await pi.EvaluateDirectory(options.Root);

            Log.Debug($"Done with {platform} dependencies");

            string output = Output.Generate(evaluatedPackages, options.PrettyPrint);

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
                "js" => await JavaScript.JavaScript.Create(fileAccess, config),
                _ => throw new ArgumentException($"Invalid platform: {platform}")
            };
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

            success = success && OkOrDefaultPathConfig(() => config.PackagePolicies.Split(","), nameof(config.PackagePolicies), "publicPackagePolicies.json, internalPackagePolicies.json");
            success = success && OkOrDefaultPathConfig(() => config.LicensePolicies, nameof(config.LicensePolicies), "licensePolicies.json");
            success = success && OkOrDefaultPathConfig(() => config.LicenseInfo, nameof(config.LicenseInfo), "licenses.json");
            success = success && OkOrDefaultPathConfig(() => config.ProjectsInfo, nameof(config.ProjectsInfo), "projects.json");

            if (success)
            {
                SetIfEmpty(config.LicensePolicies, () => config.LicensePolicies = "licensePolicies.json");
                SetIfEmpty(config.LicenseInfo, () => config.LicenseInfo = "licenses.json");
                SetIfEmpty(config.ProjectsInfo, () => config.ProjectsInfo = "projects.json");
                foreach (var p in config.PackagePolicies.Split(","))
                {
                    SetIfEmpty(config.PackagePolicies, () => config.PackagePolicies = "publicPackagePolicies.json, internalPackagePolicies.json");
                }
            }

            return success;
        }

        private static bool OkOrDefaultPathConfig(Func<string[]> getPaths, string configName, string defaultFilename)
        {
            return getPaths().All(path => OkOrDefaultPathConfig(() => path.Trim(), configName, defaultFilename));
        }

        private static bool OkOrDefaultPathConfig(Func<string> getPath, string configName, string defaultFilename)
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

                var paths = defaultFilename
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => Path.Combine(executingDir, p.Trim()))
                    .ToArray();

                return OkOrDefaultPathConfig(() => paths, configName, string.Empty);
            }

            if (!File.Exists(getPath()))
            {
                Log.Error($"Configuration error. The specified path for {configName} does not exist: {getPath()}");
                return false;
            }

            return true;
        }

        private static void SetIfEmpty(string path, Action setPath)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return;
            }

            setPath();
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

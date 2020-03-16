using CommandLine;

namespace LicenseInspector
{
    public class Options
    {
        [Option('t', "platform", Required = false, Default = "dotnet", HelpText = "Type of code projects to scan. Supported values: dotnet, js")]
        public string Platform { get; set; } = string.Empty;

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('q', "quiet", Required = false, HelpText = "Do not log anything.")]
        public bool Quiet { get; set; }

        [Option('l', "log-path", Required = false, HelpText = "(Default: No log file created) Path or directory to put log file")]
        public string LogPath { get; set; } = string.Empty;

        [Option('r', "root", Required = true, HelpText = "The directory to scan for dependencies and licenses in.")]
        public string Root { get; set; } = string.Empty;

        [Option('o', "output", Required = false, HelpText = "File path to write the final result to. If not supplied, output will be written to stdout")]
        public string Output { get; set; } = string.Empty;

        [Option('c', "configuration", Required = true, HelpText = "Path to the configuration file to use.")]
        public string ConfigPath { get; set; } = string.Empty;

        [Option('p', "pretty", Required = false, HelpText = "Pretty print the output instead of the default JSON.")]
        public bool PrettyPrint { get; set; }

        [Option('n', "no-cache", Required = false, HelpText = "Do not make use of (most of) the cache.")]
        public bool NoCache { get; set; }
    }
}

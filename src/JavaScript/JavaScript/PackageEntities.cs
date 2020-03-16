using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LicenseInspector.JavaScript.Tests")]

namespace LicenseInspector.JavaScript
{
#pragma warning disable CS8618
    internal class PackageJson
    {
        [JsonProperty("name")]
        public string Id { get; set; }

        public Dictionary<string, string> Dependencies { get; set; }
    }

    internal class NpmLicense
    {
        public string Type { get; set; }
        public string? Url { get; set; }
    }

    internal class Repository
    {
        public string Type { get; set; }
        public string Url { get; set; }
    }

    internal class NpmPackageOverview
    {
        [JsonProperty("name")]
        public string Id { get; set; }

        public Dictionary<string, object>? Versions { get; set; }
    }
#pragma warning restore CS8618

    internal class NpmPackage
    {
        public static string UnknownUrl = "<unknown>";

#pragma warning disable CS8618
        [JsonProperty("name")]
        public string Id { get; set; }

        public string Version { get; set; }
        public string? License { get; set; }
        public List<NpmLicense>? Licenses { get; set; }
        public Repository? Repository { get; set; }
        public Dictionary<string, string>? Dependencies { get; set; }
#pragma warning restore CS8618

        public static NpmPackage? Deserialize(string text)
        {
            try
            {
                return JsonConvert.DeserializeObject<NpmPackage>(text);
            }
            catch
            {
                // Try with old, deprecated schema allows for a NpmLicense
                // object for the "license" field
                if (TryDeserializeOldFormat(text, out var result1))
                {
                    return result1;
                }

                // Try with an invalid format found for some packages.
                if (TryDeserializeInvalidRepositoryFormat(text, out var result2))
                {
                    return result2;
                }

                Log.Error($"Could not deserialize NPM package. Invalid format? Full text: {text}");
                return null;
            }
        }

        private static bool TryDeserializeOldFormat(string text, [NotNullWhen(true)] out NpmPackage? result)
        {
            try
            {
                JObject j = JObject.Parse(text);
                result = new NpmPackage
                {
                    Id = j["name"]!.ToObject<string>()!,
                    Version = j["version"]!.ToObject<string>()!,
                    License = j["license"]?.SelectToken("type", true)?.ToObject<string>(),
                    Repository = j["repository"]?.ToObject<Repository>(),
                    Dependencies = j["dependencies"]?.ToObject<Dictionary<string, string>>(),
                };
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private static bool TryDeserializeInvalidRepositoryFormat(string text, [NotNullWhen(true)] out NpmPackage? result)
        {
            try
            {
                JObject j = JObject.Parse(text);
                string repositoryString = j["repository"]?.ToObject<string>()!;
                result = new NpmPackage
                {
                    Id = j["name"]!.ToObject<string>()!,
                    Version = j["version"]!.ToObject<string>()!,
                    License = j["license"]?.ToObject<string>(),
                    Repository = new Repository { Type = $"unknown:{repositoryString}", Url = UnknownUrl },
                    Dependencies = j["dependencies"]?.ToObject<Dictionary<string, string>>(),
                };
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}

using Newtonsoft.Json;
using Serilog;
using System;
using System.Threading.Tasks;

namespace LicenseInspector.JavaScript
{
    internal interface INpm
    {
        Task<(PackageDetailsResultEnum, NpmPackage?)> GetPackage(IPackage package);
        Task<NpmPackageOverview?> GetPackageOverview(string packageId);
    }

    internal class Npm : INpm
    {
        private readonly Uri baseUrl;

        public Npm()
        {
            this.baseUrl = new Uri("https://registry.npmjs.org/");
        }

        public async Task<(PackageDetailsResultEnum, NpmPackage?)> GetPackage(IPackage package)
        {
            Uri url = new Uri(this.baseUrl, $"{package.Id}/{package.Version}");
            string? text = await GetText(url);
            if (text == null)
            {
                return (PackageDetailsResultEnum.NoPackageFound, null);
            }
            return (PackageDetailsResultEnum.Success, NpmPackage.Deserialize(text));
        }

        public async Task<NpmPackageOverview?> GetPackageOverview(string packageId)
        {
            Uri url = new Uri(this.baseUrl, $"{packageId}");
            string? text = await GetText(url);
            return text == null ? null : JsonConvert.DeserializeObject<NpmPackageOverview>(text);
        }

        private async Task<string?> GetText(Uri url)
        {
            try
            {
                return await SharedHttpClient.GetStringAsync(url).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                string message = $"Error getting package file from {url}: {e.Message}";
                Log.Warning(message);
                return null;
            }
        }
    }
}

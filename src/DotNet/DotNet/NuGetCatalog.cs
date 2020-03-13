using Newtonsoft.Json;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LicenseInspector.DotNet
{
    /// <summary>
    /// Proxy class toward the online NuGet catalog.
    /// </summary>
    internal class NuGetCatalog
    {
        private readonly string catalogIndexFileName;
        private readonly string indexFileName;
        private static string pagePathFormatStr = Path.Combine("pages", "{0}");

        private const string NuGetIndexUrl = "https://api.nuget.org/v3/index.json";

        private readonly DiskCacheConfig config;

        public NuGetCatalog(DiskCacheConfig config)
        {
            this.config = config;
            this.catalogIndexFileName = Path.Combine(config.CacheRoot, "catalogIndex.json");
            this.indexFileName = Path.Combine(config.CacheRoot, "index.json");
            pagePathFormatStr = Path.Combine(config.CacheRoot, "pages", "{0}");
        }

        public async Task<InMemoryIndex> GetIndex()
        {
            if (DiskCache.TryGetValue(this.indexFileName, this.config.NuGetIndex, out InMemoryIndex.Content? cached))
            {
                Log.Debug("Using cached index");
                return new InMemoryIndex(cached);
            }

            Log.Debug("Will fetch fresh index..");
            InMemoryIndex index = new InMemoryIndex();
            IList<CatalogPage> pages = await GetAllPages();
            foreach (var page in pages)
            {
                index.Add(page);
            }

            File.WriteAllText(this.indexFileName, index.Serialized());
            return index;
        }

        internal static string GetPageCachePath(string pageUrl)
        {
            string filename = pageUrl.Substring(pageUrl.LastIndexOf('/') + 1);
            return string.Format(pagePathFormatStr, filename);
        }

        private async Task<IList<CatalogPage>> GetAllPages()
        {
            var index = await GetCatalogIndex();
            var pageItems = index.Items;
            return await GetPages(pageItems);
        }

        private async Task<CatalogIndex> GetCatalogIndex()
        {
            if (DiskCache.TryGetValue(this.catalogIndexFileName, this.config.NuGetCatalogIndex, out CatalogIndex? cached))
            {
                Log.Debug($"Using cached index from {this.catalogIndexFileName}");
                return cached;
            }

            return await DownloadIndex().ContinueWith(Cache);

            CatalogIndex Cache(Task<CatalogIndex> i)
            {
                if (!this.config.NuGetCatalogIndex.DoCache)
                {
                    return i.Result;
                }

                return DiskCache.Cache(this.catalogIndexFileName, i);
            }
        }

        private static async Task<CatalogIndex> DownloadIndex()
        {
            var catalogIndexUrl = await GetCatalogIndexUrlAsync(NuGetIndexUrl);

            string indexString = await SharedHttpClient.GetStringAsync(catalogIndexUrl);
            Log.Information($"Fetched catalog index {catalogIndexUrl}");
            var index = JsonConvert.DeserializeObject<CatalogIndex>(indexString);
            return index;
        }

        private static async Task<IList<CatalogPage>> GetPages(IEnumerable<CatalogPageIdentifier> pageIds)
        {
            // Assumes URLs ends with '/page[digits].json', e.g. https://api.nuget.org/v3/catalog0/page8163.json
            Regex digitRegex = new Regex(@"/page(\d+)\.json$", RegexOptions.Compiled);
            var orderedPages = pageIds.OrderByDescending(x => int.Parse(digitRegex.Match(x.Url).Groups[1].Value));

            var result = new List<CatalogPage>();
            bool forceDownload = true;

            int cachedCount = Directory.GetFiles(Path.GetDirectoryName(pagePathFormatStr)).Count();
            Log.Information($"Fetching {orderedPages.Count().ToString()} pages. Around {cachedCount} seems to be in the disk cache.");
            foreach (var pageId in orderedPages)
            {
                CatalogPage page;
                (page, forceDownload) = await GetPage(pageId, forceDownload);
                result.Add(page);
            }
            Log.Information("Done fetching pages.");
            return result;
        }

        private static async Task<(CatalogPage, bool)> GetPage(CatalogPageIdentifier pageId, bool forceDownload)
        {
            string filePath = GetPageCachePath(pageId.Url);

            if (!File.Exists(filePath))
            {
                Log.Debug($"Cannot find page {pageId.Url}, will download it..");
                return (await DownloadPage(pageId).ContinueWith(CachePage), true);
            }

            if (forceDownload)
            {
                Log.Debug($"Download of page {pageId.Url} was forced..");
                return (await DownloadPage(pageId).ContinueWith(CachePage), false);
            }

            string pageString = File.ReadAllText(filePath);
            return (JsonConvert.DeserializeObject<CatalogPage>(pageString), false);

            CatalogPage CachePage(Task<CatalogPage> i)
            {
                return DiskCache.Cache(filePath, i);
            }
        }

        private static async Task<CatalogPage> DownloadPage(CatalogPageIdentifier pageId)
        {
            var pageString = await SharedHttpClient.GetStringAsync(pageId.Url);
            return JsonConvert.DeserializeObject<CatalogPage>(pageString);
        }

        private static async Task<Uri> GetCatalogIndexUrlAsync(string sourceUrl)
        {
            var sourceRepository = Repository.Factory.GetCoreV3(sourceUrl);
            var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            var catalogIndexUrl = serviceIndex.GetServiceEntryUri(new[] { "Catalog/3.0.0" });
            return catalogIndexUrl;
        }
    }
}

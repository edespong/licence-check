using System;

namespace LicenseInspector
{
    /// <summary>
    /// Contains available configuration for the application.
    /// </summary>
    public class Config
    {
        /// <summary>Path to package policy file(s)</summary>
        /// <remarks>Can contain a comma-separated list of files.</remarks>
        public string PackagePolicies { get; set; } = string.Empty;
        public string LicensePolicies { get; set; } = string.Empty;
        public string LicenseInfo { get; set; } = string.Empty;
        public double MinimumLicenseConfidenceThreshold { get; set; }
        public bool IgnoreDuplicatePackages { get; set; }
        public DiskCacheConfig DiskCache { get; set; } = new DiskCacheConfig();

        public static Config WithoutCache(Config config)
        {
            return new Config
            {
                PackagePolicies = config.PackagePolicies,
                LicensePolicies = config.LicensePolicies,
                LicenseInfo = config.LicenseInfo,
                MinimumLicenseConfidenceThreshold = config.MinimumLicenseConfidenceThreshold,
                IgnoreDuplicatePackages = config.IgnoreDuplicatePackages,
                DiskCache = new DiskCacheConfig
                {
                    CacheRoot = config.DiskCache.CacheRoot,
                    NuGetCatalogIndex = DiskCacheItem.NoCache,
                    NuGetIndex = config.DiskCache.NuGetIndex,
                    PackageDetails = DiskCacheItem.NoCache,
                    ResolvedDependencies = DiskCacheItem.NoCache,
                    LicenseFiles = DiskCacheItem.NoCache,
                    ResolvedLicenses = DiskCacheItem.NoCache,
                    NpmPackages = DiskCacheItem.NoCache,
                }
            };
        }
    }

    public class DiskCacheConfig
    {
        public string CacheRoot { get; set; } = string.Empty;

        public DiskCacheItem NuGetCatalogIndex { get; set; } = new DiskCacheItem
        {
            DoCache = true,
            MaxAge = TimeSpan.FromDays(1)
        };

        public DiskCacheItem NuGetIndex { get; set; } = new DiskCacheItem
        {
            DoCache = true,
            MaxAge = TimeSpan.FromDays(1)
        };

        public DiskCacheItem PackageDetails { get; set; } = new DiskCacheItem
        {
            DoCache = true,
            MaxAge = TimeSpan.FromDays(14)
        };

        public DiskCacheItem ResolvedDependencies { get; set; } = new DiskCacheItem
        {
            DoCache = true,
            MaxAge = TimeSpan.FromDays(14)
        };

        public DiskCacheItem LicenseFiles { get; set; } = new DiskCacheItem
        {
            DoCache = true,
            MaxAge = TimeSpan.FromDays(14)
        };

        public DiskCacheItem ResolvedLicenses { get; set; } = new DiskCacheItem
        {
            DoCache = true,
            MaxAge = TimeSpan.FromDays(14)
        };

        public DiskCacheItem NpmPackages { get; set; } = new DiskCacheItem
        {
            DoCache = true,
            MaxAge = TimeSpan.FromDays(14)
        };
    }

    public struct DiskCacheItem
    {
        public bool DoCache { get; set; }
        public TimeSpan MaxAge { get; set; }

        public static DiskCacheItem NoCache
        {
            get
            {
                return new DiskCacheItem
                {
                    DoCache = false,
                    MaxAge = TimeSpan.FromSeconds(0),
                };
            }
        }
    }
}


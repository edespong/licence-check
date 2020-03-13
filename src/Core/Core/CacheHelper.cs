using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseInspector
{
    /// <summary>
    /// Contains helper functions for caching data to disk.
    /// </summary>
    public static class DiskCache
    {
        private static readonly IDictionary<string, string> sessionCache = new Dictionary<string, string>();

        private static readonly object writeLock = new object();

        public static bool TryGetValue<T>(string path, DiskCacheItem config, [NotNullWhen(true)] out T? cachedResult) where T : class
        {
            if (!TryGetValue(path, config, out string? resultStr))
            {
                cachedResult = default;
                return false;
            }

            cachedResult = JsonConvert.DeserializeObject<T>(resultStr!);
            return true;
        }

        public static bool TryGetValue(string path, DiskCacheItem config, out string? cachedResult)
        {
            cachedResult = null;
            if (sessionCache.ContainsKey(path))
            {
                cachedResult = sessionCache[path];
                return true;
            }

            if (!config.DoCache)
            {
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            TimeSpan age = DateTime.Now - File.GetLastWriteTime(path);
            if (age > config.MaxAge)
            {
                return false;
            }

            cachedResult = File.ReadAllText(path);
            return true;
        }

        public static T Cache<T>(string path, Task<T> item) where T : class
        {
            string str = JsonConvert.SerializeObject(item.Result);
            CacheData(path, str);
            return item.Result;
        }

        public static void Cache<T>(string path, T item) where T : class
        {
            string str = JsonConvert.SerializeObject(item);
            CacheData(path, str);
        }

        public static void CacheData(string path, string data, bool retry = true)
        {
            string expectedDir = Path.GetDirectoryName(path);
            if (!Directory.Exists(expectedDir))
            {
                Directory.CreateDirectory(expectedDir);
            }

            try
            {
                lock (writeLock)
                {
                    sessionCache[path] = data;
                    File.WriteAllText(path, data);
                }
            }
            catch (Exception e)
            {
                if (retry)
                {
                    Thread.Sleep(500);
                    CacheData(path, data, false);
                }
                else
                {
                    Log.Error($"Error caching data to {path}: {e.Message}");
                }
            }
        }
    }
}

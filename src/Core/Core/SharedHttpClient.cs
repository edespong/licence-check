using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LicenseInspector
{
    /// <summary>
    /// Shared class for fetching HTTP data.
    /// </summary>
    /// <remarks>
    /// See https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong
    /// </remarks>
    public sealed class SharedHttpClient
    {
        private static readonly Lazy<SharedHttpClient> lazy = new Lazy<SharedHttpClient>(() => new SharedHttpClient());

        public static SharedHttpClient Instance { get { return lazy.Value; } }

        private SharedHttpClient()
        {
            Client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
        }

        public HttpClient Client { get; }

        public static Task<string> GetStringAsync(string requestUri)
        {
            return Instance.Client.GetStringAsync(requestUri);
        }

        public static Task<string> GetStringAsync(Uri requestUri)
        {
            return Instance.Client.GetStringAsync(requestUri);
        }
    }
}

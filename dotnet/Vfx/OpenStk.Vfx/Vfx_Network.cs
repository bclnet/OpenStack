using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace OpenStack.Vfx;

/// <summary>
/// AbstractHost
/// </summary>
//public abstract class AbstractHost {
//    /// <summary>
//    /// Gets the set asynchronous.
//    /// </summary>
//    /// <param name="shouldThrow">if set to <c>true</c> [should throw].</param>
//    /// <returns></returns>
//    public abstract Task<HashSet<string>> GetSetAsync(bool shouldThrow = false);

//    /// <summary>
//    /// Gets the file asynchronous.
//    /// </summary>
//    /// <param name="filePath">The file path.</param>
//    /// <param name="shouldThrow">if set to <c>true</c> [should throw].</param>
//    /// <returns></returns>
//    public abstract Task<Stream> GetFileAsync(string filePath, bool shouldThrow = false);
//}

/// <summary>
/// NetworkHost
/// </summary>
public class NetworkHost {
    readonly MemoryCache Cache = new(new MemoryCacheOptions { });
    readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(30) };

    public NetworkHost(Uri address, string folder = null)
        => Client.BaseAddress = folder == null ? address : new UriBuilder(address) { Path = $"{address.LocalPath}{folder}/" }.Uri;

    public static readonly Func<Uri, string, NetworkHost> Factory = (address, folder) => new NetworkHost(address, folder);

    public async Task<T> CallAsync<T>(string path, NameValueCollection nvc = null, bool shouldThrow = false) {
        var requestUri = ToPathAndQueryString(path, nvc);
        //Log($"query: {requestUri}");
        var r = await Client.GetAsync(requestUri).ConfigureAwait(false);
        if (!r.IsSuccessStatusCode) return !shouldThrow ? default : throw new InvalidOperationException(r.ReasonPhrase);
        var data = await r.Content.ReadAsByteArrayAsync();
        return FromBytes<T>(data);
    }

    public virtual async Task<HashSet<string>> GetSetAsync(bool shouldThrow = false)
        => await Cache.GetOrCreate(".set", async x => await CallAsync<HashSet<string>>((string)x.Key));

    public virtual async Task<Stream> GetFileAsync(string filePath, bool shouldThrow = false)
        => await Cache.GetOrCreateAsync(filePath.Replace('\\', '/'), async x => await CallAsync<Stream>((string)x.Key));

    static string ToPathAndQueryString(string path, NameValueCollection nvc) {
        if (nvc == null) return path;
        var array = (
            from key in nvc.AllKeys
            from value in nvc.GetValues(key)
            select !string.IsNullOrEmpty(value) ? $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(value)}" : null)
            .Where(x => x != null).ToArray();
        return path + (array.Length > 0 ? $"?{string.Join("&", array)}" : "");
    }

    static T FromBytes<T>(byte[] data) {
        string path;
        if (typeof(T) == typeof(Stream)) return (T)(object)new MemoryStream(data);
        else if (typeof(T) == typeof(HashSet<string>)) {
            var d = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // dir /s/b/a-d > .set
            var lines = Encoding.ASCII.GetString(data)?.Split('\n');
            if (lines?.Length >= 0) {
                var startIndex = Path.GetDirectoryName(lines[0].TrimEnd().Replace('\\', '/')).Length + 1;
                foreach (var line in lines) if (line.Length >= startIndex && (path = line[startIndex..].TrimEnd().Replace('\\', '/')) != ".set") d.Add(path);
            }
            return (T)(object)d;
        }
        else throw new ArgumentOutOfRangeException(nameof(T), typeof(T).ToString());
    }
}

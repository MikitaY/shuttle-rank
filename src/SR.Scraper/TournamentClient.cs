using System.Security.Cryptography;
using System.Text;
using SR.Core;

namespace SR.Scraper;

/// <summary>tournamentsoftware HTTP client with an on-disk cache.
/// The cache key — md5(url [+ "|xhr"]) — matches the Python version, so already
/// downloaded pages are reused without hitting the site.
/// Results of a finished tournament never change, so the cache has no expiry; pages
/// that do change are re-fetched via <c>bypassCache</c> (see <see cref="Scraper"/>).</summary>
public sealed class TournamentClient : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly string _cacheDir;
    private readonly bool _refresh;

    public TournamentClient(AppConfig cfg, string cacheDir, bool refresh)
    {
        _cacheDir = cacheDir;
        _refresh = refresh;
        Directory.CreateDirectory(_cacheDir);
        _http.Timeout = TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", cfg.UserAgent);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cfg.Cookie);
    }

    /// <param name="bypassCache">Ignore any cached copy and download the page again,
    /// overwriting the cache entry. For pages whose content still changes.</param>
    public async Task<string> FetchAsync(string url, bool xhr = false, bool bypassCache = false)
    {
        var key = Md5Hex(url + (xhr ? "|xhr" : ""));
        var cacheFile = Path.Combine(_cacheDir, key + ".html");
        if (File.Exists(cacheFile) && !_refresh && !bypassCache)
            return await File.ReadAllTextAsync(cacheFile, Encoding.UTF8);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (xhr) req.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        using var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync();

        await Task.Delay(700); // polite pause between real requests
        await File.WriteAllTextAsync(cacheFile, text, new UTF8Encoding(false));
        return text;
    }

    private static string Md5Hex(string s) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    public void Dispose() => _http.Dispose();
}

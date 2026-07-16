using System.Security.Cryptography;
using System.Text;
using SR.Core;

namespace SR.Scraper;

/// <summary>HTTP-клиент tournamentsoftware с дисковым кэшем.
/// Ключ кэша — md5(url [+ "|xhr"]) — совпадает с Python-версией, поэтому
/// уже скачанные страницы переиспользуются без обращения к сайту.</summary>
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

    public async Task<string> FetchAsync(string url, bool xhr = false)
    {
        var key = Md5Hex(url + (xhr ? "|xhr" : ""));
        var cacheFile = Path.Combine(_cacheDir, key + ".html");
        if (File.Exists(cacheFile) && !_refresh)
            return await File.ReadAllTextAsync(cacheFile, Encoding.UTF8);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (xhr) req.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        using var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync();

        await Task.Delay(700); // вежливая пауза между реальными запросами
        await File.WriteAllTextAsync(cacheFile, text, new UTF8Encoding(false));
        return text;
    }

    private static string Md5Hex(string s) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    public void Dispose() => _http.Dispose();
}

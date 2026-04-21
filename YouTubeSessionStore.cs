using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace SimpleYoutubeDownloader;

/// <summary>
/// Sessão YouTube para o YoutubeExplode: cópia em memória + ficheiro encriptado (DPAPI, só o utilizador Windows)
/// em %LocalAppData%\SimpleYoutubeDownloader\. Evita cookies em texto na pasta do projeto e falhas por CWD.
/// </summary>
internal static class YouTubeSessionStore
{
    private static readonly object Sync = new();
    private static IReadOnlyList<Cookie>? _memoryCookies;

    /// <summary>Pasta onde fica <c>youtube-session.dpapi</c> (DPAPI, só este utilizador Windows).</summary>
    public static string EncryptedSessionDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleYoutubeDownloader");

    private static string ProtectedSessionPath => Path.Combine(EncryptedSessionDirectory, "youtube-session.dpapi");

    /// <summary>Legado: ficheiro ao lado do .exe (não usa diretório de trabalho).</summary>
    private static string LegacyPlainCookiesPath => Path.Combine(AppContext.BaseDirectory, "cookies.json");

    private static readonly JsonSerializerOptions JsonReadRelaxed = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>Grava DPAPI + atualiza memória. Lança se a lista for vazia.</summary>
    public static void SaveProtected(IReadOnlyList<Cookie> cookies, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cookies);
        if (cookies.Count == 0)
            throw new InvalidOperationException("Nenhum cookie foi recolhido do browser.");

        Directory.CreateDirectory(EncryptedSessionDirectory);
        var dtos = cookies.Select(CookieDto.FromNet).ToList();
        var json = JsonSerializer.Serialize(dtos);
        var plain = Encoding.UTF8.GetBytes(json);
        var blob = ProtectedData.Protect(plain, optionalEntropy: null, DataProtectionScope.CurrentUser);

        cancellationToken.ThrowIfCancellationRequested();

        lock (Sync)
        {
            _memoryCookies = cookies.ToList();
        }

        File.WriteAllBytes(ProtectedSessionPath, blob);
    }

    public static async Task<(List<Cookie> Cookies, bool UsingCookies)> TryLoadForYoutubeClientAsync(
        CancellationToken cancellationToken)
    {
        lock (Sync)
        {
            if (_memoryCookies is { Count: > 0 })
                return (_memoryCookies.ToList(), true);
        }

        if (File.Exists(ProtectedSessionPath))
        {
            try
            {
                var blob = await File.ReadAllBytesAsync(ProtectedSessionPath, cancellationToken).ConfigureAwait(false);
                var plain = ProtectedData.Unprotect(blob, optionalEntropy: null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(plain);
                var dtos = JsonSerializer.Deserialize<List<CookieDto>>(json);
                if (dtos is { Count: > 0 })
                {
                    var list = dtos.Select(d => d.ToNet()).ToList();
                    lock (Sync)
                    {
                        _memoryCookies = list;
                    }

                    return (list, true);
                }
            }
            catch (Exception ex)
            {
                AppFileLogger.Write("Falha ao ler sessão YouTube (ficheiro encriptado em AppData).", ex);
            }
        }

        if (File.Exists(LegacyPlainCookiesPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(LegacyPlainCookiesPath, cancellationToken).ConfigureAwait(false);
                var fromDto = JsonSerializer.Deserialize<List<CookieDto>>(json, JsonReadRelaxed);
                if (fromDto is { Count: > 0 })
                {
                    var list = fromDto.Select(d => d.ToNet()).ToList();
                    lock (Sync)
                    {
                        _memoryCookies = list;
                    }

                    return (list, true);
                }

                var legacyNet = JsonSerializer.Deserialize<List<Cookie>>(json, JsonReadRelaxed);
                if (legacyNet is { Count: > 0 })
                {
                    lock (Sync)
                    {
                        _memoryCookies = legacyNet.ToList();
                    }

                    return (legacyNet.ToList(), true);
                }
            }
            catch (Exception ex)
            {
                AppFileLogger.Write("cookies.json (legado) inválido ou ilegível.", ex);
            }
        }

        return (new List<Cookie>(), false);
    }

    /// <summary>
    /// Nomes de cookies críticos para autenticar pedidos do InnerTube (SAPISIDHASH, sessão de utilizador).
    /// Sem estes, a sessão está incompleta mesmo que existam outros cookies.
    /// </summary>
    private static readonly string[] CriticalCookieNames =
    {
        "SAPISID",
        "__Secure-1PAPISID",
        "__Secure-3PAPISID",
        "SID",
        "__Secure-1PSID",
        "__Secure-3PSID",
        "HSID",
        "SSID",
        "LOGIN_INFO"
    };

    /// <summary>Resumo da sessão atual (cookies por domínio, presença de cookies críticos). Só para diagnóstico.</summary>
    public static string Summarize(IReadOnlyList<Cookie> cookies)
    {
        if (cookies.Count == 0) return "Sessão: 0 cookies (anónimo).";

        var byDomain = cookies
            .GroupBy(c => string.IsNullOrEmpty(c.Domain) ? "(?)" : c.Domain)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}={g.Count()}")
            .ToArray();

        var present = cookies.Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        var haveCritical = CriticalCookieNames.Where(present.Contains).ToArray();
        var missingCritical = CriticalCookieNames.Where(n => !present.Contains(n)).ToArray();

        var sb = new StringBuilder();
        sb.Append("Sessão: ").Append(cookies.Count).Append(" cookies totais");
        sb.Append(" [").Append(string.Join(", ", byDomain)).Append(']');
        sb.Append(". Críticos presentes: ");
        sb.Append(haveCritical.Length == 0 ? "nenhum" : string.Join(", ", haveCritical));
        if (missingCritical.Length > 0)
            sb.Append(". Em falta: ").Append(string.Join(", ", missingCritical));
        return sb.ToString();
    }

    /// <summary>Modo privado: apaga ficheiros de sessão e limpa memória.</summary>
    public static void ClearAllPersistedAndMemory()
    {
        lock (Sync)
        {
            _memoryCookies = null;
        }

        TryDelete(ProtectedSessionPath);
        TryDelete(LegacyPlainCookiesPath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignorado — utilizador ou antivírus pode bloquear
        }
    }

    private sealed class CookieDto
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Path { get; set; }
        public string? Domain { get; set; }
        public bool Secure { get; set; }

        public static CookieDto FromNet(Cookie c) =>
            new()
            {
                Name = c.Name,
                Value = c.Value,
                Path = string.IsNullOrEmpty(c.Path) ? "/" : c.Path,
                Domain = c.Domain,
                Secure = c.Secure
            };

        public Cookie ToNet()
        {
            var name = Name ?? "";
            var value = Value ?? "";
            var path = string.IsNullOrEmpty(Path) ? "/" : Path!;
            var domain = string.IsNullOrEmpty(Domain) ? ".youtube.com" : Domain!;
            return new Cookie(name, value, path, domain) { Secure = Secure };
        }
    }
}

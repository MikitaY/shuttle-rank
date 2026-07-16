using System.Text.Encodings.Web;
using System.Text.Json;

namespace SR.Core;

/// <summary>Единые настройки сериализации: snake_case ключи, читаемый отступ,
/// кириллица без \u-экранирования (аналог ensure_ascii=False в Python).</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = null,              // ключи словарей ("overall", "E"…) как есть
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

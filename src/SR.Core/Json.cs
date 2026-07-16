using System.Text.Encodings.Web;
using System.Text.Json;

namespace SR.Core;

/// <summary>Shared serialization options: snake_case keys, readable indentation,
/// and non-ASCII (Cyrillic) left unescaped (like ensure_ascii=False in Python).</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = null,              // keep dictionary keys ("overall", "E"…) verbatim
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}

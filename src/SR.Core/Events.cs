namespace SR.Core;

/// <summary>Parsing of tournamentsoftware event codes and round names.</summary>
public static class Events
{
    /// <summary>"SE - Group A" -> ("SE", "Group A"); "XDD" -> ("XDD", null).</summary>
    public static (string Event, string? Group) ParseEventCode(string drawName)
    {
        var parts = drawName.Split(" - ", 2);
        var evt = parts[0].Trim();
        var group = parts.Length > 1 ? parts[1].Trim() : null;
        return (evt, group);
    }

    /// <summary>Event code -> (discipline, level).
    /// SE = singles E, DC = doubles C, XDB = mixed B, MSA = men's singles A,
    /// MDC = men's doubles C, MASTER+ = doubles Masters.</summary>
    public static (string Discipline, string? Level) EventMeta(string eventCode)
    {
        var code = eventCode.ToUpperInvariant();
        static string? At(string s, int i) => i < s.Length ? s[i].ToString() : null;

        if (code.StartsWith("MASTER")) return ("doubles", "M");
        if (code.StartsWith("XD")) return ("mixed", At(code, 2));
        if (code.StartsWith("MS")) return ("singles", At(code, 2));
        if (code.StartsWith("WS")) return ("singles", At(code, 2));
        if (code.StartsWith("MD")) return ("doubles", At(code, 2));
        if (code.StartsWith("WD")) return ("doubles", At(code, 2));
        if (code.StartsWith("S")) return ("singles", At(code, 1));
        if (code.StartsWith("D")) return ("doubles", At(code, 1));
        return ("unknown", null);
    }

    /// <summary>Round name -> stage used for awarding points.</summary>
    public static string RoundKind(string? roundName)
    {
        var r = (roundName ?? "").ToLowerInvariant();
        if (r.Contains("final") && !r.Contains("semi") && !r.Contains("quarter")) return "final";
        if (r.Contains("semi")) return "semi";
        if (r.Contains("quarter")) return "quarter";
        return "group";
    }
}

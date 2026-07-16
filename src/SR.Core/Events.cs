namespace SR.Core;

/// <summary>Разбор кодов событий и раундов tournamentsoftware.</summary>
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

    /// <summary>Код события -> (дисциплина, уровень).
    /// SE = одиночки E, DC = пары C, XDB = микст B, MASTER+ = пары Masters.</summary>
    public static (string Discipline, string? Level) EventMeta(string eventCode)
    {
        var code = eventCode.ToUpperInvariant();
        static string? At(string s, int i) => i < s.Length ? s[i].ToString() : null;

        if (code.StartsWith("MASTER")) return ("doubles", "M");
        if (code.StartsWith("XD")) return ("mixed", At(code, 2));
        if (code.StartsWith("WS")) return ("singles", At(code, 2));
        if (code.StartsWith("WD")) return ("doubles", At(code, 2));
        if (code.StartsWith("S")) return ("singles", At(code, 1));
        if (code.StartsWith("D")) return ("doubles", At(code, 1));
        return ("unknown", null);
    }

    /// <summary>Название раунда -> стадия для начисления очков.</summary>
    public static string RoundKind(string? roundName)
    {
        var r = (roundName ?? "").ToLowerInvariant();
        if (r.Contains("final") && !r.Contains("semi") && !r.Contains("quarter")) return "final";
        if (r.Contains("semi")) return "semi";
        if (r.Contains("quarter")) return "quarter";
        return "group";
    }
}

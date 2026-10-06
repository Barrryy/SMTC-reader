using System.Text.RegularExpressions;

namespace SmtcReader.Smtc;

/// <summary>Filtering and "which session is current" logic. Pure functions, fully tested.</summary>
public static class SessionQuery
{
    public static string Key(string aumid, string? title) => aumid + "|" + (title ?? string.Empty);

    public static IReadOnlyList<SessionSnapshot> Apply(
        IReadOnlyList<SessionSnapshot> sessions,
        string? pattern,
        string? currentSessionKey)
    {
        var result = new List<SessionSnapshot>(sessions.Count);
        foreach (var session in sessions)
        {
            // Windows hands out a fresh RCW per call, so object identity cannot be used
            // to match GetCurrentSession() against GetSessions(). AUMID + media title is
            // the closest stable key, and it also survives apps that open several
            // sessions at once (PotPlayer does).
            var isCurrent = currentSessionKey is not null &&
                            string.Equals(
                                Key(session.SourceAppUserModelId, session.Media.Title),
                                currentSessionKey,
                                StringComparison.Ordinal);

            var snapshot = session with { IsCurrentSession = isCurrent };
            if (pattern is null || Matches(snapshot, pattern))
            {
                result.Add(snapshot);
            }
        }

        return result;
    }

    public static bool Matches(SessionSnapshot session, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return true;
        }

        var regex = BuildRegex(pattern);
        if (regex.IsMatch(session.SourceAppUserModelId))
        {
            return true;
        }

        return session.ResolvedAppName is { Length: > 0 } name && regex.IsMatch(name);
    }

    /// <summary>
    /// Builds a case-insensitive wildcard matcher. A pattern without any <c>*</c> is
    /// treated as a substring search, which is what people expect from
    /// <c>--app pot</c>.
    /// </summary>
    public static Regex BuildRegex(string pattern)
    {
        var effective = pattern.Contains('*') ? pattern : "*" + pattern + "*";
        var escaped = Regex.Escape(effective).Replace("\\*", ".*", StringComparison.Ordinal);
        return new Regex("^" + escaped + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

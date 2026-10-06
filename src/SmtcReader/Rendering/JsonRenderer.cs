using System.Text.Encodings.Web;
using System.Text.Json;
using SmtcReader.Smtc;

namespace SmtcReader.Rendering;

/// <summary>Machine readable output. The shape is part of the public contract.</summary>
public static class JsonRenderer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Render(IReadOnlyList<SessionSnapshot> sessions, DateTimeOffset capturedAt)
    {
        var report = new JsonReport(capturedAt, sessions.Count, sessions);
        return JsonSerializer.Serialize(report, Options);
    }

    private sealed record JsonReport(
        DateTimeOffset capturedAt,
        int count,
        IReadOnlyList<SessionSnapshot> sessions);
}

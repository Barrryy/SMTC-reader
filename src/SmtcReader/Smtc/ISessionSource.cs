namespace SmtcReader.Smtc;

/// <summary>
/// The single seam between "talk to Windows" and everything else. The WinRT
/// implementation is deliberately thin so the rest of the program stays pure and
/// testable without a live media session.
/// </summary>
public interface ISessionSource
{
    Task<CaptureResult> CaptureAsync(bool includeRaw);
}

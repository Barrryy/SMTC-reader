using SmtcReader.Smtc;
using Xunit;

namespace SmtcReader.Tests;

public class SessionQueryTests
{
    [Fact]
    public void Apply_marks_the_current_session_using_aumid_and_title()
    {
        var current = FakeSessions.Spotify();
        var key = SessionQuery.Key(current.SourceAppUserModelId, current.Media.Title);

        var sessions = SessionQuery.Apply(
            new[] { FakeSessions.Spotify(isCurrent: false), FakeSessions.MinimalDesktopPlayer() },
            pattern: null,
            currentSessionKey: key);

        Assert.True(sessions[0].IsCurrentSession);
        Assert.False(sessions[1].IsCurrentSession);
    }

    [Fact]
    public void Apply_does_not_mark_a_different_track_of_the_same_app()
    {
        var other = FakeSessions.Spotify(title: "Come As You Are");
        var key = SessionQuery.Key(other.SourceAppUserModelId, "Smells Like Teen Spirit");

        var sessions = SessionQuery.Apply(new[] { other }, pattern: null, currentSessionKey: key);

        Assert.False(sessions[0].IsCurrentSession);
    }

    [Theory]
    [InlineData("spotify")]
    [InlineData("SPOTIFY")]
    [InlineData("*spotify*")]
    [InlineData("*Spotify*")]
    public void Matches_supports_substring_and_wildcards_case_insensitively(string pattern)
    {
        Assert.True(SessionQuery.Matches(FakeSessions.Spotify(), pattern));
    }

    [Fact]
    public void Matches_uses_the_resolved_app_name_too()
    {
        var netease = FakeSessions.MinimalDesktopPlayer() with { ResolvedAppName = "网易云音乐" };
        Assert.True(SessionQuery.Matches(netease, "网易云"));
        Assert.True(SessionQuery.Matches(FakeSessions.MinimalDesktopPlayer(), "cloudmusic"));
    }

    [Fact]
    public void Matches_is_anchored_when_the_pattern_has_wildcards()
    {
        Assert.False(SessionQuery.Matches(FakeSessions.Spotify(), "pot*"));
    }

    [Fact]
    public void Apply_filters_out_everything_that_does_not_match()
    {
        var sessions = SessionQuery.Apply(
            new[] { FakeSessions.Spotify(), FakeSessions.MinimalDesktopPlayer() },
            pattern: "cloudmusic",
            currentSessionKey: null);

        Assert.Single(sessions);
        Assert.Equal("cloudmusic.exe", sessions[0].SourceAppUserModelId);
    }
}

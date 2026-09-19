using Windows.Storage.Streams;

namespace FluentMusic.Models;

/// <summary>
/// A snapshot of whatever Windows is currently playing, flattened out of the
/// SMTC APIs so the rest of the app never touches them directly.
/// </summary>
public sealed class MediaSessionInfo
{
    public required string Title { get; init; }

    public required string Artist { get; init; }

    public string? Album { get; init; }

    /// <summary>Friendly name of the app that owns the session, e.g. "Spotify".</summary>
    public required string SourceApp { get; init; }

    public PlaybackState State { get; init; }

    public bool IsPlaying => State == PlaybackState.Playing;

    public TimeSpan Position { get; init; }

    public TimeSpan Duration { get; init; }

    /// <summary>True when the session reported a usable duration.</summary>
    public bool HasTimeline => Duration > TimeSpan.Zero;

    /// <summary>
    /// Artwork as handed over by the session. Left as a stream reference so the service
    /// stays free of UI types; the view model turns it into an image.
    /// </summary>
    public IRandomAccessStreamReference? Thumbnail { get; init; }

    /// <summary>
    /// Identifies the track so callers can tell a genuine track change from a routine
    /// position update, and avoid reloading identical artwork.
    /// </summary>
    public string TrackKey => $"{SourceApp}|{Title}|{Artist}|{Album}";

    public bool CanPlay { get; init; }

    public bool CanPause { get; init; }

    public bool CanGoNext { get; init; }

    public bool CanGoPrevious { get; init; }

    public bool CanSeek { get; init; }
}

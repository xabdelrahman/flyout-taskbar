using FluentMusic.Models;
using Windows.Foundation;
using Windows.Media.Control;

using SmtcManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;
using SmtcSession = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;
using SmtcStatus = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionPlaybackStatus;

namespace FluentMusic.Services;

/// <summary>
/// Watches the Windows System Media Transport Controls and reports whatever is
/// currently playing. Everything here is event driven: nothing polls, and the service
/// re-attaches itself as apps open, close and hand playback between each other.
/// </summary>
public sealed class MediaSessionService : IDisposable
{
    private static readonly Dictionary<string, string> KnownApps = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome"] = "Chrome",
        ["msedge"] = "Edge",
        ["firefox"] = "Firefox",
        ["spotify"] = "Spotify",
        ["anghami"] = "Anghami",
        ["anghamiinc"] = "Anghami",
        ["vlc"] = "VLC",
        ["zune"] = "Media Player",
    };

    /// <summary>
    /// How long to wait before showing the empty state. Apps briefly report no usable
    /// session while switching tracks, and without this the panel flashes
    /// "Nothing playing" between songs.
    /// </summary>
    private static readonly TimeSpan EmptyStateGrace = TimeSpan.FromMilliseconds(600);

    private readonly object _gate = new();

    private SmtcManager? _manager;
    private SmtcSession? _session;
    private CancellationTokenSource? _pendingEmpty;
    private bool _disposed;

    /// <summary>Raised when the session, its metadata or its playback state changes.</summary>
    public event EventHandler<MediaSessionInfo?>? SessionChanged;

    public MediaSessionInfo? Current { get; private set; }

    public async Task InitializeAsync()
    {
        _manager = await SmtcManager.RequestAsync();

        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
        _manager.SessionsChanged += OnSessionsChanged;

        AttachToBestSession();
    }

    private void OnCurrentSessionChanged(SmtcManager sender, CurrentSessionChangedEventArgs args) =>
        AttachToBestSession();

    private void OnSessionsChanged(SmtcManager sender, SessionsChangedEventArgs args) =>
        AttachToBestSession();

    /// <summary>
    /// Picks the session to follow: whichever one is actually playing, falling back to
    /// the session Windows considers current. This is what lets the app land on Anghami
    /// or a YouTube tab without the user choosing a source.
    /// </summary>
    private void AttachToBestSession()
    {
        ResolveAndAttach();
        _ = PublishAsync();
    }

    /// <summary>Re-resolves the best session and subscribes to it, without publishing.</summary>
    private void ResolveAndAttach()
    {
        if (_manager is null)
        {
            return;
        }

        SmtcSession? best = null;

        try
        {
            foreach (var candidate in _manager.GetSessions())
            {
                if (candidate.GetPlaybackInfo()?.PlaybackStatus == SmtcStatus.Playing)
                {
                    best = candidate;
                    break;
                }
            }

            best ??= _manager.GetCurrentSession();
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            // A session can disappear between being listed and being queried.
            best = null;
        }

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (!ReferenceEquals(best, _session))
            {
                DetachCurrentSession();
                _session = best;

                if (_session is not null)
                {
                    _session.MediaPropertiesChanged += OnSessionDetailChanged;
                    _session.PlaybackInfoChanged += OnSessionDetailChanged;
                    _session.TimelinePropertiesChanged += OnSessionDetailChanged;
                }
            }
        }
    }

    private void OnSessionDetailChanged(SmtcSession sender, object args)
    {
        try
        {
            // Playback moving to another app shows up here first.
            if (sender.GetPlaybackInfo()?.PlaybackStatus != SmtcStatus.Playing)
            {
                AttachToBestSession();
                return;
            }
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            AttachToBestSession();
            return;
        }

        _ = PublishAsync();
    }

    private void DetachCurrentSession()
    {
        if (_session is null)
        {
            return;
        }

        try
        {
            _session.MediaPropertiesChanged -= OnSessionDetailChanged;
            _session.PlaybackInfoChanged -= OnSessionDetailChanged;
            _session.TimelinePropertiesChanged -= OnSessionDetailChanged;
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            // The owning app already went away; nothing left to unsubscribe from.
        }

        _session = null;
    }

    private async Task PublishAsync()
    {
        var info = await ReadAsync().ConfigureAwait(false);

        if (info is not null)
        {
            CancelPendingEmpty();
            Current = info;
            SessionChanged?.Invoke(this, info);
            return;
        }

        // Already empty: nothing to smooth over.
        if (Current is null)
        {
            SessionChanged?.Invoke(this, null);
            return;
        }

        // Going from "playing" to "nothing" is usually just a track change in progress.
        // Hold the previous track on screen briefly and confirm before believing it.
        CancelPendingEmpty();

        var cts = new CancellationTokenSource();
        _pendingEmpty = cts;

        try
        {
            await Task.Delay(EmptyStateGrace, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // The session may have been replaced rather than removed, so look again.
        ResolveAndAttach();
        var confirmed = await ReadAsync().ConfigureAwait(false);

        if (cts.IsCancellationRequested)
        {
            return;
        }

        Current = confirmed;
        SessionChanged?.Invoke(this, confirmed);
    }

    private void CancelPendingEmpty()
    {
        var pending = Interlocked.Exchange(ref _pendingEmpty, null);
        if (pending is null)
        {
            return;
        }

        pending.Cancel();
        pending.Dispose();
    }

    /// <summary>Reads a snapshot, tolerating every field being absent.</summary>
    private async Task<MediaSessionInfo?> ReadAsync()
    {
        var session = _session;
        if (session is null)
        {
            return null;
        }

        try
        {
            var properties = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();

            var status = playback?.PlaybackStatus ?? SmtcStatus.Closed;
            if (status == SmtcStatus.Closed)
            {
                return null;
            }

            var state = status switch
            {
                SmtcStatus.Playing => PlaybackState.Playing,
                SmtcStatus.Paused => PlaybackState.Paused,
                SmtcStatus.Stopped => PlaybackState.Stopped,
                _ => PlaybackState.Unknown,
            };

            var duration = timeline.EndTime - timeline.StartTime;
            var position = timeline.Position - timeline.StartTime;

            // The reported position is a snapshot taken at LastUpdatedTime, so advance it
            // ourselves rather than asking the session again every second.
            if (state == PlaybackState.Playing && timeline.LastUpdatedTime > DateTimeOffset.MinValue)
            {
                position += DateTimeOffset.Now - timeline.LastUpdatedTime;
            }

            if (position < TimeSpan.Zero)
            {
                position = TimeSpan.Zero;
            }

            if (duration > TimeSpan.Zero && position > duration)
            {
                position = duration;
            }

            var controls = playback?.Controls;

            return new MediaSessionInfo
            {
                Title = Fallback(properties?.Title, "Unknown"),
                Artist = Fallback(properties?.Artist, Fallback(properties?.AlbumArtist, "Unknown")),
                Album = string.IsNullOrWhiteSpace(properties?.AlbumTitle) ? null : properties.AlbumTitle,
                SourceApp = FriendlyAppName(session.SourceAppUserModelId),
                State = state,
                Position = position,
                Duration = duration > TimeSpan.Zero ? duration : TimeSpan.Zero,
                Thumbnail = properties?.Thumbnail,
                CanPlay = controls?.IsPlayEnabled ?? false,
                CanPause = controls?.IsPauseEnabled ?? false,
                CanGoNext = controls?.IsNextEnabled ?? false,
                CanGoPrevious = controls?.IsPreviousEnabled ?? false,
                CanSeek = controls?.IsPlaybackPositionEnabled ?? false,
            };
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            // The media app closed mid-read. Report nothing playing rather than crash.
            return null;
        }
    }

    private static string Fallback(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    /// <summary>Turns an app user model id into something worth showing a person.</summary>
    private static string FriendlyAppName(string? appUserModelId)
    {
        if (string.IsNullOrWhiteSpace(appUserModelId))
        {
            return "Unknown";
        }

        var name = appUserModelId;

        // Packaged apps report Publisher.Name_hash!App; desktop apps report an exe.
        int bang = name.IndexOf('!');
        if (bang >= 0)
        {
            name = name[..bang];
        }

        int underscore = name.LastIndexOf('_');
        if (underscore > 0)
        {
            name = name[..underscore];
        }

        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        int lastDot = name.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < name.Length - 1)
        {
            name = name[(lastDot + 1)..];
        }

        if (name.Length == 0)
        {
            return "Unknown";
        }

        return KnownApps.TryGetValue(name, out var friendly)
            ? friendly
            : char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>
    /// SMTC surfaces a dead session as a COM failure rather than a null, so treat these
    /// as "the app went away" instead of letting them reach the user.
    /// </summary>
    private static bool IsTransient(Exception ex) =>
        ex is System.Runtime.InteropServices.COMException
            or ObjectDisposedException
            or InvalidOperationException
            or UnauthorizedAccessException
            or TimeoutException;

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CancelPendingEmpty();
            DetachCurrentSession();

            if (_manager is not null)
            {
                _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _manager.SessionsChanged -= OnSessionsChanged;
                _manager = null;
            }
        }
    }

    // --- Transport controls -------------------------------------------------
    // Each returns false when the active session refuses or has gone away, so the UI
    // never claims an action worked when it did not.

    public Task<bool> TogglePlayPauseAsync() => InvokeAsync(s => s.TryTogglePlayPauseAsync());

    public Task<bool> NextAsync() => InvokeAsync(s => s.TrySkipNextAsync());

    public Task<bool> PreviousAsync() => InvokeAsync(s => s.TrySkipPreviousAsync());

    public Task<bool> SeekAsync(TimeSpan position) =>
        InvokeAsync(s => s.TryChangePlaybackPositionAsync(position.Ticks));

    private async Task<bool> InvokeAsync(Func<SmtcSession, IAsyncOperation<bool>> action)
    {
        var session = _session;
        if (session is null)
        {
            return false;
        }

        try
        {
            return await action(session);
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            return false;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentMusic.Models;
using FluentMusic.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentMusic.ViewModels;

/// <summary>
/// Bindable state for the player panel. Owns the marshalling from the media service's
/// background callbacks onto the UI thread, and interpolates playback position between
/// the relatively infrequent updates the session gives us.
/// </summary>
public sealed class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly MediaSessionService _media;
    private readonly DispatcherQueue _dispatcher;
    private readonly DispatcherQueueTimer _tick;

    private MediaSessionInfo? _session;
    private string _loadedArtworkKey = string.Empty;
    private bool _isScrubbing;
    private bool _artworkLoading;
    private bool _disposed;

    public PlayerViewModel(MediaSessionService media, DispatcherQueue dispatcher)
    {
        _media = media;
        _dispatcher = dispatcher;

        _tick = dispatcher.CreateTimer();
        _tick.Interval = TimeSpan.FromMilliseconds(500);
        _tick.IsRepeating = true;
        _tick.Tick += (_, _) => AdvancePosition();

        PlayPauseCommand = new AsyncRelayCommand(() => _media.TogglePlayPauseAsync());
        NextCommand = new AsyncRelayCommand(() => _media.NextAsync(), () => CanGoNext);
        PreviousCommand = new AsyncRelayCommand(() => _media.PreviousAsync(), () => CanGoPrevious);

        _media.SessionChanged += OnSessionChanged;
        Apply(_media.Current);
    }

    public AsyncRelayCommand PlayPauseCommand { get; }

    public AsyncRelayCommand NextCommand { get; }

    public AsyncRelayCommand PreviousCommand { get; }

    // --- Displayed state ----------------------------------------------------

    private bool _hasSession;
    public bool HasSession
    {
        get => _hasSession;
        private set
        {
            if (SetProperty(ref _hasSession, value))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public bool IsEmpty => !HasSession;

    private string _title = "Nothing playing";
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    private string _artist = string.Empty;
    public string Artist
    {
        get => _artist;
        private set => SetProperty(ref _artist, value);
    }

    private string _sourceApp = string.Empty;
    public string SourceApp
    {
        get => _sourceApp;
        private set => SetProperty(ref _sourceApp, value);
    }

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                OnPropertyChanged(nameof(PlayPauseGlyph));
                OnPropertyChanged(nameof(PlayPauseTooltip));
                UpdateTicking();
            }
        }
    }

    /// <summary>Segoe Fluent Icons: pause when playing, play when not.</summary>
    public string PlayPauseGlyph => IsPlaying ? "" : "";

    public string PlayPauseTooltip => IsPlaying ? "Pause" : "Play";

    private BitmapImage? _artwork;
    public BitmapImage? Artwork
    {
        get => _artwork;
        private set
        {
            if (SetProperty(ref _artwork, value))
            {
                OnPropertyChanged(nameof(HasArtwork));
                OnPropertyChanged(nameof(NoArtwork));
            }
        }
    }

    public bool HasArtwork => Artwork is not null;

    public bool NoArtwork => Artwork is null;

    private bool _canGoNext;
    public bool CanGoNext
    {
        get => _canGoNext;
        private set
        {
            if (SetProperty(ref _canGoNext, value))
            {
                NextCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private bool _canGoPrevious;
    public bool CanGoPrevious
    {
        get => _canGoPrevious;
        private set
        {
            if (SetProperty(ref _canGoPrevious, value))
            {
                PreviousCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private bool _canSeek;
    public bool CanSeek
    {
        get => _canSeek;
        private set => SetProperty(ref _canSeek, value);
    }

    private bool _hasTimeline;
    public bool HasTimeline
    {
        get => _hasTimeline;
        private set => SetProperty(ref _hasTimeline, value);
    }

    private TimeSpan _position;
    public TimeSpan Position
    {
        get => _position;
        private set
        {
            if (SetProperty(ref _position, value))
            {
                OnPropertyChanged(nameof(PositionSeconds));
                OnPropertyChanged(nameof(PositionText));
            }
        }
    }

    private TimeSpan _duration;
    public TimeSpan Duration
    {
        get => _duration;
        private set
        {
            if (SetProperty(ref _duration, value))
            {
                OnPropertyChanged(nameof(DurationSeconds));
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    public double PositionSeconds => Position.TotalSeconds;

    public double DurationSeconds => Duration.TotalSeconds > 0 ? Duration.TotalSeconds : 1;

    public string PositionText => Format(Position);

    public string DurationText => Format(Duration);

    private static string Format(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            value = TimeSpan.Zero;
        }

        return value.TotalHours >= 1
            ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
            : $"{value.Minutes}:{value.Seconds:00}";
    }

    // --- Wiring -------------------------------------------------------------

    private void OnSessionChanged(object? sender, MediaSessionInfo? info)
    {
        // Media callbacks arrive on a pool thread; everything below touches UI state.
        if (_dispatcher.HasThreadAccess)
        {
            Apply(info);
        }
        else
        {
            _dispatcher.TryEnqueue(() => Apply(info));
        }
    }

    private void Apply(MediaSessionInfo? info)
    {
        if (_disposed)
        {
            return;
        }

        _session = info;

        if (info is null)
        {
            HasSession = false;
            Title = "Nothing playing";
            Artist = string.Empty;
            SourceApp = string.Empty;
            IsPlaying = false;
            CanGoNext = false;
            CanGoPrevious = false;
            CanSeek = false;
            HasTimeline = false;
            Position = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
            Artwork = null;
            _loadedArtworkKey = string.Empty;
            UpdateTicking();
            return;
        }

        HasSession = true;
        Title = info.Title;
        Artist = info.Artist;
        SourceApp = info.SourceApp;
        CanGoNext = info.CanGoNext;
        CanGoPrevious = info.CanGoPrevious;
        CanSeek = info.CanSeek;
        HasTimeline = info.HasTimeline;
        Duration = info.Duration;

        if (!_isScrubbing)
        {
            Position = info.Position;
        }

        IsPlaying = info.IsPlaying;

        // Refetch when the track changed, and also when this track still has no artwork
        // but now offers a thumbnail: apps routinely publish title and artist a moment
        // before the artwork is ready, and that first update must not be treated as
        // "this track has none".
        bool trackChanged = info.TrackKey != _loadedArtworkKey;
        bool artworkArrivedLate = !trackChanged && Artwork is null && info.Thumbnail is not null;

        if (trackChanged)
        {
            _loadedArtworkKey = info.TrackKey;
            Artwork = null;
        }

        if ((trackChanged || artworkArrivedLate) && !_artworkLoading)
        {
            _ = LoadArtworkAsync(info);
        }
    }

    private async Task LoadArtworkAsync(MediaSessionInfo info)
    {
        if (info.Thumbnail is null)
        {
            // Not "this track has no artwork" — just not published yet. Leave the key
            // alone so the next update retries.
            return;
        }

        _artworkLoading = true;

        try
        {
            using (var stream = await info.Thumbnail.OpenReadAsync())
            {
                // The track may have changed again while the thumbnail was opening.
                if (_disposed || info.TrackKey != _loadedArtworkKey)
                {
                    return;
                }

                var image = new BitmapImage();
                await image.SetSourceAsync(stream);

                if (!_disposed && info.TrackKey == _loadedArtworkKey)
                {
                    Artwork = image;
                }
            }
        }
        catch (Exception ex) when (IsArtworkFailure(ex))
        {
            // No artwork is a normal state, not a failure worth surfacing.
            Artwork = null;
            return;
        }
        finally
        {
            _artworkLoading = false;
        }
    }

    private static bool IsArtworkFailure(Exception ex) =>
        ex is System.Runtime.InteropServices.COMException
            or ObjectDisposedException
            or InvalidOperationException
            or OperationCanceledException
            or UnauthorizedAccessException
            or IOException
            or ArgumentException;

    /// <summary>Advances the displayed position between session updates.</summary>
    private void AdvancePosition()
    {
        if (_session is null || !_session.IsPlaying || _isScrubbing)
        {
            return;
        }

        var next = Position + TimeSpan.FromMilliseconds(500);
        Position = Duration > TimeSpan.Zero && next > Duration ? Duration : next;
    }

    /// <summary>The timer only runs while the panel is visible and something is playing.</summary>
    private bool _isVisible;

    public void SetPanelVisible(bool visible)
    {
        _isVisible = visible;
        UpdateTicking();
    }

    private void UpdateTicking()
    {
        if (_isVisible && IsPlaying && HasTimeline)
        {
            if (!_tick.IsRunning)
            {
                _tick.Start();
            }
        }
        else if (_tick.IsRunning)
        {
            _tick.Stop();
        }
    }

    // --- Scrubbing ----------------------------------------------------------

    public void BeginScrub() => _isScrubbing = true;

    public void ScrubTo(double seconds) => Position = TimeSpan.FromSeconds(seconds);

    public async Task EndScrubAsync(double seconds)
    {
        _isScrubbing = false;

        if (CanSeek)
        {
            await _media.SeekAsync(TimeSpan.FromSeconds(seconds));
        }
        else
        {
            // Put the thumb back where playback actually is.
            Position = _session?.Position ?? TimeSpan.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tick.Stop();
        _media.SessionChanged -= OnSessionChanged;
    }
}

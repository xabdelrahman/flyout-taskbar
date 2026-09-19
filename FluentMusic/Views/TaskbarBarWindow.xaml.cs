using FluentMusic.Interop;
using FluentMusic.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace FluentMusic.Views;

/// <summary>
/// A slim bar that sits inside the taskbar's strip, in the empty run between the pinned
/// icons and the tray.
///
/// It is a separate always-on-top window placed over that region and painted to match the
/// taskbar, not content injected into explorer.exe. Windows 11 removed deskbands and
/// offers no API to put content in the taskbar itself, so injection is the only literal
/// route — and it is fragile enough to take Explorer down with it. This looks the same,
/// follows the taskbar as it moves or resizes, and cannot be broken by a Windows update.
/// </summary>
public sealed partial class TaskbarBarWindow : Window
{
    private const int WidthDips = 400;

    /// <summary>Inset from the top and bottom of the taskbar strip.</summary>
    private const int VerticalInsetDips = 3;

    /// <summary>Gap between the bar and the tray icons to its right.</summary>
    private const int TrayGapDips = 8;

    /// <summary>Used only when the taskbar cannot be located.</summary>
    private const int FallbackHeightDips = 46;

    private readonly SettingsService _settings;
    private readonly nint _hWnd;
    private readonly TaskbarBarView _view;
    private readonly DispatcherQueueTimer _follow;

    private RectInt32 _lastPlacement;
    private int _dragOriginX;
    private bool _dragging;

    public TaskbarBarWindow(SettingsService settings, MediaSessionService media)
    {
        _settings = settings;

        InitializeComponent();

        _hWnd = WindowNative.GetWindowHandle(this);

        _view = new TaskbarBarView(media, settings.Settings.ShowWaveform);
        _view.DragTo = OnDragTo;
        _view.DragFinished = OnDragFinished;
        Root.Children.Add(_view);

        Title = "Fluent Music";

        // No acrylic: the taskbar already has its own, and blurring a blurred surface
        // reads as a smear. A flat matching fill is what makes it look embedded.
        ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
        }

        AppWindow.IsShownInSwitchers = false;
        NativeMethods.MakeToolWindow(_hWnd);
        NativeMethods.SetCornerPreference(_hWnd, NativeMethods.CornerPreference.DoNotRound);

        SnapIntoTaskbar();

        // The taskbar moves when it resizes, the display changes, or icons come and go.
        // Two window-rect reads a second costs far less than a misplaced bar.
        _follow = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _follow.Interval = TimeSpan.FromSeconds(1);
        _follow.IsRepeating = true;
        _follow.Tick += (_, _) => SnapIntoTaskbar();
        _follow.Start();
    }

    public TaskbarBarView View => _view;

    private double Scale => NativeMethods.GetScaleForWindow(_hWnd);

    private int ToPixels(double dips) => (int)Math.Round(dips * Scale);

    /// <summary>
    /// Places the bar in the taskbar strip, right edge just left of the tray, vertically
    /// inset so it reads as part of the bar rather than sitting on top of it.
    /// </summary>
    private void SnapIntoTaskbar()
    {
        // Re-assert every tick, not just when the placement changes: Explorer raises the
        // taskbar whenever it is interacted with, which would otherwise bury the bar.
        NativeMethods.KeepAboveTaskbar(_hWnd);

        var layout = NativeMethods.GetTaskbarLayout();
        RectInt32 placement;

        if (layout.Found && layout.Taskbar.Height > 0)
        {
            int inset = ToPixels(VerticalInsetDips);
            int height = Math.Max(ToPixels(24), layout.Taskbar.Height - (inset * 2));
            int width = ToPixels(WidthDips);

            int y = layout.Taskbar.Y + ((layout.Taskbar.Height - height) / 2);
            int x = _dragging
                ? _lastPlacement.X
                : ClampToTaskbar(
                    layout.Taskbar.X + (int)(layout.Taskbar.Width * _settings.Settings.TaskbarBarXFraction),
                    width,
                    layout);

            placement = new RectInt32(x, y, width, height);
        }
        else
        {
            // Taskbar hidden or replaced: fall back to the bottom-right of the work area
            // so the bar is still usable rather than stranded off screen.
            var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
            int width = ToPixels(WidthDips);
            int height = ToPixels(FallbackHeightDips);

            placement = new RectInt32(
                work.X + work.Width - width - ToPixels(10),
                work.Y + work.Height - height - ToPixels(10),
                width,
                height);
        }

        if (placement.X == _lastPlacement.X &&
            placement.Y == _lastPlacement.Y &&
            placement.Width == _lastPlacement.Width &&
            placement.Height == _lastPlacement.Height)
        {
            return;
        }

        _lastPlacement = placement;
        AppWindow.MoveAndResize(placement);

        if (layout.Found)
        {
            MatchTaskbarColour(layout.Taskbar, placement);
        }
    }

    /// <summary>Keeps the bar within the taskbar and clear of the tray icons.</summary>
    private int ClampToTaskbar(int x, int width, NativeMethods.TaskbarLayout layout)
    {
        int min = layout.Taskbar.X;
        int max = layout.TrayArea.X - ToPixels(TrayGapDips) - width;
        return max < min ? min : Math.Clamp(x, min, max);
    }

    private void OnDragTo(int deltaX)
    {
        var layout = NativeMethods.GetTaskbarLayout();
        if (!layout.Found)
        {
            return;
        }

        if (!_dragging)
        {
            _dragging = true;
            _dragOriginX = _lastPlacement.X;
        }

        int x = ClampToTaskbar(_dragOriginX + deltaX, _lastPlacement.Width, layout);
        _lastPlacement = new RectInt32(x, _lastPlacement.Y, _lastPlacement.Width, _lastPlacement.Height);
        AppWindow.Move(new PointInt32(x, _lastPlacement.Y));
    }

    private void OnDragFinished()
    {
        _dragging = false;

        var layout = NativeMethods.GetTaskbarLayout();
        if (!layout.Found || layout.Taskbar.Width == 0)
        {
            return;
        }

        _settings.Settings.TaskbarBarXFraction =
            (double)(_lastPlacement.X - layout.Taskbar.X) / layout.Taskbar.Width;
        _settings.Save();
    }

    /// <summary>
    /// Paints the bar the same colour as the taskbar it sits in, sampled from a point
    /// well clear of our own window so we never sample ourselves.
    /// </summary>
    private void MatchTaskbarColour(RectInt32 taskbar, RectInt32 placement)
    {
        int sampleX = Math.Max(taskbar.X + 2, placement.X - ToPixels(24));
        int sampleY = taskbar.Y + (taskbar.Height / 2);

        var sample = NativeMethods.SampleScreenPixel(sampleX, sampleY);
        if (sample is null)
        {
            return;
        }

        _view.SetSurfaceColour(
            Microsoft.UI.ColorHelper.FromArgb(0xFF, sample.Value.R, sample.Value.G, sample.Value.B));
    }

    public void SetWaveformEnabled(bool enabled) => _view.SetWaveformEnabled(enabled);

    /// <summary>Stops the follow timer and the waveform before the window goes away.</summary>
    public void Teardown()
    {
        _follow.Stop();
        _view.Teardown();
    }
}

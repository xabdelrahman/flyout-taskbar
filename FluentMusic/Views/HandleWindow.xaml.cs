using FluentMusic.Controls;
using FluentMusic.Interop;
using FluentMusic.Models;
using FluentMusic.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;

namespace FluentMusic.Views;

/// <summary>
/// The always-visible tab tucked against a screen edge. Drag it to reposition, click
/// it to fly the player out.
/// </summary>
public sealed partial class HandleWindow : Window
{
    private const int WidthDips = 28;
    private const int HeightDips = 66;

    /// <summary>How far the pointer must travel before a click becomes a drag.</summary>
    private const double DragThresholdDips = 4;

    private readonly SettingsService _settings;
    private readonly nint _hWnd;
    private readonly PlayerWindow _player;

    private bool _isDragging;
    private bool _passedDragThreshold;
    private PointInt32 _cursorAtDragStart;
    private PointInt32 _windowAtDragStart;

    public HandleWindow(SettingsService settings, MediaSessionService media)
    {
        _settings = settings;

        InitializeComponent();

        _hWnd = WindowNative.GetWindowHandle(this);

        Title = "Fluent Music";
        // Thin acrylic reads as real glass; the default kind is nearly opaque.
        SystemBackdrop = new ThinAcrylicBackdrop();

        ExtendsContentIntoTitleBar = true;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
        }

        AppWindow.IsShownInSwitchers = false;
        NativeMethods.MakeToolWindow(_hWnd);
        NativeMethods.SetCornerPreference(_hWnd, NativeMethods.CornerPreference.RoundSmall);

        _player = new PlayerWindow(media);
        _player.SetOwner(_hWnd);

        AppWindow.Resize(new SizeInt32(ToPixels(WidthDips), ToPixels(HeightDips)));
        MoveToSavedPosition();
    }

    private double Scale => NativeMethods.GetScaleForWindow(_hWnd);

    private int ToPixels(double dips) => (int)Math.Round(dips * Scale);

    private RectInt32 WorkArea =>
        DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;

    /// <summary>Places the handle flush against its saved edge, at its saved height.</summary>
    private void MoveToSavedPosition()
    {
        var work = WorkArea;
        int width = AppWindow.Size.Width;
        int height = AppWindow.Size.Height;

        int x = _settings.Settings.HandleEdge == ScreenEdge.Left
            ? work.X
            : work.X + work.Width - width;

        int y = work.Y + (int)(work.Height * _settings.Settings.HandleYFraction);
        y = Math.Clamp(y, work.Y, work.Y + work.Height - height);

        AppWindow.Move(new PointInt32(x, y));
        ApplyEdgeShape();
    }

    /// <summary>Rounds only the corners facing away from the edge, so the tab hugs it.</summary>
    private void ApplyEdgeShape()
    {
        Pill.CornerRadius = _settings.Settings.HandleEdge == ScreenEdge.Left
            ? new CornerRadius(0, 9, 9, 0)
            : new CornerRadius(9, 0, 0, 9);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _passedDragThreshold = false;
        _cursorAtDragStart = NativeMethods.GetCursorPosition();
        _windowAtDragStart = AppWindow.Position;
        Root.CapturePointer(e.Pointer);
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var cursor = NativeMethods.GetCursorPosition();
        int dx = cursor.X - _cursorAtDragStart.X;
        int dy = cursor.Y - _cursorAtDragStart.Y;

        if (!_passedDragThreshold &&
            Math.Sqrt((dx * dx) + (dy * dy)) < DragThresholdDips * Scale)
        {
            return;
        }

        _passedDragThreshold = true;
        _player.Hide();

        // Free movement while dragging; the snap to an edge happens on release.
        AppWindow.Move(new PointInt32(_windowAtDragStart.X + dx, _windowAtDragStart.Y + dy));
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        Root.ReleasePointerCapture(e.Pointer);

        if (_passedDragThreshold)
        {
            SnapToNearestEdge();
        }
        else
        {
            TogglePlayer();
        }
    }

    /// <summary>Tucks the handle against whichever vertical edge it was dropped nearest.</summary>
    private void SnapToNearestEdge()
    {
        var work = WorkArea;
        int width = AppWindow.Size.Width;
        int height = AppWindow.Size.Height;

        int centreX = AppWindow.Position.X + (width / 2);
        var edge = centreX < work.X + (work.Width / 2) ? ScreenEdge.Left : ScreenEdge.Right;

        int y = Math.Clamp(AppWindow.Position.Y, work.Y, work.Y + work.Height - height);

        _settings.Settings.HandleEdge = edge;
        _settings.Settings.HandleYFraction = work.Height == 0
            ? 0.35
            : (double)(y - work.Y) / work.Height;
        _settings.Save();

        int x = edge == ScreenEdge.Left ? work.X : work.X + work.Width - width;
        AppWindow.Move(new PointInt32(x, y));
        ApplyEdgeShape();
    }

    private void TogglePlayer()
    {
        if (_player.IsPanelVisible)
        {
            _player.Hide();
        }
        else
        {
            _player.ShowBesideHandle(AppWindow.Position, AppWindow.Size, WorkArea, _settings.Settings.HandleEdge);
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) =>
        ((App)Application.Current).ShowSettings();

    private void OnExitClick(object sender, RoutedEventArgs e) => Application.Current.Exit();

    /// <summary>Closes the player panel before this window goes away.</summary>
    public void Teardown()
    {
        _player.Hide();
        _player.Close();
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        PillScale.ScaleX = 1.08;
        Glyph.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"];
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        PillScale.ScaleX = 1.0;
        Glyph.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
    }
}

using FluentMusic.Controls;
using FluentMusic.Interop;
using FluentMusic.Models;
using FluentMusic.Services;
using FluentMusic.ViewModels;
using Microsoft.UI.Composition;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;

namespace FluentMusic.Views;

/// <summary>
/// The player panel that flies out from the handle. Behaves like a flyout rather than
/// a window: no chrome, hides on Escape or when focus moves elsewhere.
/// </summary>
public sealed partial class PlayerWindow : Window
{
    private const int WidthDips = 300;
    private const int HeightDips = 404;

    /// <summary>Breathing room between the handle and the panel.</summary>
    private const int GapDips = 8;

    private readonly nint _hWnd;
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _dismissTimer;

    /// <summary>The handle window, which is allowed to hold focus without dismissing us.</summary>
    private nint _ownerHWnd;

    /// <summary>
    /// Tracked explicitly rather than read from AppWindow.IsVisible, which reports true
    /// for a window that has been constructed but never shown — that made the first
    /// click on the handle hide an already-hidden panel instead of opening it.
    /// </summary>
    public bool IsPanelVisible { get; private set; }

    private readonly PlayerView _view;

    public PlayerWindow(MediaSessionService media)
    {
        InitializeComponent();

        _view = new PlayerView(media);
        Root.Children.Add(_view);

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
        NativeMethods.SetCornerPreference(_hWnd, NativeMethods.CornerPreference.Round);

        AppWindow.Resize(new SizeInt32(ToPixels(WidthDips), ToPixels(HeightDips)));
        AppWindow.Hide();

        // Focus bounces between the handle and this panel while opening, so confirm
        // where focus actually landed before dismissing.
        _dismissTimer = DispatcherQueue.CreateTimer();
        _dismissTimer.Interval = TimeSpan.FromMilliseconds(150);
        _dismissTimer.IsRepeating = false;
        _dismissTimer.Tick += OnDismissTick;

        Activated += OnActivated;
    }

    /// <summary>Tells the panel which window counts as "still ours" for focus purposes.</summary>
    public void SetOwner(nint ownerHWnd) => _ownerHWnd = ownerHWnd;

    private double Scale => NativeMethods.GetScaleForWindow(_hWnd);

    private int ToPixels(double dips) => (int)Math.Round(dips * Scale);

    /// <summary>
    /// Positions the panel next to the handle, on the inward side of whichever edge the
    /// handle is docked to, and animates it out from that edge.
    /// </summary>
    public void ShowBesideHandle(PointInt32 handlePosition, SizeInt32 handleSize, RectInt32 workArea, ScreenEdge edge)
    {
        // Re-measure in case the handle was dragged to a monitor with a different DPI.
        int width = ToPixels(WidthDips);
        int height = ToPixels(HeightDips);
        AppWindow.Resize(new SizeInt32(width, height));

        int gap = ToPixels(GapDips);

        int x = edge == ScreenEdge.Left
            ? handlePosition.X + handleSize.Width + gap
            : handlePosition.X - width - gap;

        // Centre on the handle, then keep the whole panel inside the work area.
        int y = handlePosition.Y + (handleSize.Height / 2) - (height / 2);
        y = Math.Clamp(y, workArea.Y, workArea.Y + workArea.Height - height);

        AppWindow.Move(new PointInt32(x, y));
        AppWindow.Show();
        Activate();

        IsPanelVisible = true;
        _view.ViewModel.SetPanelVisible(true);
        PlayFlyOutAnimation(edge);
    }

    public void Hide()
    {
        IsPanelVisible = false;
        _dismissTimer.Stop();
        _view.ViewModel.SetPanelVisible(false);
        AppWindow.Hide();
    }

    /// <summary>
    /// Opening motion from the spec: fade up, settle in from slightly small, and drift
    /// in from the docked edge. Short and eased so it reads as the panel arriving rather
    /// than as an effect.
    /// </summary>
    private void PlayFlyOutAnimation(ScreenEdge edge)
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = new Duration(TimeSpan.FromMilliseconds(200));
        var storyboard = new Storyboard();

        var slide = new DoubleAnimation
        {
            From = edge == ScreenEdge.Left ? -14 : 14,
            To = 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(slide, SlideTransform);
        Storyboard.SetTargetProperty(slide, "X");
        storyboard.Children.Add(slide);

        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration,
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, Root);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        foreach (var axis in new[] { "ScaleX", "ScaleY" })
        {
            var scale = new DoubleAnimation
            {
                From = 0.96,
                To = 1,
                Duration = duration,
                EasingFunction = easing,
            };
            Storyboard.SetTarget(scale, OpenScale);
            Storyboard.SetTargetProperty(scale, axis);
            storyboard.Children.Add(scale);
        }

        storyboard.Begin();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        // Clicking away dismisses the panel, the way a flyout behaves.
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            _dismissTimer.Start();
        }
        else
        {
            _dismissTimer.Stop();
        }
    }

    private void OnDismissTick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground != _hWnd && foreground != _ownerHWnd)
        {
            Hide();
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }
}

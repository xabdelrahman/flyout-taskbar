using FluentMusic.Services;
using FluentMusic.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace FluentMusic.Views;

/// <summary>
/// Contents of the taskbar bar: artwork, track, transport and the playing waveform.
/// </summary>
public sealed partial class TaskbarBarView : UserControl
{
    private const int WaveformBars = 13;
    private const double BarWidth = 3;
    private const double BarHeight = 20;

    private readonly List<ScaleTransform> _barTransforms = new();

    private Storyboard? _waveform;
    private bool _waveformEnabled;

    public TaskbarBarView(MediaSessionService media, bool showWaveform)
    {
        ViewModel = new PlayerViewModel(
            media,
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
        _waveformEnabled = showWaveform;

        InitializeComponent();

        BuildWaveform();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += (_, _) => ViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        ViewModel.SetPanelVisible(true);
        UpdateWaveform();
    }

    public PlayerViewModel ViewModel { get; }

    /// <summary>Called with a horizontal delta in physical pixels while dragging.</summary>
    public Action<int>? DragTo { get; set; }

    /// <summary>Called once the drag finishes, so the position can be saved.</summary>
    public Action? DragFinished { get; set; }

    private bool _dragging;
    private int _cursorAtDragStart;

    private void OnSurfacePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Buttons mark their own pointer events handled, so this only fires on the
        // artwork and track area — the part that behaves like a grab handle.
        _dragging = true;
        _cursorAtDragStart = Interop.NativeMethods.GetCursorPosition().X;
        Surface.CapturePointer(e.Pointer);
    }

    private void OnSurfacePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        int cursor = Interop.NativeMethods.GetCursorPosition().X;
        DragTo?.Invoke(cursor - _cursorAtDragStart);
    }

    private void OnSurfacePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        Surface.ReleasePointerCapture(e.Pointer);
        DragFinished?.Invoke();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.IsPlaying))
        {
            UpdateWaveform();
        }
    }

    private void BuildWaveform()
    {
        // Amber, matching the playing indicator Windows shows in its own media flyout.
        var fill = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xF0, 0xA8, 0x68));

        for (int i = 0; i < WaveformBars; i++)
        {
            var transform = new ScaleTransform { ScaleX = 1, ScaleY = 0.28, CenterY = BarHeight / 2 };

            var bar = new Rectangle
            {
                Width = BarWidth,
                Height = BarHeight,
                RadiusX = BarWidth / 2,
                RadiusY = BarWidth / 2,
                Fill = fill,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransform = transform,
            };

            _barTransforms.Add(transform);
            Waveform.Children.Add(bar);
        }
    }

    public void SetWaveformEnabled(bool enabled)
    {
        _waveformEnabled = enabled;
        UpdateWaveform();
    }

    private void UpdateWaveform()
    {
        if (_waveformEnabled && ViewModel.IsPlaying)
        {
            StartWaveform();
        }
        else
        {
            StopWaveform();
        }

        Waveform.Visibility = _waveformEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Animates ScaleY rather than Height: a scale runs on the compositor, so thirteen
    /// bars cost close to nothing, where animating height would relayout every frame.
    /// </summary>
    private void StartWaveform()
    {
        if (_waveform is not null)
        {
            return;
        }

        var storyboard = new Storyboard();

        // Fixed seed so the pattern is the same every run rather than randomly ugly.
        var random = new Random(7);

        foreach (var transform in _barTransforms)
        {
            var animation = new DoubleAnimation
            {
                From = 0.22,
                To = 0.55 + (random.NextDouble() * 0.45),
                Duration = new Duration(TimeSpan.FromMilliseconds(420 + random.Next(0, 460))),
                BeginTime = TimeSpan.FromMilliseconds(random.Next(0, 380)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };

            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, "ScaleY");
            storyboard.Children.Add(animation);
        }

        _waveform = storyboard;
        storyboard.Begin();
    }

    private void StopWaveform()
    {
        if (_waveform is null)
        {
            return;
        }

        _waveform.Stop();
        _waveform = null;

        // Settle to a flat, dim line while paused.
        foreach (var transform in _barTransforms)
        {
            transform.ScaleY = 0.28;
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) =>
        ((App)Application.Current).ShowSettings();

    private void OnExitClick(object sender, RoutedEventArgs e) => Application.Current.Exit();

    /// <summary>Repaints the bar to match the taskbar behind it.</summary>
    public void SetSurfaceColour(Windows.UI.Color colour)
    {
        if (Surface.Background is SolidColorBrush existing && existing.Color == colour)
        {
            return;
        }

        Surface.Background = new SolidColorBrush(colour);
    }

    public void Teardown()
    {
        StopWaveform();
        ViewModel.Dispose();
    }
}

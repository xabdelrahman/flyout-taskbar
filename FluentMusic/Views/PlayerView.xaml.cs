using System.ComponentModel;
using FluentMusic.Services;
using FluentMusic.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace FluentMusic.Views;

public sealed partial class PlayerView : UserControl
{
    // Short and soft: motion should register without being noticed (spec section 11).
    private static readonly TimeSpan TrackChangeDuration = TimeSpan.FromMilliseconds(260);
    private static readonly TimeSpan HoverDuration = TimeSpan.FromMilliseconds(120);

    public PlayerView(MediaSessionService media)
    {
        // Qualified because UserControl exposes an instance DispatcherQueue property.
        ViewModel = new PlayerViewModel(
            media,
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());

        InitializeComponent();

        // Handled events still need to reach us: the Slider marks pointer input as
        // handled internally, so subscribe with handledEventsToo.
        ProgressSlider.AddHandler(PointerPressedEvent, new PointerEventHandler(OnScrubStart), true);
        ProgressSlider.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnScrubEnd), true);
        ProgressSlider.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnScrubEnd), true);

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Unloaded += (_, _) => ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    public PlayerViewModel ViewModel { get; }

    private void OnScrubStart(object sender, PointerRoutedEventArgs e) => ViewModel.BeginScrub();

    private async void OnScrubEnd(object sender, PointerRoutedEventArgs e) =>
        await ViewModel.EndScrubAsync(ProgressSlider.Value);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PlayerViewModel.Artwork):
                AnimateArtworkChange();
                break;
        }
    }

    /// <summary>New artwork fades up and settles in from very slightly small.</summary>
    private void AnimateArtworkChange()
    {
        if (ViewModel.Artwork is null)
        {
            return;
        }

        var storyboard = new Storyboard();
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TrackChangeDuration),
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, ArtworkCard);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        foreach (var axis in new[] { "ScaleX", "ScaleY" })
        {
            var scale = new DoubleAnimation
            {
                From = 0.94,
                To = 1,
                Duration = new Duration(TrackChangeDuration),
                EasingFunction = easing,
            };
            Storyboard.SetTarget(scale, ArtworkScale);
            Storyboard.SetTargetProperty(scale, axis);
            storyboard.Children.Add(scale);
        }

        storyboard.Begin();
    }

    // --- Hover ---------------------------------------------------------------

    private void OnButtonPointerEntered(object sender, PointerRoutedEventArgs e) =>
        AnimateButtonScale(sender as Button, 1.08);

    private void OnButtonPointerExited(object sender, PointerRoutedEventArgs e) =>
        AnimateButtonScale(sender as Button, 1.0);

    /// <summary>
    /// Buttons grow very slightly under the pointer. The transform is created on demand
    /// so the XAML stays free of per-button boilerplate.
    /// </summary>
    private static void AnimateButtonScale(Button? button, double to)
    {
        if (button is null)
        {
            return;
        }

        if (button.RenderTransform is not ScaleTransform transform)
        {
            transform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
            button.RenderTransform = transform;
            button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        }

        var storyboard = new Storyboard();

        foreach (var axis in new[] { "ScaleX", "ScaleY" })
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = new Duration(HoverDuration),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            };
            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, axis);
            storyboard.Children.Add(animation);
        }

        storyboard.Begin();
    }
}

using FluentMusic.Controls;
using FluentMusic.Interop;
using FluentMusic.Models;
using FluentMusic.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using WinRT.Interop;

namespace FluentMusic.Views;

/// <summary>Small settings window: chooses where the player lives.</summary>
public sealed partial class SettingsWindow : Window
{
    private const int WidthDips = 420;
    private const int HeightDips = 460;

    private readonly SettingsService _settings;
    private readonly nint _hWnd;

    /// <summary>Suppresses change events while the controls are being seeded.</summary>
    private bool _loading;

    public SettingsWindow(SettingsService settings)
    {
        _settings = settings;

        InitializeComponent();

        _hWnd = WindowNative.GetWindowHandle(this);

        Title = "Fluent Music Settings";
        SystemBackdrop = new ThinAcrylicBackdrop();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarArea);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        NativeMethods.SetCornerPreference(_hWnd, NativeMethods.CornerPreference.Round);

        double scale = NativeMethods.GetScaleForWindow(_hWnd);
        AppWindow.Resize(new SizeInt32((int)(WidthDips * scale), (int)(HeightDips * scale)));

        LoadSettings();
    }

    /// <summary>Raised when the mode changes, so the app can swap surfaces.</summary>
    public event EventHandler<PlayerMode>? ModeChanged;

    /// <summary>Raised when the waveform toggle changes.</summary>
    public event EventHandler<bool>? WaveformChanged;

    private void LoadSettings()
    {
        _loading = true;

        EdgeModeOption.IsChecked = _settings.Settings.Mode == PlayerMode.EdgeHandle;
        TaskbarModeOption.IsChecked = _settings.Settings.Mode == PlayerMode.TaskbarBar;
        WaveformToggle.IsOn = _settings.Settings.ShowWaveform;

        _loading = false;
    }

    private void OnModeChecked(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not RadioButton { Tag: string tag })
        {
            return;
        }

        if (!Enum.TryParse<PlayerMode>(tag, out var mode) || mode == _settings.Settings.Mode)
        {
            return;
        }

        _settings.Settings.Mode = mode;
        _settings.Save();
        ModeChanged?.Invoke(this, mode);
    }

    private void OnWaveformToggled(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _settings.Settings.ShowWaveform = WaveformToggle.IsOn;
        _settings.Save();
        WaveformChanged?.Invoke(this, WaveformToggle.IsOn);
    }
}

using FluentMusic.Models;
using FluentMusic.Services;
using FluentMusic.Views;
using Microsoft.UI.Xaml;

namespace FluentMusic;

public partial class App : Application
{
    private SettingsService _settings = null!;
    private MediaSessionService _media = null!;

    private HandleWindow? _handle;
    private TaskbarBarWindow? _bar;
    private SettingsWindow? _settingsWindow;

    public App() => InitializeComponent();

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _settings = new SettingsService();
        _media = new MediaSessionService();

        ShowCurrentMode();

        // Started after a window exists so the first session update has somewhere to land.
        await _media.InitializeAsync();
    }

    /// <summary>Creates whichever surface the settings ask for, tearing down the other.</summary>
    private void ShowCurrentMode()
    {
        if (_settings.Settings.Mode == PlayerMode.TaskbarBar)
        {
            CloseHandle();

            _bar ??= new TaskbarBarWindow(_settings, _media);
            _bar.Activate();
        }
        else
        {
            CloseBar();

            _handle ??= new HandleWindow(_settings, _media);
            _handle.Activate();
        }
    }

    private void CloseHandle()
    {
        if (_handle is null)
        {
            return;
        }

        _handle.Teardown();
        _handle.Close();
        _handle = null;
    }

    private void CloseBar()
    {
        if (_bar is null)
        {
            return;
        }

        _bar.Teardown();
        _bar.Close();
        _bar = null;
    }

    /// <summary>Opens settings, reusing the window if it is already up.</summary>
    public void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var window = new SettingsWindow(_settings);

        window.ModeChanged += (_, _) => ShowCurrentMode();
        window.WaveformChanged += (_, enabled) => _bar?.SetWaveformEnabled(enabled);
        window.Closed += (_, _) => _settingsWindow = null;

        _settingsWindow = window;
        window.Activate();
    }
}

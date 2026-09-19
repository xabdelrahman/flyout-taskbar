namespace FluentMusic.Models;

public enum ScreenEdge
{
    Left,
    Right,
}

/// <summary>Where the player lives on screen.</summary>
public enum PlayerMode
{
    /// <summary>A handle tucked against the left or right screen edge, opening a panel.</summary>
    EdgeHandle,

    /// <summary>A slim bar docked above the taskbar, bottom right.</summary>
    TaskbarBar,
}

public sealed class AppSettings
{
    public PlayerMode Mode { get; set; } = PlayerMode.EdgeHandle;

    /// <summary>Which side of the screen the handle is tucked against.</summary>
    public ScreenEdge HandleEdge { get; set; } = ScreenEdge.Right;

    /// <summary>
    /// Vertical position of the handle as a fraction of the work area height, so the
    /// handle lands in the same relative spot across monitors and resolutions.
    /// </summary>
    public double HandleYFraction { get; set; } = 0.35;

    /// <summary>
    /// Where the bar sits along the taskbar, as a fraction of the taskbar width. Defaults
    /// to the left run, clear of the widgets button and before the centred Start icons.
    /// </summary>
    public double TaskbarBarXFraction { get; set; } = 0.11;

    /// <summary>Animate the waveform on the taskbar bar while something is playing.</summary>
    public bool ShowWaveform { get; set; } = true;
}

using System.Runtime.InteropServices;
using Windows.Graphics;

namespace FluentMusic.Interop;

/// <summary>
/// The handful of Win32 calls WinUI 3 does not surface: a tool-window style so the
/// handle stays out of the taskbar and Alt+Tab, rounded corners, cursor position for
/// dragging, and per-window DPI.
/// </summary>
internal static partial class NativeMethods
{
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;

    // DWMWA_WINDOW_CORNER_PREFERENCE
    private const int DwmwaWindowCornerPreference = 33;

    internal enum CornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3,
    }

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hWnd, int attribute, in int value, int size);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT point);

    [LibraryImport("user32.dll")]
    private static partial uint GetDpiForWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindow(string? className, string? windowName);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindowEx(nint parent, nint childAfter, string? className, string? windowName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint hWnd, out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    /// <summary>The primary taskbar and its notification (tray) area, in screen pixels.</summary>
    internal readonly record struct TaskbarLayout(RectInt32 Taskbar, RectInt32 TrayArea, bool Found);

    /// <summary>
    /// Locates the taskbar and the tray so content can be placed in the empty run
    /// between the pinned icons and the clock. These are long-standing shell window
    /// classes; if either is missing we report Found = false rather than guessing.
    /// </summary>
    internal static TaskbarLayout GetTaskbarLayout()
    {
        nint taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == 0 || !GetWindowRect(taskbar, out var taskbarRect))
        {
            return default;
        }

        var taskbarArea = ToRect(taskbarRect);

        nint tray = FindWindowEx(taskbar, 0, "TrayNotifyWnd", null);
        if (tray == 0 || !GetWindowRect(tray, out var trayRect))
        {
            // No tray found: treat the right edge of the taskbar as the boundary.
            var fallback = new RectInt32(
                taskbarArea.X + taskbarArea.Width, taskbarArea.Y, 0, taskbarArea.Height);
            return new TaskbarLayout(taskbarArea, fallback, true);
        }

        return new TaskbarLayout(taskbarArea, ToRect(trayRect), true);
    }

    private static RectInt32 ToRect(RECT r) =>
        new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    private static readonly nint HwndTopMost = -1;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

    /// <summary>
    /// Re-asserts top-most placement. The taskbar is top-most too, so whichever window
    /// was raised last wins; without repeating this the bar silently slips behind it.
    /// </summary>
    internal static void KeepAboveTaskbar(nint hWnd) =>
        SetWindowPos(hWnd, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);

    [LibraryImport("user32.dll")]
    private static partial nint GetDC(nint hWnd);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(nint hWnd, nint hDC);

    [LibraryImport("gdi32.dll")]
    private static partial uint GetPixel(nint hDC, int x, int y);

    private const uint CLR_INVALID = 0xFFFFFFFF;

    /// <summary>
    /// Samples a pixel from the taskbar itself so the bar can paint the exact same
    /// colour. The taskbar's tint follows the theme, the accent colour and the
    /// wallpaper behind its acrylic, so no fixed colour blends reliably.
    /// </summary>
    internal static (byte R, byte G, byte B)? SampleScreenPixel(int x, int y)
    {
        nint dc = GetDC(0);
        if (dc == 0)
        {
            return null;
        }

        try
        {
            uint value = GetPixel(dc, x, y);
            if (value == CLR_INVALID)
            {
                return null;
            }

            // COLORREF is 0x00BBGGRR.
            return ((byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), (byte)((value >> 16) & 0xFF));
        }
        finally
        {
            ReleaseDC(0, dc);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    /// <summary>Keeps the window out of the taskbar and the Alt+Tab list.</summary>
    internal static void MakeToolWindow(nint hWnd)
    {
        var style = GetWindowLongPtr(hWnd, GwlExStyle);
        SetWindowLongPtr(hWnd, GwlExStyle, style | WsExToolWindow);
    }

    internal static void SetCornerPreference(nint hWnd, CornerPreference preference)
    {
        int value = (int)preference;
        DwmSetWindowAttribute(hWnd, DwmwaWindowCornerPreference, in value, sizeof(int));
    }

    /// <summary>Cursor position in physical screen pixels.</summary>
    internal static PointInt32 GetCursorPosition()
    {
        return GetCursorPos(out var p) ? new PointInt32(p.X, p.Y) : default;
    }

    /// <summary>Scale factor for the monitor the window is currently on.</summary>
    internal static double GetScaleForWindow(nint hWnd)
    {
        uint dpi = GetDpiForWindow(hWnd);
        return dpi == 0 ? 1.0 : dpi / 96.0;
    }
}

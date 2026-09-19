# Fluent Music Taskbar

## 1. Project Goal

Build a **native Windows 11 desktop music controller** that provides a beautiful, minimal, Fluent-style music experience for media currently playing on the user's computer.

The application should work with:

* Spotify
* YouTube / YouTube Music
* Anghami
* Other applications that expose Windows media information through Windows System Media Transport Controls (SMTC)

The application must **not download, scrape, copy, or redistribute music**.

It should only control and display media that is already being played by supported applications.

The goal is to create a polished Windows-native experience that feels like it belongs inside Windows 11.

---

# 2. Important Product Direction

This is NOT a clone of Spotify.

This is NOT a music streaming application.

This is NOT a replacement for Windows Explorer.

It is a:

> **Universal Windows music controller / mini-player**

The application detects the currently active media session and provides a custom interface for controlling it.

The user should be able to continue using Spotify, YouTube, Anghami, etc. normally.

Our application simply provides a beautiful control layer.

---

# 3. Technology

Use:

* C#
* .NET
* WinUI 3
* Windows App SDK
* XAML
* Windows System Media Transport Controls / Global System Media Transport Controls APIs where appropriate
* Windows App SDK / Win32 interoperability when necessary

Target:

> Windows 11

Do NOT use Electron unless there is a strong technical reason.

Prefer a native Windows implementation.

---

# 4. Design Philosophy

The application should feel:

* Modern
* Fluent
* Minimal
* Premium
* Smooth
* Native to Windows 11
* Lightweight
* Fast
* Subtle

Avoid:

* Excessive gradients
* Neon cyberpunk styling
* Excessive glass effects
* Huge buttons
* Generic AI-generated UI
* Excessive animations
* Clutter
* Old Windows styling

The UI should look like a carefully designed Microsoft-quality Windows utility.

---

# 5. Visual Style

Use Windows 11 Fluent Design principles.

Preferred:

* Mica / Acrylic where appropriate
* Rounded corners
* Subtle shadows
* Fluent icons
* Windows typography
* Soft hover states
* Minimal borders
* Proper spacing
* Smooth transitions

Use the existing Windows accent color where possible.

Do not hardcode an aggressive color palette.

The application should automatically feel comfortable in both:

* Light mode
* Dark mode

Respect the user's Windows theme.

---

# 6. Main Experience

The application should primarily live as a small utility near the Windows taskbar/system tray.

The user should not need to keep a large application window open.

Basic experience:

```text
Windows Taskbar

[ Start ] [ Apps ... ]                 [ Network ] [ Volume ] [ Clock ]

                                              🎵
```

When music is playing, the application should expose the current media.

The exact taskbar integration must use supported Windows APIs.

Do NOT modify Windows Explorer.exe or patch Windows system files.

Do NOT replace the native Windows taskbar.

---

# 7. Mini Player

When the user opens the application, show a compact player.

Example:

```text
┌────────────────────────────────────────────┐
│                                            │
│   ┌──────────┐                             │
│   │          │    Blinding Lights          │
│   │  Artwork │    The Weeknd               │
│   │          │                             │
│   └──────────┘                             │
│                                            │
│             ◀    ❚❚    ▶                  │
│                                            │
│        ━━━━━━━━━●━━━━━━━━                   │
│                                            │
└────────────────────────────────────────────┘
```

Information:

* Album artwork
* Song title
* Artist
* Album if available
* Playback state
* Playback position
* Duration if available

Controls:

* Previous
* Play / Pause
* Next

Optional:

* Volume
* Open source application
* Repeat
* Shuffle

Only expose controls supported by the active media session.

---

# 8. Media Session Detection

The application should detect active Windows media sessions.

Possible applications include:

* Spotify
* Chrome
* Microsoft Edge
* Firefox
* YouTube in a browser
* YouTube Music
* Anghami
* VLC
* Other compatible media applications

Do not assume that every application provides the same metadata.

Handle missing data gracefully.

For example:

```text
Title:
Unknown

Artist:
Unknown

Artwork:
Default music icon
```

The UI must never break because metadata is missing.

---

# 9. Multiple Media Sessions

There may be multiple media sessions.

For example:

```text
Spotify      Playing
YouTube      Paused
VLC          Paused
```

The application should identify the currently active session according to Windows media-session state.

If appropriate, provide a media-source selector:

```text
┌───────────────────────────────────┐
│ Playing from                      │
│                                   │
│ ● Spotify                         │
│ ○ YouTube                         │
│ ○ VLC                             │
└───────────────────────────────────┘
```

Do not make this complicated.

The default experience should automatically select the active session.

---

# 10. Artwork

Display album artwork when available.

Artwork should:

* Maintain correct aspect ratio
* Have rounded corners
* Load asynchronously
* Cache where appropriate
* Avoid blocking the UI
* Handle missing artwork

Fallback:

```text
┌──────────┐
│          │
│    ♪     │
│          │
└──────────┘
```

Do not download artwork from third-party websites unless the active media session explicitly provides an appropriate source and doing so is technically and legally appropriate.

Prefer artwork provided by Windows/media session APIs.

---

# 11. Animations

Animations are important, but they must remain subtle.

Use animations for:

### Opening

```text
opacity:
0 → 1

scale:
0.96 → 1
```

### Track change

When the song changes:

```text
Old artwork
    ↓
fade
    ↓
New artwork
    ↓
fade in
```

### Play / pause

The play button should transition smoothly:

```text
▶
↓
❚❚
```

### Hover

Buttons should have subtle:

* opacity change
* background change
* scale
* icon transition

Do NOT use exaggerated bouncing animations.

Animations should normally be short and smooth.

---

# 12. Compact Mode

Provide a very small player mode.

Example:

```text
┌─────────────────────────────────┐
│ 🖼  Blinding Lights   ❚❚  ▶     │
└─────────────────────────────────┘
```

The user should be able to keep it visible without taking much screen space.

---

# 13. Expanded Mode

Expanded mode:

```text
┌────────────────────────────────────────────┐
│                                            │
│                ┌──────────┐                │
│                │          │                │
│                │ Artwork  │                │
│                │          │                │
│                └──────────┘                │
│                                            │
│             Blinding Lights                │
│               The Weeknd                   │
│                                            │
│       ━━━━━━━━━●━━━━━━━━━━                  │
│                                            │
│          ◀      ❚❚      ▶                 │
│                                            │
└────────────────────────────────────────────┘
```

The expanded view should still be compact.

---

# 14. System Tray

Add a system-tray icon.

Right click:

```text
┌─────────────────────────────┐
│ Fluent Music                │
├─────────────────────────────┤
│ Open Player                 │
│                             │
│ Start with Windows     ✓    │
│                             │
│ Settings                    │
│                             │
│ Exit                        │
└─────────────────────────────┘
```

Left click should open the player.

---

# 15. Global Keyboard Shortcuts

Provide optional global shortcuts.

Default examples:

```text
Play/Pause
Next Track
Previous Track
Open Player
```

Do not conflict aggressively with existing Windows shortcuts.

Allow the user to customize shortcuts later.

---

# 16. Startup

The application should optionally start with Windows.

Setting:

```text
Start Fluent Music with Windows
[ ON ]
```

Use the appropriate Windows-supported startup mechanism.

Do not modify registry entries unnecessarily.

---

# 17. Performance Requirements

This application should be lightweight.

Target:

* Low CPU usage while idle
* Low memory usage
* No constant polling if event-driven APIs are available
* No unnecessary background loops
* No excessive disk access
* No unnecessary network requests

Prefer event-driven media-session updates.

Do not constantly query every application.

---

# 18. Architecture

Use a clean architecture.

Suggested structure:

```text
FluentMusic/
│
├── App.xaml
├── App.xaml.cs
│
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── Models/
│   ├── MediaSessionInfo.cs
│   ├── PlaybackState.cs
│   └── AppSettings.cs
│
├── Services/
│   ├── MediaSessionService.cs
│   ├── MediaControlService.cs
│   ├── ArtworkService.cs
│   ├── TrayService.cs
│   └── SettingsService.cs
│
├── ViewModels/
│   ├── PlayerViewModel.cs
│   └── SettingsViewModel.cs
│
├── Views/
│   ├── PlayerView.xaml
│   └── SettingsView.xaml
│
├── Controls/
│   ├── AlbumArt.xaml
│   ├── PlaybackControls.xaml
│   └── ProgressBar.xaml
│
├── Styles/
│   ├── Colors.xaml
│   ├── Typography.xaml
│   ├── Buttons.xaml
│   └── Controls.xaml
│
└── Assets/
    └── Icons/
```

Use MVVM where practical.

Do not over-engineer the application.

---

# 19. MediaSessionService

Create a dedicated service responsible for:

* Detecting media sessions
* Identifying active sessions
* Reading metadata
* Reading playback state
* Reading timeline information
* Listening for changes

Expose a clean application-level model.

Example:

```csharp
public class MediaSessionInfo
{
    public string Title { get; set; }
    public string Artist { get; set; }
    public string Album { get; set; }

    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }

    public bool IsPlaying { get; set; }

    public ImageSource Artwork { get; set; }

    public string SourceApp { get; set; }
}
```

Adapt this model to the actual Windows APIs.

Do not invent APIs.

Verify the exact Windows App SDK / Windows API available for the chosen target version.

---

# 20. Media Controls

Implement:

```text
Play()
Pause()
TogglePlayPause()

Next()
Previous()
```

If supported:

```text
Seek()
SetPosition()
```

Only expose actions that the active media session supports.

If a control is unavailable:

* Disable it
* Do not pretend it works

---

# 21. Progress

Display:

```text
01:24 ━━━━━━━━━●━━━━━━ 03:42
```

Update smoothly.

Avoid excessive API calls.

The UI can interpolate the displayed position between media-session updates.

When the media session reports a new position, resynchronize.

---

# 22. Settings

Create a small Fluent settings page.

Settings:

### General

* Start with Windows
* Show player on startup
* Minimize to tray

### Appearance

* Follow Windows
* Light
* Dark

### Behavior

* Automatically detect active media
* Remember last media source

### Shortcuts

* Play/Pause
* Next
* Previous
* Open Player

Keep settings simple.

---

# 23. Window Behavior

The player should behave more like a utility popup than a normal desktop application.

Requirements:

* Small footprint
* No unnecessary title bar
* Rounded corners
* Always-on-top option
* Close → minimize to tray
* Escape → close/hide player
* Clicking outside → optionally hide

Do not force always-on-top by default.

---

# 24. Fluent Components

Create reusable components rather than duplicating XAML.

Examples:

```text
FluentButton
MediaButton
ArtworkView
TrackInfo
PlaybackControls
ProgressBar
GlassCard
```

However, do not create abstractions that make simple UI harder to maintain.

---

# 25. Accessibility

Support:

* Keyboard navigation
* Proper accessible names
* Tooltips
* High contrast where possible
* Reasonable focus states
* Screen-reader-friendly controls

Example tooltip:

```text
Play / Pause
```

instead of relying only on an icon.

---

# 26. Error Handling

The app should never crash because:

* Spotify is closed
* YouTube is closed
* Anghami is closed
* No media is playing
* Metadata is missing
* Artwork is missing
* A media session disappears
* A control is unsupported

Empty state:

```text
┌──────────────────────────────┐
│                              │
│             ♪                │
│                              │
│       Nothing playing        │
│                              │
│  Play something to get       │
│  started.                   │
│                              │
└──────────────────────────────┘
```

---

# 27. No-Media State

When nothing is playing:

```text
Nothing playing

♪


Start playing music or video
to control it here.
```

Keep it visually attractive.

---

# 28. Security / Privacy

The application should not:

* Collect listening history
* Upload song metadata
* Track the user
* Scrape websites
* Store personal information unnecessarily
* Require a Spotify login
* Require an Anghami login

The application should operate locally.

Internet access should not be required for basic media control.

---

# 29. Spotify / YouTube / Anghami Strategy

Do NOT create separate Spotify, YouTube, and Anghami APIs initially.

First implement the Windows media-session layer.

Concept:

```text
                 Fluent Music
                       │
                       ↓
             Windows Media Sessions
                       │
        ┌──────────────┼──────────────┐
        ↓              ↓              ↓
     Spotify        YouTube        Anghami
```

If an application exposes compatible Windows media controls, it should work automatically.

Only create service-specific integrations if a feature cannot be implemented through Windows media APIs.

Do not request user credentials unless a future feature genuinely requires them.

---

# 30. Future Features

Do NOT implement these in the first version.

Possible future versions:

### v2

* Media source selector
* Better taskbar integration
* Animated visualizer
* Custom themes
* Album color extraction
* More keyboard shortcuts

### v3

* Spotify-specific features
* Playlist controls
* Queue
* Lyrics where legally/technically appropriate
* Media history
* Custom widgets

### v4

* Plugin system
* Custom themes
* User-created layouts

Keep the first version focused.

---

# 31. Development Phases

## Phase 1 — Application foundation

Build:

* WinUI 3 project
* Fluent styling
* Dark/light mode
* Mica
* Basic window
* System tray

Do not build advanced UI yet.

---

## Phase 2 — Media detection

Implement:

* Media session discovery
* Active session detection
* Metadata
* Playback state
* Artwork

Create a debug view so we can verify the actual information Windows exposes.

Example:

```text
Source: Spotify
Title: Blinding Lights
Artist: The Weeknd
State: Playing
Position: 01:24
Duration: 03:20
```

---

## Phase 3 — Controls

Implement:

* Play/pause
* Previous
* Next
* Position
* Seek if supported

Test with:

* Spotify
* YouTube
* Anghami

---

## Phase 4 — Final UI

Build:

* Album artwork
* Track information
* Controls
* Progress
* Empty state
* Animations
* Compact mode
* Expanded mode

---

## Phase 5 — Windows integration

Implement:

* System tray
* Startup
* Popup behavior
* Keyboard shortcuts
* Always-on-top option

---

## Phase 6 — Polish

Test:

* Dark mode
* Light mode
* Multiple monitors
* Different DPI scaling
* 100%
* 125%
* 150%
* 200%

Test:

* No media
* Spotify
* YouTube
* Anghami
* Multiple media sessions
* Media switching
* Pausing
* Resuming
* Closing media applications

---

# 32. Important Implementation Rules

1. Do not modify Windows system files.
2. Do not inject code into Explorer.exe.
3. Do not patch the Windows taskbar.
4. Do not scrape Spotify/YouTube/Anghami websites.
5. Do not require streaming-service credentials for basic functionality.
6. Prefer official Windows APIs.
7. Verify APIs against the actual SDK installed.
8. Keep the application native.
9. Keep CPU/memory usage low.
10. Keep the UI independent from media-source-specific code.
11. Do not break existing application behavior.
12. Do not redesign working components unnecessarily.
13. Make small, testable changes.
14. Build after each major implementation step.
15. Fix compilation errors before moving to the next phase.

---

# 33. Coding Style

Prefer:

* Clear C#
* Small services
* MVVM
* Dependency injection only where useful
* Async APIs where appropriate
* CancellationToken where useful
* Proper disposal
* Null-safe code
* Clear naming

Avoid:

* Giant classes
* Giant XAML files
* Global state everywhere
* Excessive abstractions
* Hardcoded paths
* Magic numbers
* Busy polling loops

---

# 34. First Task for Claude Code

Before writing the entire application:

1. Inspect the current development environment.
2. Check installed .NET SDK.
3. Check Visual Studio / WinUI tooling if available.
4. Check Windows version.
5. Create the WinUI 3 project.
6. Confirm it builds.
7. Run the empty application.
8. Then implement the Fluent shell.
9. Then implement media-session detection.

Do NOT attempt to implement every feature in one step.

Work incrementally.

---

# 35. First Milestone

The first successful milestone should look approximately like:

```text
┌─────────────────────────────────────────┐
│                                         │
│       ┌───────────────┐                 │
│       │               │                 │
│       │    Artwork    │                 │
│       │               │                 │
│       └───────────────┘                 │
│                                         │
│          Blinding Lights                │
│             The Weeknd                  │
│                                         │
│        ━━━━━━━●━━━━━━━━                 │
│                                         │
│             ◀  ❚❚  ▶                   │
│                                         │
└─────────────────────────────────────────┘
```

with actual information coming from the currently playing Windows media session.

---

# 36. Definition of Done

The first production-ready version should:

* Run natively on Windows 11
* Detect active media sessions
* Display title
* Display artist
* Display artwork when available
* Display playback state
* Play/pause
* Previous
* Next
* Show playback progress
* Handle missing metadata
* Handle media applications closing
* Support Spotify
* Support browser-based YouTube when Windows exposes its media session
* Support Anghami when Windows exposes its media session
* Have a polished Fluent UI
* Support dark/light mode
* Have subtle animations
* Run from the system tray
* Have low idle resource usage
* Never modify Windows system files
* Never require media-service credentials for basic functionality

---

# 37. Claude Code Instructions

You are acting as the senior Windows desktop engineer for this project.

Do not simply generate a large amount of code.

First inspect the environment and existing project.

Then work incrementally.

For every major change:

1. Explain what you are changing.
2. Implement it.
3. Build the project.
4. Fix errors.
5. Run/test where possible.
6. Report exactly what works.
7. Continue to the next milestone.

If an API is uncertain, verify the actual SDK/documentation instead of guessing.

Prioritize a working native implementation over adding unnecessary features.

The final application should feel like a **real Windows 11 Fluent utility**, not a web application placed inside a Windows window.

The core product identity is:

> **A beautiful, lightweight, universal music controller for Windows.**

# Flyout Taskbar

Now playing, inside the Windows 11 taskbar.

Two takes on the same idea, both driven by the Windows System Media Transport
Controls, so they follow Spotify, a YouTube tab, or anything else that publishes
a media session.

## `windhawk/` — the taskbar mod

A [Windhawk](https://windhawk.net/) mod that adds the strip as a real child of
the taskbar's own XAML tree: album artwork, the track, the artist and a waveform
that animates only while something is playing. Clicking it opens a player
popover with previous / play-pause / next, shuffle and repeat.

**To use it:** install Windhawk, then *Create a new mod*, paste
[`windhawk/fluent-music-taskbar.wh.cpp`](windhawk/fluent-music-taskbar.wh.cpp)
over the template, and press Compile.

Windows 11 removed deskbands and offers no supported API for putting content in
the taskbar, so a mod is the only way to be genuinely *inside* it rather than
floating over it.

Developed and tested on Windows 11 25H2 (build 26200). MIT licensed.

## `FluentMusic/` — the standalone app

A WinUI 3 app that needs no Windhawk. It runs either as a handle tucked against
the screen edge that opens a glass player panel, or as a bar painted to match
the taskbar and positioned over it. Being a separate window, it sits *on* the
taskbar rather than in it — the trade for not touching Explorer.

Requires the .NET 10 SDK; `dotnet build` in `FluentMusic/`.

## `tools/SmtcProbe/`

A small console probe that dumps whatever the media session currently reports —
title, artist, playback state, thumbnail bytes. Useful for telling "the app
isn't publishing artwork" apart from "our code isn't showing it".

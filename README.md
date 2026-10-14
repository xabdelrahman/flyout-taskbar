# Flyout Taskbar

Now playing, inside the Windows 11 taskbar.

![The strip in the taskbar](docs/taskbar-strip.png)

![The player popover](docs/popover.png)

Album artwork, the track, the artist and an animation that runs only while
something is actually playing. Clicking the strip opens a player popover with
previous / play-pause / next, shuffle and repeat.

It reads the Windows System Media Transport Controls, so it follows Spotify, a
YouTube tab in a browser, or anything else that publishes a media session — no
per-app setup.

The strip is a real child of the taskbar's own XAML tree, not a window floating
over it. Windows 11 removed deskbands and offers no supported API for putting
content in the taskbar, so a [Windhawk](https://windhawk.net/) mod is the only
way to be genuinely *inside* it.

## Should you use this one?

Probably not — and you should know that before installing.

Windhawk's catalog already has
[Taskbar Fluent Media Player](https://windhawk.net/mods/taskbar-fluent-media-player),
**Island Media Controls** and **Taskbar Music Lounge**. The first two also inject
into the taskbar's XAML tree, so "really inside the taskbar" is not a
differentiator. Taskbar Fluent Media Player is ~10,000 lines to this mod's
~1,700, with 20+ placements, auto-hide, theme-aware materials and a genuine
audio visualizer (WASAPI loopback and an FFT) rather than the decorative
animation here.

This mod was [submitted to the catalog](https://github.com/ramensoftware/windhawk-mods/pull/5580)
and declined as too close to what already exists, which was a fair call. It
lives on here because it works and because the code may be useful to read —
not because it is the better choice.

**Use Taskbar Fluent Media Player instead** unless you specifically want this
one's minimal fixed layout and its cross-fade between tracks.

## Install

Since it is not in the catalog, it installs by hand:

1. Install [Windhawk](https://windhawk.net/) — a normal Windows installer, once.
2. Open Windhawk and choose **Create a new mod**.
3. Select all in the editor and paste
   [`windhawk/fluent-music-taskbar.wh.cpp`](windhawk/fluent-music-taskbar.wh.cpp)
   over the template.
4. Press **Compile**.

Windhawk compiles it locally and loads it into Explorer. There are no automatic
updates on this path — to update, paste a newer copy over it and compile again.
Disabling or removing the mod puts the taskbar back exactly as it was.

## Settings

In Windhawk, under the mod's **Settings** tab:

* **Distance from the left** moves the strip along the taskbar. The strip is
  drawn *over* the taskbar rather than reserving space in it, so it needs a clear
  run: anything underneath it cannot be clicked. The default sits it in the empty
  gap between the widgets button and the centred Start icons. With a left-aligned
  taskbar, or with many windows open and a centred taskbar, the task buttons can
  grow under it — move the strip if that happens.
* Album artwork and the popover controls can each be turned off.
* **Playing animation** is a decorative indicator, not an audio visualizer — it
  is not driven by the audio signal, it only tells you something is playing.

## Compatibility

Developed and tested on Windows 11 25H2 (build 26200). It hooks
`TaskbarFrame::OnTaskbarLayoutChildBoundsChanged` in `Taskbar.View.dll`; if that
symbol is absent on your build, the mod logs a failure and does nothing rather
than misbehaving.

Secondary monitors each get their own strip, and all of them update together.

MIT licensed.

## `tools/SmtcProbe/`

A small console probe that dumps whatever the media session currently reports —
title, artist, playback state, thumbnail bytes. Useful for telling "the app
isn't publishing artwork" apart from "our code isn't showing it".

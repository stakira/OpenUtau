
# OpenUtau

OpenUtau aims to be an open source editing environment for UTAU community, with modern user experience and intelligent phonological support.

Current stage: Alpha

<a href="https://ci.appveyor.com/project/stakira/openutau" target="_blank"><img src="https://ci.appveyor.com/api/projects/status/github/stakira/OpenUtau?svg=true" alt="CI Badge"/></a>

[Discord](https://discord.gg/UfpMnqMmEM)

## How to Install

- Download the <a href="https://github.com/stakira/OpenUtau/releases">Latest Build</a> or build it yourself.
- Install singer from menu Tools -> Install Singer. Select the singer zip or rar file.
- Put resampler exe or dll under Resamplers folder. Open Preferences menu to select resampler.

Tested resamplers
- fresamp14.exe
- tn_fnds.exe
- TIPS.exe

## How to Use

Fluent Navigation Using Scroll Wheel

![Editor](Misc/GIFs/editor.gif)

Feature-Rich Midi Editor

![Editor](Misc/GIFs/editor2.gif)

Vibrato Editing

![Vibrato](Misc/GIFs/vibrato.gif)

Render and Playback

![Playback](Misc/GIFs/playback.gif)

Redo Undo

![undo](Misc/GIFs/undo.gif)

Other Actions
- Scroll wheel on the measure bar (the bar with numbers right below the horizontal scroll bar) to zoom horizontally.
- Scroll wheel on the widget right above the vertical scroll bar to zoom vertically.
- Press `Ctrl` key to select multiple notes.
- Press `Space` key anywhere to start playing or pause.

## How to Build

- Visual Studio 2019
- .NET Framework 4.8 Developer Pack

## Scope
#### The scope of OpenUtau includes:
- Modern user experience.
- Compatibility with UTAU technologies.
- Intelligent VCV, CVVC an other voicebank sampling technique support.
- Internationalization, including UI translation and file system encoding support.
- Smooth preview/rendering experience.
- A easy to use plugin system.
- An efficient sample connecting engine (a.k.a. wavetool).
- An efficient resampling engine interface.
- A Windows version.

#### The scope of OpenUtau does not include:
- Resampling engines (a.k.a resampler).
- Full feature digital music workstation.
- OpenUtau does not strike for Vocaloid compatibility, other than limited features.

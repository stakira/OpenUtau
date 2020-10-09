
# OpenUtau

OpenUtau aims to be an open source editing environment for UTAU community, with modern user experience and intelligent phonological support.

Current stage: Alpha

<img src="https://ci.appveyor.com/api/projects/status/github/stakira/OpenUtau?svg=true" alt="CI Badge"/>

## How to Use

- Download the <a href="https://ci.appveyor.com/project/stakira/openutau/build/artifacts" target="_blank">Latest Build</a> or build it yourself.
- You will need to provide your own sound libraries and your favourite resampler.
- Open Preferences menu to select a singer folder, such as the voice folder under UTAU.
- Put resampler exe or dll under Resamplers folder. Open Preferences menu to select resampler.

## How to Build

Requires:
- Visual Studio 2019
- .NET Framework 4.8 Developer Pack

## Preview

Fluent Navigation Using Scroll Wheel

![Editor](Misc/GIFs/editor.gif)

Feature-Rich Midi Editor

![Editor](Misc/GIFs/editor2.gif)

Render and Playback

![Playback](Misc/GIFs/playback.gif)

Redo Undo

![undo](Misc/GIFs/undo.gif)

Other Actions
- Scroll wheel on the measure bar (the bar with numbers right below the horizontal scroll bar) to zoom horizontally.
- Scroll wheel on the widget right above the vertical scroll bar to zoom vertically.
- Press `Ctrl` key to select multiple notes.
- Press spacebar key anywhere to start playing or pause.

## Scope
#### The scope of OpenUtau includes:
- Modern user experience.
- Compatibility with UTAU technologies.
- Intelligent VCV, CVVC an other voicebank sampling technique support.
- Internationalization, including UI translation and file system encoding support.
- Smooth preview/rendering experience.
- A easy to use plugin system.
- An efficient sample connecting engine (a.k.a. wavetool).
- A Windows version.

#### The scope of OpenUtau does not include:
- Resampling engines (a.k.a resampler).
- Full feature digital music workstation.
- OpenUtau does not strike for Vocaliod compatibility, other than limited features.

#### The scope of OpenUtau may include:
- An efficient resampling engine interface.
- Coorperate with other projects on resampling engine integration.
- A OS X version, but only after Windows version is mature.

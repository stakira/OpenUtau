
# OpenUtau

OpenUtau is an open source editing environment for UTAU community with modern user experience.

[![AppVeyor](https://img.shields.io/appveyor/build/stakira/OpenUtau?style=for-the-badge&label=appveyor&logo=appveyor)](https://ci.appveyor.com/project/stakira/openutau)
[![Discord](https://img.shields.io/discord/551606189386104834?style=for-the-badge&label=discord&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/UfpMnqMmEM)
[![Trello](https://img.shields.io/badge/trello-go-blue?style=for-the-badge&logo=trello)](https://trello.com/b/93ANoCIV/openutau)

## Getting Started

[![Download](https://img.shields.io/static/v1?style=for-the-badge&logo=github&label=download&message=latest&labelColor=FF347C&color=4ea6ea)](https://github.com/stakira/OpenUtau/releases/download/OpenUtau-Latest/OpenUtau.zip)

It is **strongly recommend** to go through a few Wiki pages before use:
- [Getting-Started](https://github.com/stakira/OpenUtau/wiki/Getting-Started)
- [Resamplers](https://github.com/stakira/OpenUtau/wiki/Resamplers)
- [Phonemizers](https://github.com/stakira/OpenUtau/wiki/Phonemizers)
- [FAQ](https://github.com/stakira/OpenUtau/wiki/FAQ)

## How to Contribute

Tried OpenUtau and not satisfied? Don't just walk away! You could help shape it:
- Report issues on [Discord](https://discord.gg/UfpMnqMmEM) or Github.
- Suggest features on Discord or Github.

Have coding skills? Don't just fork and keep it to yourself!
- Contribute fixes through Pull Requests.
- See the roadmap on [Trello](https://trello.com/b/93ANoCIV/openutau) and discuss it on Discord.

## Plugin Development

[API Document](OpenUtau.Core/Api/README.md)

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

## Scope
#### The scope of OpenUtau includes:
- Modern user experience.
- Selected compatibility with UTAU technologies.
  - OpenUtau aims to solve problems in less laborious ways, so don't expect it to replicate exact UTAU featuers.
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

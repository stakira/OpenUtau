
# OpenUtau

OpenUtau is an open source editing environment for UTAU community with modern user experience.

[![AppVeyor](https://img.shields.io/appveyor/build/stakira/OpenUtau?style=for-the-badge&label=appveyor&logo=appveyor)](https://ci.appveyor.com/project/stakira/openutau)
[![Discord](https://img.shields.io/discord/551606189386104834?style=for-the-badge&label=discord&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/UfpMnqMmEM)
[![QQ Qroup](https://img.shields.io/badge/QQ-485658015-blue?style=for-the-badge)](https://qm.qq.com/cgi-bin/qm/qr?k=8EtEpehB1a-nfTNAnngTVqX3o9xoIxmT&jump_from=webapi)
[![Trello](https://img.shields.io/badge/trello-go-blue?style=for-the-badge&logo=trello)](https://trello.com/b/93ANoCIV/openutau)

## Getting Started

[![Download](https://img.shields.io/static/v1?style=for-the-badge&logo=github&label=download&message=windows-x64&labelColor=FF347C&color=4ea6ea)](https://github.com/stakira/OpenUtau/releases/latest/download/OpenUtau-win-x64.zip)</br>
[![Download](https://img.shields.io/static/v1?style=for-the-badge&logo=github&label=download&message=windows-x86&labelColor=FF347C&color=4ea6ea)](https://github.com/stakira/OpenUtau/releases/latest/download/OpenUtau-win-x86.zip)</br>
[![Download](https://img.shields.io/static/v1?style=for-the-badge&logo=github&label=download&message=macos-x64&labelColor=FF347C&color=4ea6ea)](https://github.com/stakira/OpenUtau/releases/latest/download/OpenUtau-osx-x64.dmg)</br>
[![Download](https://img.shields.io/static/v1?style=for-the-badge&logo=github&label=download&message=linux-x64&labelColor=FF347C&color=4ea6ea)](https://github.com/stakira/OpenUtau/releases/latest/download/OpenUtau-linux-x64.tar.gz)

It is **strongly recommend** to go through a few Wiki pages before use:
- [Getting-Started](https://github.com/stakira/OpenUtau/wiki/Getting-Started)
- [Resamplers](https://github.com/stakira/OpenUtau/wiki/Resamplers-and-Wavtools)
- [Phonemizers](https://github.com/stakira/OpenUtau/wiki/Phonemizers)
- [FAQ](https://github.com/stakira/OpenUtau/wiki/FAQ)

- [中文使用说明](https://opensynth.miraheze.org/wiki/OpenUTAU/%E4%BD%BF%E7%94%A8%E6%96%B9%E6%B3%95)

## How to Contribute

Tried OpenUtau and not satisfied? Don't just walk away! You could help shape it:
- Report issues on [Discord](https://discord.gg/UfpMnqMmEM) or Github.
- Suggest features on Discord or Github.
- Add or update translations on Github.

Have coding skills? Don't just fork and keep it to yourself!
- Contribute fixes through Pull Requests.
- See the roadmap on [Trello](https://trello.com/b/93ANoCIV/openutau) and discuss it on Discord.

## Plugin Development

- [Editing Macros API Document](OpenUtau.Core/Editing/README.md)
- [Phonemizers API Document](OpenUtau.Core/Api/README.md)

## How to Use

Fluent Navigation Using Scroll Wheel

![Editor](Misc/GIFs/editor.gif)

Feature-Rich Midi Editor

![Editor](Misc/GIFs/editor2.gif)

Vibrato Editing

![Vibrato](Misc/GIFs/vibrato.gif)

Render and Playback

![Playback](Misc/GIFs/playback.gif)

See [Getting-Started](https://github.com/stakira/OpenUtau/wiki/Getting-Started) for more!

## Scope
#### The scope of OpenUtau includes:
- Modern user experience.
- Selected compatibility with UTAU technologies.
  - OpenUtau aims to solve problems in less laborious ways, so don't expect it to replicate exact UTAU features.
- Extensible realtime phonetics (VCV, CVVC, Arpasing) intellegence.
  - English, Japanese, Chinese, Korean, Russian and more.
- Internationalization, including UI translation and file system encoding support.
  - No you don't need to change system locale to use OpenUtau.
- Smooth preview/rendering experience.
- A easy to use plugin system.
- An efficient resampling engine interface.
  - Compatible with most UTAU resamplers.
- A Windows and a macOS version.

#### The scope of OpenUtau does not include:
- Full feature digital music workstation.
- OpenUtau does not strike for Vocaloid compatibility, other than limited features.

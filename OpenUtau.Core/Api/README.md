# OpenUtau API

API for OpenUtau plugin development. They are also used in OpenUtau.Plugin.Builtin project. You can refer to them as example.

## Phonemizer

Experimental. Subject to change. Feedback welcomed.

API documented in:
- [Phonemizer.cs](Phonemizer.cs)

Heavily commented example implementations, from simplest to most complex:
- [DefaultPhonemizer.cs](../DefaultPhonemizer.cs)
- [JapaneseVCVPhonemizer.cs](../../OpenUtau.Plugin.Builtin/JapaneseVCVPhonemizer.cs)
- [ChineseCVVPhonemizer.cs](../../OpenUtau.Plugin.Builtin/ChineseCVVPhonemizer.cs)
- [ArpasingPhonemizer.cs](../../OpenUtau.Plugin.Builtin/ArpasingPhonemizer.cs)

The main method to implement is:
```
public abstract Phoneme[] Process(Note[] notes, Note? prevNeighbour, Note? nextNeighbour);
```
`notes`: A group of notes. The first note contains the lyric. The rest are extender notes with lyric "+" or "+n" (n is a number).
`prevNeighbour` and `nextNeighbour`: Useful info for creating diphones, if applicable. E.g., creating proper leading diphone in VCV.
`returns`: An array of phonemes, positioned relative to the first note.
For actual document read comments in [Phonemizer.cs](Phonemizer.cs).

A complete Phonemizer should:
1. Produce phonemes (or diphones) from the lyric, and previous / next notes if exsit.
2. Distribute phonemes to positions relative to the first note.
3. Supports phonetic hinting, e.g., lyric like "read", "read[r iy d]" or "[r iy d]".
4. Supports extender note aligments if the language is multisyllabic, i.e., "+n" notes.

Tips:
- To load singer specific resouce, Implement resouce loading in SetSinger() and use singer.Location to look for files.
- If uses expensive resource, load it lazily when the phonemizer is created the first time. Use your best adjudgement to decide its lifetime.

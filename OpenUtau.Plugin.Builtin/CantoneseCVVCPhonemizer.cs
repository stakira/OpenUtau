using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenUtau.Api;
using OpenUtau.Classic;
using OpenUtau.Core.G2p;
using OpenUtau.Core.Ustx;
using Serilog;

namespace OpenUtau.Plugin.Builtin {
    /// <summary>
    /// Cantonese CVVC phonemizer.
    /// It works similarly to the Chinese CVVC phonemizer, including presamp.ini requirement.
    /// The big difference is that it converts hanzi to jyutping instead of pinyin.
    /// </summary>
    [Phonemizer("Cantonese CVVC Phonemizer", "ZH-YUE CVVC", "Lotte V", language: "ZH-YUE")]
    public class CantoneseCVVCPhonemizer : ChineseCVVCPhonemizer {
        protected override string[] Romanize(IEnumerable<string> lyrics) {
            return ZhG2p.CantoneseInstance.Convert(lyrics.ToList(), false, true).Split(" ");
        }
    }
}

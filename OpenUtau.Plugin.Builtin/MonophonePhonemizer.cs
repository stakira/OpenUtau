using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenUtau.Plugin.Builtin
{
    public abstract class MonophonePhonemizer : SyllableBasedPhonemizer
    {
        protected override string GetDictionaryName() => "";
        protected override List<string> ProcessSyllable(Syllable syllable){
            return syllable.cc.Append(syllable.v).ToList();
        }

        protected override List<string> ProcessEnding(Ending ending){
            return ending.cc.ToList();
        }
    }
}
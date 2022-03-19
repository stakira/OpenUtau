using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Vogen {
    class VogenSinger : USinger {
        public override string Id => meta.id;
        public override string Name => meta.name;
        public override USingerType SingerType => USingerType.Vogen;
        public override string BasePath => basePath;
        public override string Author => meta.voiceBy + " / " + meta.builtBy;
        public override string Location => filePath;
        public override string Web => string.Empty;
        public override string Version => meta.version;
        public override string OtherInfo => string.Empty;
        public override IList<string> Errors => errors;
        public override string Avatar => string.Empty;
        public override byte[] AvatarData => null;
        public override string Portrait => string.Empty;
        public override float PortraitOpacity => 0;
        public override Encoding TextFileEncoding => Encoding.UTF8;
        public override IList<USubbank> Subbanks => subbanks;
        public override Dictionary<string, UOto> Otos => otos;

        string basePath;
        string filePath;
        VogenMeta meta;
        List<string> errors = new List<string>();
        List<USubbank> subbanks = new List<USubbank>();
        Dictionary<string, UOto> otos = new Dictionary<string, UOto>();

        public byte[] model;

        public VogenSinger(string filePath, VogenMeta meta, byte[] model) {
            basePath = Path.GetDirectoryName(filePath);
            this.filePath = filePath;
            this.meta = meta;
            this.model = model;
            found = true;
            loaded = true;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, out UOto oto) {
            oto = new UOto() {
                Alias = phoneme,
                Phonetic = phoneme,
            };
            return true;
        }

        public override bool TryGetMappedOto(string phoneme, int tone, string color, out UOto oto) {
            oto = new UOto() {
                Alias = phoneme,
                Phonetic = phoneme,
            };
            return true;
        }
    }
}

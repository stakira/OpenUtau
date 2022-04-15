using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.Render {
    public static class Renderers {
        public const string CLASSIC = "CLASSIC";
        public const string WORLDLINER = "WORLDLINER";
        public const string ENUNU = "ENUNU";
        public const string VOGEN = "VOGEN";

        static readonly string[] classicRenderers = new[] { CLASSIC, WORLDLINER };
        static readonly string[] enunuRenderers = new[] { ENUNU };
        static readonly string[] vogenRenderers = new[] { VOGEN };
        static readonly string[] noRenderers = new string[0];

        public static string[] GetSupportedRenderers(USingerType singerType) {
            switch (singerType) {
                case USingerType.Classic:
                    return classicRenderers;
                case USingerType.Enunu:
                    return enunuRenderers;
                case USingerType.Vogen:
                    return vogenRenderers;
                default:
                    return noRenderers;
            }
        }

        public static string GetDefaultRenderer(USingerType singerType) {
            return GetSupportedRenderers(singerType)[0];
        }

        public static IRenderer CreateRenderer(string renderer) {
            if (renderer == CLASSIC) {
                return new Classic.ClassicRenderer();
            } else if (renderer == WORLDLINER) {
                return new Classic.WorldlineRenderer();
            } else if (renderer == ENUNU) {
                return new Enunu.EnunuRenderer();
            } else if (renderer == VOGEN) {
                return new Vogen.VogenRenderer();
            }
            return null;
        }
    }
}

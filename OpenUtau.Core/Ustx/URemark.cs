using System;


namespace OpenUtau.Core.Ustx {
    public class URemark {

        public int position;
        public string text = "";
        public string color = "Red";

        public URemark() {
        }
        public URemark(string text, string color, int position) {
            this.text = text;
            this.color = color;
            this.position = position;
        }
        public override string ToString() {
            return text;
        }
        public URemark Clone() {
            return new URemark(this.text, this.color, this.position);
        }
        public void updateRemark(string text, string color, int position) {
            this.text = text;
            this.color = color;
            this.position = position;
        }
        public void AfterLoad(UProject project, UTrack track, UVoicePart part) { }

        public void BeforeSave(UProject project, UTrack track, UVoicePart part) { }
    }
}

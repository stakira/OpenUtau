using System;
using System.Collections.Generic;
using System.Text;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.DawIntegration {
    public class DawMessage {
    }

    public class UpdateStatusMessage : DawMessage {
        public string ustx;
        public List<string> mixes;

        public UpdateStatusMessage(string ustx, List<string> mixes) {
            this.ustx = ustx;
            this.mixes = mixes;
        }
    }
}

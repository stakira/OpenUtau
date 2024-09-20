using System.Collections.Generic;

namespace OpenUtau.Core.DawIntegration {
    public class DawMessage {
    }

    public class InitMessage : DawMessage {
        public string ustx;

        public InitMessage(string ustx) {
            this.ustx = ustx;
        }
    }
    public class ErrorMessage : DawMessage {
        public string message;

        public ErrorMessage(string message) {
            this.message = message;
        }
    }
    public class UpdateStatusMessage : DawMessage {
        public string ustx;
        public List<string> trackNames;
        public List<string> mixes;

        public UpdateStatusMessage(string ustx, List<string> trackNames, List<string> mixes) {
            this.ustx = ustx;
            this.trackNames = trackNames;
            this.mixes = mixes;
        }
    }
}

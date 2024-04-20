using System;

namespace OpenUtau.Core {
    public class MessageCustomizableException : Exception {

        public override string Message { get; } = string.Empty;
        public string TranslatableMessage { get; set; } = string.Empty;
        public Exception SubstanceException { get; }
        public bool ShowStackTrace { get; } = true;

        /// <summary>
        /// This allows the use of translatable messages and the hiding of stack traces in the message box.
        /// <summary>
        /// <paramref name="message">untranslated message</paramref>
        /// <paramref name="translatableMessage">By enclosing the resource key with a tag like "<translate:key>", only that part will be translated.</paramref>
        /// <paramref name="e">underlying exception</paramref>
        public MessageCustomizableException(string message, string translatableMessage, Exception e) {
            Message = message;
            TranslatableMessage = translatableMessage;
            SubstanceException = e;
        }

        /// <summary>
        /// This allows the use of translatable messages and the hiding of stack traces in the message box.
        /// <summary>
        /// <paramref name="message">untranslated message</paramref>
        /// <paramref name="translatableMessage">By enclosing the resource key with a tag like "<translate:key>", only that part will be translated.</paramref>
        /// <paramref name="e">underlying exception</paramref>
        /// <paramref name="showStackTrace">Can be omitted. Default is true.</paramref>
        public MessageCustomizableException(string message, string translatableMessage, Exception e, bool showStackTrace) {
            Message = message;
            TranslatableMessage = translatableMessage;
            SubstanceException = e;
            ShowStackTrace = showStackTrace;
        }

        public override string ToString() {
            return SubstanceException.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace OpenUtau.App.ViewModels {
    public static class DebugWindowExtension {
        public static LoggerConfiguration DebugWindow(
                  this LoggerSinkConfiguration loggerConfiguration,
                  IFormatProvider formatProvider = null) {
            return loggerConfiguration.Sink(DebugViewModel.Sink.Inst);
        }
    }

    public class DebugViewModel : ViewModelBase {
        public class Sink : ILogEventSink {
            static Sink sink = new Sink();
            public static Sink Inst => sink;

            public DebugViewModel? debugViewModel { get; set; }
            public void Emit(LogEvent logEvent) {
                if (debugViewModel != null) {
                    debugViewModel.Emit(logEvent);
                }
            }
        }

        public ObservableCollection<string> TextItems => textItems;
        private ObservableCollectionExtended<string> textItems;

        private ITextFormatter formater;
        private StringWriter stringWriter;

        public DebugViewModel() {
            const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            textItems = new ObservableCollectionExtended<string>();
            formater = new MessageTemplateTextFormatter(template);
            stringWriter = new StringWriter();
        }

        public void Clear() {
            textItems.Clear();
        }

        public void Attach() {
            Sink.Inst.debugViewModel = this;
        }

        public void Detach() {
            Sink.Inst.debugViewModel = null;
        }

        public void Emit(LogEvent logEvent) {
            formater.Format(logEvent, stringWriter);
            textItems.Add(stringWriter.GetStringBuilder().ToString());
            stringWriter.GetStringBuilder().Clear();
        }
    }
}

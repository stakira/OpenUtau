using System;
using System.Collections.ObjectModel;
using System.IO;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Avalonia.Data.Converters;
using System.Globalization;

namespace OpenUtau.App.ViewModels {
    public class LogEventConverter : IValueConverter {
        private ITextFormatter formater;
        private StringWriter stringWriter;
        public LogEventConverter() {
            const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            formater = new MessageTemplateTextFormatter(template);
            stringWriter = new StringWriter();
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is LogEvent logEvent) {
                formater.Format(logEvent, stringWriter);
                string message = stringWriter.GetStringBuilder().ToString();
                stringWriter.GetStringBuilder().Clear();
                return message;
            }
            return string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class DebugViewModel : ViewModelBase {

        public class Sink : ILogEventSink {
            static Sink sink = new Sink();
            public static Sink Inst => sink;

            public LoggingLevelSwitch LevelSwitch =
                new LoggingLevelSwitch(LogEventLevel.Error);
            public ObservableCollectionExtended<LogEvent> LogEvents =
                new ObservableCollectionExtended<LogEvent>();

            public void Emit(LogEvent logEvent) {
                LogEvents.Add(logEvent);
            }
        }

        [Reactive] public LogEventLevel LogEventLevel { get; set; }
        public ObservableCollection<LogEvent> LogEvents => Sink.Inst.LogEvents;

        public void Clear() {
            Sink.Inst.LogEvents.Clear();
        }

        public void Attach() {
            Core.Util.DebugSwitches.DebugRendering = true;
            Sink.Inst.LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
        }

        public void Detach() {
            Core.Util.DebugSwitches.DebugRendering = false;
            Sink.Inst.LevelSwitch.MinimumLevel = LogEventLevel.Error;
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using Avalonia.Data.Converters;
using DynamicData.Binding;
using OpenUtau.App.Views;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

/*
 * Made And Checked By DELTA SYNTH & Gemini AI
 * Original Author: OpenUtau Team & Delta
 */

namespace OpenUtau.App.ViewModels {
    /// <summary>
    /// ตัวแปลงค่า LogEvent สำหรับการแสดงผลบน UI ภาษาไทย
    /// </summary>
    public class LogEventConverter : IValueConverter {
        private readonly ITextFormatter _formatter;
        
        public LogEventConverter() {
            // ปรับรูปแบบ Template ให้เหมาะสมและอ่านง่าย
            const string template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
            _formatter = new MessageTemplateTextFormatter(template);
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is LogEvent logEvent) {
                using var stringWriter = new StringWriter();
                _formatter.Format(logEvent, stringWriter);
                return stringWriter.ToString();
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            return new Avalonia.Data.BindingNotification(new NotImplementedException(), Avalonia.Data.BindingErrorType.Error);
        }
    }

    public class DebugViewModel : ViewModelBase {
        private DebugWindow? _window;

        public void SetWindow(DebugWindow w) {
            _window = w;
        }

        public DebugViewModel() {
            // คำสั่งสำหรับการสลับลำดับการแสดงผล Log
            ReverseLogOrderCommand = ReactiveCommand.Create(() => { 
                Sink.Inst.ToggleOrder(); 
            });

            // คำสั่งสำหรับคัดลอก Log ทั้งหมดไปยังคลิปบอร์ด
            CopyLogCommand = ReactiveCommand.Create(() => {
                _window?.CopyLogText();
            });
        }

        /// <summary>
        /// ระบบรับข้อมูล Log (Log Sink) ที่เชื่อมต่อกับ Serilog
        /// </summary>
        public class Sink : ILogEventSink {
            private static readonly Sink _instance = new Sink();
            public static Sink Inst => _instance;

            public LoggingLevelSwitch LevelSwitch { get; } = new LoggingLevelSwitch(LogEventLevel.Error);
            public ObservableCollectionExtended<LogEvent> LogEvents { get; } = new ObservableCollectionExtended<LogEvent>();

            private bool _reverseLogOrder = Preferences.Default.ReverseLogOrder;

            /// <summary>
            /// สลับลำดับ Log ระหว่าง ใหม่ไปเก่า หรือ เก่าไปใหม่
            /// </summary>
            public void ToggleOrder() {
                _reverseLogOrder = !_reverseLogOrder;
                ApplyOrderReversal();
            }

            private void ApplyOrderReversal() {
                var currentEvents = LogEvents.ToList();
                currentEvents.Reverse();
                
                using (LogEvents.SuspendNotifications()) {
                    LogEvents.Clear();
                    LogEvents.AddRange(currentEvents);
                }
            }

            /// <summary>
            /// กำหนดสถานะการเรียงลำดับโดยตรง
            /// </summary>
            public void SetReverseOrder(bool reversed) {
                if (_reverseLogOrder != reversed) {
                    ToggleOrder();
                }
            }

            public void Emit(LogEvent logEvent) {
                // จัดการเรื่อง Thread Safety และการจัดลำดับข้อมูล
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    if (_reverseLogOrder) {
                        LogEvents.Insert(0, logEvent);
                    } else {
                        LogEvents.Add(logEvent);
                    }
                    
                    // จำกัดจำนวน Log เพื่อประสิทธิภาพ (ตัวอย่าง: เก็บไว้ 1,000 รายการ)
                    if (LogEvents.Count > 1000) {
                        LogEvents.RemoveAt(_reverseLogOrder ? 1000 : 0);
                    }
                });
            }

            public override string ToString() {
                var sb = new StringBuilder();
                foreach (var l in LogEvents) {
                    sb.AppendLine($"[{l.Timestamp:yyyy-MM-dd HH:mm:ss}] [{l.Level}] {l.MessageTemplate.Text}");
                }
                return sb.ToString();
            }
        }

        // Properties สำหรับ UI
        [Reactive] public LogEventLevel LogEventLevel { get; set; }
        public ObservableCollection<LogEvent> LogEvents => Sink.Inst.LogEvents;
        public ReactiveCommand<Unit, Unit> ReverseLogOrderCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyLogCommand { get; }

        /// <summary>
        /// ล้างข้อมูลบันทึกทั้งหมด
        /// </summary>
        public void Clear() {
            Sink.Inst.LogEvents.Clear();
        }

        /// <summary>
        /// เริ่มต้นการติดตามการทำงาน (Attach Debug)
        /// </summary>
        public void Attach() {
            Core.Util.ProcessRunner.DebugSwitch = true;
            Sink.Inst.LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
        }

        /// <summary>
        /// ยกเลิกการติดตามการทำงาน (Detach Debug)
        /// </summary>
        public void Detach() {
            Core.Util.ProcessRunner.DebugSwitch = false;
            Sink.Inst.LevelSwitch.MinimumLevel = LogEventLevel.Error;
        }
    }
}

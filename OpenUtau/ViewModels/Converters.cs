using System;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace OpenUtau.App.ViewModels {
    /// <summary>
    /// แปลง CultureInfo เป็นชื่อภาษาที่มนุษย์อ่านออก
    /// เช่น "en-US" -> "English (United States)"
    /// </summary>
    public class CultureNameConverter : IValueConverter {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is CultureInfo cultureInfo) {
                // หากเป็น Invariant Culture ให้ดึงค่าจาก Resource (เช่น "No Language")
                if (cultureInfo.Equals(CultureInfo.InvariantCulture)) {
                    return ThemeManager.GetString("languages.invariant");
                }

                // ดึงชื่อภาษาในรูปแบบของภาษานั้นๆ (Native Name)
                string name = cultureInfo.NativeName;

                // ปรับให้ตัวอักษรตัวแรกเป็นตัวพิมพ์ใหญ่ (บาง Culture อาจคืนค่าตัวพิมพ์เล็ก)
                if (!string.IsNullOrEmpty(name)) {
                    return char.ToUpper(name[0]) + name.Substring(1);
                }
                return name;
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
            => throw new NotImplementedException();
    }

    /// <summary>
    /// แปลง Encoding เป็นชื่อเรียกมาตรฐาน
    /// เช่น Shift-JIS -> "Japanese (Shift-JIS)"
    /// </summary>
    public class EncodingNameConverter : IValueConverter {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            if (value is Encoding encoding) {
                return encoding.EncodingName;
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
            => throw new NotImplementedException();
    }
}

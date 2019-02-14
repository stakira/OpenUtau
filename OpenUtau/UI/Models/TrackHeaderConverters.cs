using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OpenUtau.UI.Models
{
    public class FaderValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double db = (double)value;
            if (db  == -24) return "-Inf";
            if (db < -16) db = db * 2 + 16;
            var result = db > 0 ? $"{db:+00.0}" : $"{db:00.0}";
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace TivoAhoy.Phone.Converters
{
    public class UtcToLocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime)
            {
                var localTime = (DateTime)value;
                var utcTime = localTime.ToLocalTime();

                return utcTime;
            } 
            
            if (value is DateTimeOffset)
            {
                var localTime = (DateTimeOffset)value;
                var utcTime = localTime.ToLocalTime();

                return utcTime;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime)
            {
                var utcTime = (DateTime)value;
                return utcTime.ToUniversalTime();
            }

            if (value is DateTimeOffset)
            {
                var utcTime = (DateTimeOffset)value;
                return utcTime.ToUniversalTime();
            }

            return value;
        }
    }
}

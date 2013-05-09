using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace TivoTest
{
    public class DurationToWidthConverter : MarkupExtension, IMultiValueConverter
    {
        private static Lazy<DurationToWidthConverter> lazyInstance = new Lazy<DurationToWidthConverter>();

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return lazyInstance.Value;
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2 ||
                !(values[0] is TimeSpan) ||
                !(values[1] is DateTime) ||
                !(values[2] is DateTime) ||
                !(values[3] is double))
            {
                return DependencyProperty.UnsetValue;
            }

            var duration = (TimeSpan)values[0];
            var showStartTime = (DateTime)values[1];
            var viewStartTime = (DateTime)values[2];
            var pixelsPerHour = (double)values[3];

            if (showStartTime < viewStartTime)
            {
                duration = duration - (viewStartTime - showStartTime);
            }

            return duration.TotalHours * pixelsPerHour;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace TivoAhoy.Phone.Converters
{
    public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public bool IsReversed { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = true;

            if (value == null)
            {
                result = false;
            }
            else
            {
                var stringValue= value as string;
                if (stringValue != null)
                {
                    result = !string.IsNullOrWhiteSpace(stringValue);
                }

                var enumerableValue= value as IEnumerable;
                if (enumerableValue != null)
                {
                    result = enumerableValue.OfType<object>().Any();
                }
            }

            if (this.IsReversed)
            {
                result = !result;
            }

            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

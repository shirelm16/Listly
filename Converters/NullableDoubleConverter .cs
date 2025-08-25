using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Converters
{
    public class NullableDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert from double? to string
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Convert from string to double?
            var str = value as string;
            if (string.IsNullOrWhiteSpace(str))
                return null;

            if (double.TryParse(str, out double result))
                return result;

            return null;
        }
    }
}

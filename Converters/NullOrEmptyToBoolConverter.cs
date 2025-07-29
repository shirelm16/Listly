using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Converters
{
    public class NullOrEmptyToBoolConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool result = !(value == null || (value is string str && string.IsNullOrWhiteSpace(str)));

            // If value is numeric, check if it's nullable
            if (value is int || value is double || value is float || value is long || value is decimal)
                result = true;

            if (Invert)
                result = !result;

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

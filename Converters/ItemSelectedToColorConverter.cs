using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listly.Converters
{
    public class ItemSelectedToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var normalColor = Application.Current.Resources.TryGetValue("Surface", out var surfaceColor)
                ? (Color)surfaceColor
                : Color.FromArgb("#FFFFFF");

            if (values == null || values.Length < 2)
                return normalColor;

            if (values[0] is HashSet<Guid> selectedIds &&
                values[1] is Guid itemId)
            {
                if (selectedIds.Contains(itemId))
                {
                    // Return a light blue for selected items
                    return Color.FromArgb("#E3F2FD");
                }
            }

            return normalColor;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System.Globalization;

namespace Listly.Converters
{
    public class CategoryDisplayToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string display || string.IsNullOrEmpty(display))
                return "🏷️";

            var enumerator = StringInfo.GetTextElementEnumerator(display);
            return enumerator.MoveNext() ? enumerator.GetTextElement() : "🏷️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

using System.Globalization;

namespace MauiStoreApp.Converters
{
    public class FirstLetterUppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && !string.IsNullOrEmpty(str))
            {
                // 🔥 Step 1: replace underscore with space
                str = str.Replace("_", " ");

                // 🔥 Step 2: capitalize each word
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

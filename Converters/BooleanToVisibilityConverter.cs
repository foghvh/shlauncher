using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace shlauncher.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = false;
            if (value is bool bValue)
            {
                flag = bValue;
            }

            bool inverse = false;
            if (parameter is string paramString)
            {
                bool.TryParse(paramString, out inverse);
            }
            else if (parameter is bool boolParam)
            {
                inverse = boolParam;
            }


            if (inverse)
            {
                return !flag ? Visibility.Visible : Visibility.Collapsed;
            }
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool inverse = false;
            if (parameter is string paramString)
            {
                bool.TryParse(paramString, out inverse);
            }
            else if (parameter is bool boolParam)
            {
                inverse = boolParam;
            }

            bool flag = (value is Visibility v) && v == Visibility.Visible;

            if (inverse)
            {
                return !flag;
            }
            return flag;
        }
    }
}
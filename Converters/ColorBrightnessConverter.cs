using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace shlauncher.Converters
{
    public class ColorBrightnessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush originalBrush)
            {
                Color originalColor = originalBrush.Color;
                float factor = 1.0f;
                if (parameter != null && float.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedFactor))
                {
                    factor = parsedFactor;
                }

                byte r = (byte)Math.Max(0, Math.Min(255, originalColor.R * factor));
                byte g = (byte)Math.Max(0, Math.Min(255, originalColor.G * factor));
                byte b = (byte)Math.Max(0, Math.Min(255, originalColor.B * factor));

                return new SolidColorBrush(Color.FromArgb(originalColor.A, r, g, b));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
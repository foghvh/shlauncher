using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace shlauncher.Converters
{
    public class VersionStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToUpperInvariant() switch
                {
                    "UPDATED" => Brushes.LightGreen,
                    "OUTDATED" => Brushes.OrangeRed,
                    "CHECKING..." => Brushes.LightSkyBlue,
                    "ERROR" => Brushes.Red,
                    "LOCAL N/A" => Brushes.Yellow,
                    "LOCAL ERROR" => Brushes.Orange,
                    "UNKNOWN" => Brushes.LightGray,
                    "DB ERROR" => Brushes.MediumVioletRed,
                    _ => Brushes.White,
                };
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
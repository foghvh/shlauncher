using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace shlauncher.Helpers
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not String enumString)
            {
                throw new ArgumentException("Parameter must be an enum name string.");
            }

            if (value == null || !Enum.IsDefined(value.GetType(), value))
            {
                // Or handle as appropriate, perhaps return false or throw
                // Depending on if value can be null or not a valid enum member
                return false;
            }

            // Ensure the parameter string can be parsed to the type of 'value'
            try
            {
                var enumValueFromParameter = Enum.Parse(value.GetType(), enumString);
                return enumValueFromParameter.Equals(value);
            }
            catch (ArgumentException) // Parameter string is not a member of the enum
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not String enumString)
            {
                throw new ArgumentException("Parameter must be an enum name string.");
            }
            // targetType here is the Enum type itself
            return Enum.Parse(targetType, enumString);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TaxiWPF.Converters
{
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string enumString = value.ToString();
            string targetValues = parameter.ToString();
            bool invert = false;

            if (targetValues.StartsWith("!"))
            {
                invert = true;
                targetValues = targetValues.Substring(1);
            }

            bool isVisible = false;
            foreach (string targetValue in targetValues.Split(';'))
            {
                if (enumString.Equals(targetValue, StringComparison.OrdinalIgnoreCase))
                {
                    isVisible = true;
                    break;
                }
            }

            return (invert ? !isVisible : isVisible) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

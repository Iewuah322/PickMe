using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace TaxiWPF.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Определяем, является ли строка пустой или null
            bool isNullOrEmpty = string.IsNullOrEmpty(value as string);

            // Проверяем, нужно ли инвертировать результат
            bool inverse = "inverse".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);

            if (inverse)
            {
                // Если инверсия: показать, когда строка ПУСТАЯ
                return isNullOrEmpty ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // Стандартное поведение: показать, когда строка НЕ ПУСТАЯ
                return isNullOrEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
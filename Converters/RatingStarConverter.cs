using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaxiWPF.Converters
{
    public class RatingStarConverter : IMultiValueConverter // <-- Убедись, что здесь IMultiValueConverter
    {
        // Метод Convert возвращает цвет для звезды
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] - CurrentRating (int)
            // values[1] - Star Tag (string)
            if (values.Length == 2 &&
                values[0] is int currentRating &&
                values[1] is string starNumberStr &&
                int.TryParse(starNumberStr, out int starNumber))
            {
                // Если номер звезды <= текущего рейтинга, возвращаем желтый цвет
                if (starNumber <= currentRating)
                {
                    // Цвет #FFC107
                    return new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
                }
            }
            // Иначе возвращаем серый цвет #DDCAB4
            return new SolidColorBrush(Color.FromRgb(0xDD, 0xCA, 0xB4));
        }

        // Метод ConvertBack (обязателен для IMultiValueConverter, но нам не нужен)
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Возвращаем DoNothing для каждого значения из MultiBinding
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}

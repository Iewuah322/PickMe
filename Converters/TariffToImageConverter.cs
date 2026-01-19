using System;
using System.Globalization;
using System.Windows.Data;

namespace TaxiWPF.Converters
{
    public class TariffToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tariff)
            {
                string relativePath = null;
                switch (tariff)
                {
                    case "Эконом":
                        relativePath = "Assets/Эконом.png";
                        break;
                    case "Комфорт":
                        relativePath = "Assets/Комфорт.png";
                        break;
                    case "Бизнес":
                        relativePath = "Assets/Бизнес.png";
                        break;
                    default:
                        return null;
                }
                
                if (!string.IsNullOrEmpty(relativePath))
                {
                    try
                    {
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        string fullPath = System.IO.Path.Combine(baseDirectory, relativePath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            return new System.Windows.Media.Imaging.BitmapImage(new Uri(fullPath));
                        }
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


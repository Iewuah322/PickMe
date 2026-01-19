using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace TaxiWPF.Converters
{
    public class RelativePathToAbsoluteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Используем parameter как путь, если value пустой
            string relativePath = parameter as string ?? value as string;
            
            // Проверяем, что нам передали непустой путь (string)
            if (!string.IsNullOrEmpty(relativePath))
            {
                try
                {
                    // Соединяем базовую директорию .exe с относительным путем
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string fullPath = Path.Combine(baseDirectory, relativePath);

                    // Если файл существует, создаем и возвращаем изображение
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // Загружаем сразу в память
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // Игнорируем кэш, чтобы видеть новые фото
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.EndInit();
                        bitmap.Freeze(); // Замораживаем для производительности
                        return bitmap;
                    }
                }
                catch
                {
                    // В случае ошибки возвращаем null
                    return null;
                }
            }
            // Если путь пустой или неверный, не показываем ничего
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

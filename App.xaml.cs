using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TaxiWPF.Views;
using GMap.NET;

namespace TaxiWPF
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Отключаем локальный кэш, чтобы карта не искала SQLite
                GMaps.Instance.Mode = AccessMode.ServerOnly;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка настройки GMap: {ex.Message}");
            }

            base.OnStartup(e);
            
            // Показываем окно загрузки с видео
            var splashScreen = new SplashScreenWindow();
            splashScreen.Show();
        }
    }
}

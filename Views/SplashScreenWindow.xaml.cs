using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Drawing;
using System.Windows.Interop;

namespace TaxiWPF.Views
{
    /// <summary>
    /// Логика взаимодействия для SplashScreenWindow.xaml
    /// </summary>
    public partial class SplashScreenWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private readonly string _videoPath;

        public SplashScreenWindow()
        {
            InitializeComponent();
            
            // Устанавливаем иконку окна
            SetWindowIcon();
            
            // Путь к видеофайлу (в папке Assets)
            _videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Загрузочный экран.mp4");
            
            // Если файл не найден, используем альтернативные пути
            if (!File.Exists(_videoPath))
            {
                _videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "Загрузочный экран.mp4");
            }
            
            if (!File.Exists(_videoPath))
            {
                _videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Загрузочный экран.mp4");
            }

            // Настраиваем размер окна в формате 16:9
            // Стандартные разрешения 16:9: 1920x1080, 1280x720, 1024x576, 854x480
            // Используем 1280x720 для хорошего баланса
            Width = 1280;
            Height = 720;
            
            // Центрируем окно
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Загружаем и воспроизводим видео
            Loaded += SplashScreenWindow_Loaded;
            
            // Таймер на случай, если видео не загрузится или не завершится
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Максимум 5 секунд
            };
            _timer.Tick += Timer_Tick;
        }

        private void SetWindowIcon()
        {
            try
            {
                // Пытаемся загрузить иконку из папки Assets
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Иконка 1 к 1.ico");
                
                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "Иконка 1 к 1.ico");
                }
                
                if (File.Exists(iconPath))
                {
                    using (Icon icon = new Icon(iconPath))
                    {
                        this.Icon = Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки иконки: {ex.Message}");
            }
        }

        private void SplashScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_videoPath))
            {
                try
                {
                    VideoPlayer.Source = new Uri(_videoPath, UriKind.Absolute);
                    VideoPlayer.Play();
                    _timer.Start();
                }
                catch (Exception ex)
                {
                    // Если не удалось загрузить видео, просто закрываем окно
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки видео: {ex.Message}");
                    CloseAndOpenLogin();
                }
            }
            else
            {
                // Если видео не найдено, закрываем окно сразу
                System.Diagnostics.Debug.WriteLine($"Видеофайл не найден: {_videoPath}");
                CloseAndOpenLogin();
            }
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            CloseAndOpenLogin();
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _timer.Stop();
            System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения видео: {e.ErrorException?.Message}");
            CloseAndOpenLogin();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            CloseAndOpenLogin();
        }

        private void CloseAndOpenLogin()
        {
            // Закрываем окно загрузки и открываем окно входа
            var loginWindow = new LoginView();
            loginWindow.Show();
            this.Close();
        }
    }
}


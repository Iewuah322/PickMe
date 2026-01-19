using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace TaxiWPF.Views
{
    public partial class LoadingWindow : Window
    {
        public event Action OnLoadingComplete;
        private readonly DispatcherTimer _timer;
        private readonly string _videoPath;

        public LoadingWindow()
        {
            InitializeComponent();

            _videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Загрузка.mp4");
            if (!File.Exists(_videoPath))
            {
                _videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "Загрузка.mp4");
            }
            if (!File.Exists(_videoPath))
            {
                _videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Загрузка.mp4");
            }

            Width = 1280;
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.Manual;

            // Set the icon for the loading window
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Иконка 1 к 1.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.RelativeOrAbsolute));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading icon: {ex.Message}");
            }

            Loaded += LoadingWindow_Loaded;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += Timer_Tick;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            // Синхронизируем позицию с окном-владельцем
            if (Owner != null)
            {
                Left = Owner.Left;
                Top = Owner.Top;
            }
        }

        private void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(_videoPath))
            {
                try
                {
                    LoadingVideo.Source = new Uri(_videoPath, UriKind.Absolute);
                    LoadingVideo.Play();
                    _timer.Start();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки видео: {ex.Message}");
                    CompleteLoading();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Видеофайл не найден: {_videoPath}");
                CompleteLoading();
            }
        }

        private void LoadingVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            CompleteLoading();
        }

        private void LoadingVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _timer.Stop();
            System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения видео: {e.ErrorException?.Message}");
            CompleteLoading();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            CompleteLoading();
        }

        private void CompleteLoading()
        {
            OnLoadingComplete?.Invoke();
        }
    }
}


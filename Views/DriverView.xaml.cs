using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using TaxiWPF.ViewModels;

namespace TaxiWPF.Views
{
    public partial class DriverView : Window
    {
        private DriverViewModel _viewModel;
        private GMapMarker _driverMarker;
        private GMapMarker _passengerMarker;
        private GMapRoute _currentRoute;

        public DriverView()
        {
            InitializeComponent();

            OpenStreetMapProvider.UserAgent = "MyTaxiApp/1.0";
            
            // Регистрируем Google Maps провайдер
            GMap.NET.MapProviders.GMapProviders.List.Add(TaxiWPF.MapProviders.GoogleMapProvider.Instance);

            // --- НОВЫЙ БЛОК: Подписка на DataContextChanged ---
            this.DataContextChanged += (sender, args) =>
            {
                // Отписываемся от старого ViewModel (на всякий случай)
                if (_viewModel != null)
                {
                    _viewModel.OnRouteRequired -= DrawRoute;
                }

                // Получаем НОВЫЙ ViewModel
                _viewModel = DataContext as DriverViewModel;

                // Подписываемся на его события
                if (_viewModel != null)
                {
                    _viewModel.OnRouteRequired += DrawRoute;
                }
            };
            // --- КОНЕЦ НОВОГО БЛОКА ---

            // (Код ниже у тебя уже есть)
            // Настройка карты - используем Google Maps
            MainMap.MapProvider = TaxiWPF.MapProviders.GoogleMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerOnly; // Отключен SQLite кэш для совместимости
            MainMap.Position = new PointLatLng(55.1599, 61.4026); // Челябинск
            MainMap.MinZoom = 5;
            MainMap.MaxZoom = 18;
            MainMap.Zoom = 12;
            MainMap.MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            MainMap.CanDragMap = true;
            MainMap.DragButton = MouseButton.Right;
        }

        private void DriverRatingButton_Checked(object sender, RoutedEventArgs e)
        {
            // Проверяем ViewModel (он у тебя в _viewModel)
            if (_viewModel == null) return;

            if (sender is RadioButton rb && rb.Tag is string ratingStr && int.TryParse(ratingStr, out int rating))
            {
                // Напрямую устанавливаем рейтинг в ViewModel
                _viewModel.ClientRating = rating;
                
                // Обновляем цвета звезд
                UpdateDriverRatingStars(rating);
            }
        }

        private void UpdateDriverRatingStars(int rating)
        {
            var yellowBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
            var grayBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));

            Star1.Foreground = rating >= 1 ? yellowBrush : grayBrush;
            Star2.Foreground = rating >= 2 ? yellowBrush : grayBrush;
            Star3.Foreground = rating >= 3 ? yellowBrush : grayBrush;
            Star4.Foreground = rating >= 4 ? yellowBrush : grayBrush;
            Star5.Foreground = rating >= 5 ? yellowBrush : grayBrush;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }



        private void DrawRoute(PointLatLng start, PointLatLng end)
        {
            // Очищаем старые маркеры и маршруты
            MainMap.Markers.Clear();

            if (start.IsEmpty || end.IsEmpty)
            {
                return; // Если точки пустые, просто очищаем карту
            }

            // Получаем маршрут от OpenStreetMap
            var route = OpenStreetMapProvider.Instance.GetRoute(start, end, false, false, 15);
            if (route != null)
            {
                _currentRoute = new GMapRoute(route.Points)
                {
                    Shape = new Path
                    {
                        Stroke = Brushes.Blue,
                        StrokeThickness = 4,
                        StrokeDashArray = new DoubleCollection { 2, 2 } // Пунктирная линия
                    }
                };

                MainMap.Markers.Add(_currentRoute);
            }



            // Добавляем маркер водителя (точка А)
            _driverMarker = new GMapMarker(start)
            {
                Shape = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = Brushes.Blue,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                }
            };
            MainMap.Markers.Add(_driverMarker);

            // Добавляем маркер пассажира (точка Б)
            _passengerMarker = new GMapMarker(end)
            {
                Shape = new Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = Brushes.Green,
                    Stroke = Brushes.White,
                    StrokeThickness = 2
                }
            };
            MainMap.Markers.Add(_passengerMarker);

            // Центрируем и масштабируем карту, чтобы показать оба маркера
            MainMap.ZoomAndCenterMarkers(null);
        }
    }
}

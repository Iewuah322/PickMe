using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using TaxiWPF.ViewModels;
using TaxiWPF.Models;
using BookingStep = TaxiWPF.ViewModels.BookingStep;


namespace TaxiWPF.Views
{
    public partial class ClientView : Window
    {
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private GMapMarker _markerA;
        private GMapMarker _markerB;
        private GMapRoute _currentRoute;
        private ClientViewModel _viewModel;

        public ClientView()
        {
            InitializeComponent();

            GMap.NET.MapProviders.OpenStreetMapProvider.UserAgent = "MyTaxiApp/1.0";
            
            // Регистрируем Google Maps провайдер
            GMap.NET.MapProviders.GMapProviders.List.Add(TaxiWPF.MapProviders.GoogleMapProvider.Instance);

            // Загружаем баннер из файла
            try
            {
                var bannerPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Банер.png");
                if (System.IO.File.Exists(bannerPath))
                {
                    BannerImage.Source = new BitmapImage(new Uri(bannerPath, UriKind.Absolute));
                }
            }
            catch { /* Игнорируем ошибки загрузки баннера */ }



            this.DataContextChanged += (sender, args) =>
            {
                // Отписываемся от старого ViewModel (на всякий случай)
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }

                // Получаем НОВЫЙ ViewModel, который нам передал LoginViewModel
                _viewModel = DataContext as ClientViewModel;
                
                // Подписываемся на изменение CurrentBookingStep для анимации
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    _viewModel.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ClientViewModel.CurrentBookingStep))
                        {
                            Dispatcher.BeginInvoke(new Action(() => HandleBookingStepChange()), System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        // При переходе к ReadyToOrder сворачиваем панель оплаты
                        if (e.PropertyName == nameof(ClientViewModel.CurrentBookingStep) && _viewModel.CurrentBookingStep == BookingStep.ReadyToOrder)
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (PaymentPanelTransform != null && PaymentSelectionPanel.Visibility == Visibility.Visible)
                                {
                                    AnimatePanel(PaymentSelectionPanel, PaymentPanelTransform, PaymentCollapseButton, true);
                                }
                            }), System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                    };
                }

                // Подписываемся на его события
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                }
            };



            _viewModel = DataContext as ClientViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            
            // Запускаем анимацию загрузки сразу при открытии окна (с наивысшим приоритетом)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                StartLoadingAnimation();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            
            // Инициализируем карту сразу при создании окна
            // Это позволит карте загрузиться во время показа анимации загрузки
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeMap();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            MainMap.CanDragMap = true;
            MainMap.DragButton = MouseButton.Right;

            MainMap.MouseLeftButtonUp += MainMap_MouseLeftButtonUp;
            MainMap.MouseMove += MainMap_MouseMove;
            // Переименуем старый обработчик
            MainMap.MouseLeftButtonDown += MainMap_PreviewMouseLeftButtonDown;
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
        private void RatingButton_Checked(object sender, RoutedEventArgs e)
        {
            // Проверяем ViewModel
            if (_viewModel == null) return;

            if (sender is RadioButton rb && rb.Tag is string ratingStr && int.TryParse(ratingStr, out int rating))
            {
                // Напрямую устанавливаем рейтинг в ViewModel
                _viewModel.CurrentRating = rating;
            }
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


        private void CloseProfilePanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsProfilePanelVisible = false;
            }
        }

        private void CloseSettingsPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.IsSettingsPanelVisible = false;
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var currentPassword = CurrentPasswordBox?.Password ?? "";
            var newPassword = NewPasswordBox?.Password ?? "";
            var confirmPassword = ConfirmPasswordBox?.Password ?? "";

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка");
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка");
                return;
            }

            if (newPassword.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов!", "Ошибка");
                return;
            }

            var userRepo = new TaxiWPF.Repositories.UserRepository();
            if (userRepo.ChangePassword(_viewModel.CurrentUser.user_id, currentPassword, newPassword))
            {
                MessageBox.Show("Пароль успешно изменен!", "Успех");
                CurrentPasswordBox.Password = "";
                NewPasswordBox.Password = "";
                ConfirmPasswordBox.Password = "";
            }
            else
            {
                MessageBox.Show("Неверный текущий пароль!", "Ошибка");
            }
        }

        private void SelectAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string destFolder = System.IO.Path.Combine(baseDirectory, "UserData", "Images");
                    System.IO.Directory.CreateDirectory(destFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(openFileDialog.FileName);
                    string destinationPath = System.IO.Path.Combine(destFolder, uniqueFileName);
                    System.IO.File.Copy(openFileDialog.FileName, destinationPath);
                    string relativePath = System.IO.Path.Combine("UserData", "Images", uniqueFileName);

                    _viewModel.CurrentUser.DriverPhotoUrl = relativePath;
                    var userRepo = new TaxiWPF.Repositories.UserRepository();
                    userRepo.UpdateUser(_viewModel.CurrentUser);

                    // Обновляем изображение
                    if (AvatarImage != null)
                    {
                        var converter = new TaxiWPF.Converters.RelativePathToAbsoluteConverter();
                        AvatarImage.Source = converter.Convert(relativePath, typeof(System.Windows.Media.ImageSource), null, null) as System.Windows.Media.Imaging.BitmapImage;
                    }

                    MessageBox.Show("Аватар успешно обновлен!", "Успех");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении фото: {ex.Message}", "Ошибка");
                }
            }
        }


        private void InitializeMap()
        {
            if (MainMap == null) return;

            try
            {
                // Используем Google Maps
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации карты: {ex.Message}");
            }
        }

        private void StartLoadingAnimation()
        {
            if (LoadingVideoContainer == null || LoadingVideo == null) return;

            try
            {
                string videoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Загрузка.mp4");
                if (!System.IO.File.Exists(videoPath))
                {
                    videoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets", "Загрузка.mp4");
                }
                if (!System.IO.File.Exists(videoPath))
                {
                    videoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Загрузка.mp4");
                }

                if (System.IO.File.Exists(videoPath))
                {
                    LoadingVideoContainer.Visibility = Visibility.Visible;
                    LoadingVideo.Source = new Uri(videoPath, UriKind.Absolute);
                    LoadingVideo.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Видеофайл загрузки не найден: {videoPath}");
                    // Если видео не найдено, просто скрываем контейнер
                    if (LoadingVideoContainer != null)
                    {
                        LoadingVideoContainer.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения видео загрузки: {ex.Message}");
                // При ошибке скрываем контейнер
                if (LoadingVideoContainer != null)
                {
                    LoadingVideoContainer.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadingVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (LoadingVideoContainer != null)
            {
                LoadingVideoContainer.Visibility = Visibility.Collapsed;
            }
            if (LoadingVideo != null)
            {
                LoadingVideo.Stop();
            }
        }

        private void LoadingVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения видео загрузки: {e.ErrorException?.Message}");
            if (LoadingVideoContainer != null)
            {
                LoadingVideoContainer.Visibility = Visibility.Collapsed;
            }
        }


        private void SearchDriverButton_Click(object sender, RoutedEventArgs e)
        {
            // Блокируем кнопку, чтобы предотвратить множественные нажатия
            if (_viewModel != null && !_viewModel.IsSearchDriverButtonEnabled)
            {
                return;
            }

            // Отключаем кнопку
            if (_viewModel != null)
            {
                _viewModel.IsSearchDriverButtonEnabled = false;
            }

            // Сразу запускаем поиск водителя
            if (_viewModel != null && _viewModel.FindTaxiCommand.CanExecute(null))
            {
                _viewModel.FindTaxiCommand.Execute(null);
            }
        }

        private void CollapseAddressPanel_Click(object sender, RoutedEventArgs e)
        {
            bool isCollapsed = AddressPanelTransform.Y > 1;
            AnimatePanel(AddressInputPanel, AddressPanelTransform, AddressCollapseButton, !isCollapsed);
        }

        private void ExpandAddressPanel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (AddressInputPanel.Visibility == Visibility.Visible)
            {
                bool isCollapsed = AddressPanelTransform.Y > 1;
                AnimatePanel(AddressInputPanel, AddressPanelTransform, AddressCollapseButton, !isCollapsed);
            }
        }

        private void CollapseTariffPanel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool isCollapsed = TariffPanelTransform.Y > 1;
            AnimatePanel(TariffSelectionPanel, TariffPanelTransform, TariffCollapseButton, !isCollapsed);
        }

        private void ExpandTariffPanel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (TariffSelectionPanel.Visibility == Visibility.Visible)
            {
                bool isCollapsed = TariffPanelTransform.Y > 1;
                AnimatePanel(TariffSelectionPanel, TariffPanelTransform, TariffCollapseButton, !isCollapsed);
            }
        }

        private void CollapsePaymentPanel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            bool isCollapsed = PaymentPanelTransform.Y > 1;
            AnimatePanel(PaymentSelectionPanel, PaymentPanelTransform, PaymentCollapseButton, !isCollapsed);
        }

        private void ExpandPaymentPanel_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (PaymentSelectionPanel.Visibility == Visibility.Visible)
            {
                bool isCollapsed = PaymentPanelTransform.Y > 1;
                AnimatePanel(PaymentSelectionPanel, PaymentPanelTransform, PaymentCollapseButton, !isCollapsed);
            }
        }
        
        // Обработчик для кнопки "Заказать такси" - запускает анимацию выезжания панели
        private void StartBookingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentBookingStep = BookingStep.AddressInput;
                // Анимация запустится автоматически через HandleBookingStepChange
            }
        }

        private void AnimatePanel(Border panel, TranslateTransform transform, Button collapseButton, bool collapse)
        {
            if (transform == null) return;

            double targetY = collapse ? panel.Height - 60 : 0; // При сворачивании оставляем 60px видимыми
            string buttonContent = collapse ? "^" : "v";

            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = targetY,
                Duration = TimeSpan.FromSeconds(0.3),
                EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.YProperty, animation);
            if (collapseButton != null)
            {
                collapseButton.Content = buttonContent;
            }
        }

        private void HandleBookingStepChange()
        {
            if (_viewModel == null) return;

            // Сбрасываем анимации для всех панелей
            if (AddressPanelTransform != null)
            {
                AddressPanelTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }
            if (TariffPanelTransform != null)
            {
                TariffPanelTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }
            if (PaymentPanelTransform != null)
            {
                PaymentPanelTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }

            // Анимация при открытии панели адресов
            if (_viewModel.CurrentBookingStep == BookingStep.AddressInput && AddressInputPanel.Visibility == Visibility.Visible)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (AddressPanelTransform != null)
                    {
                        AddressPanelTransform.Y = 0;
                        AddressCollapseButton.Content = "v";
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            // Анимация при открытии панели тарифов
            else if (_viewModel.CurrentBookingStep == BookingStep.TariffSelection && TariffSelectionPanel.Visibility == Visibility.Visible)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (TariffPanelTransform != null)
                    {
                        TariffPanelTransform.Y = 0;
                        TariffCollapseButton.Content = "v";
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            // Анимация при открытии панели оплаты
            else if (_viewModel.CurrentBookingStep == BookingStep.PaymentSelection && PaymentSelectionPanel.Visibility == Visibility.Visible)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (PaymentPanelTransform != null)
                    {
                        PaymentPanelTransform.Y = 0;
                        PaymentCollapseButton.Content = "v";
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        

        private void MainMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (_viewModel == null || _viewModel.CurrentOrderState != OrderState.Idle || _viewModel.CurrentBookingStep != BookingStep.AddressInput)
            {
                return; // Не даем ставить точки, если мы не в режиме ввода адреса
            }

            var point = e.GetPosition(MainMap);
            var latLng = MainMap.FromLocalToLatLng((int)point.X, (int)point.Y);

            if (RbPointA.IsChecked == true)
            {
                // ... (код для маркера А)
                _viewModel.PointA = latLng;
            }
            else if (RbPointB.IsChecked == true)
            {
                // ... (код для маркера Б)
                _viewModel.PointB = latLng;
            }

            // Если обе точки установлены, строим и рисуем маршрут
            if (!_viewModel.PointA.IsEmpty && !_viewModel.PointB.IsEmpty)
            {
                DrawRoute(_viewModel.PointA, _viewModel.PointB);
            }
        }



        private void DrawRoute(PointLatLng start, PointLatLng end)
        {
            var route = OpenStreetMapProvider.Instance.GetRoute(start, end, false, false, 15);
            if (route != null)
            {
                if (_currentRoute != null)
                {
                    MainMap.Markers.Remove(_currentRoute);
                }

                _currentRoute = new GMapRoute(route.Points) { /* ... */ };
                MainMap.Markers.Add(_currentRoute);

                // Убираем автоматический зум
                // MainMap.ZoomAndCenterMarkers(null);
            }
        }
       

        private void MainMap_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _dragStartPoint = e.GetPosition(MainMap);
        }

        private void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point currentPoint = e.GetPosition(MainMap);
                // Если мышь сдвинулась на 3 пикселя, считаем это перетаскиванием
                if (Math.Abs(currentPoint.X - _dragStartPoint.X) > 3 ||
                    Math.Abs(currentPoint.Y - _dragStartPoint.Y) > 3)
                {
                    _isDragging = true;
                }
            }
        }

        private void MainMap_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            if (_viewModel == null || _viewModel.CurrentOrderState != OrderState.Idle || _viewModel.CurrentBookingStep != BookingStep.AddressInput)
            {
                _isDragging = false; // Сбрасываем флаг перетаскивания
                return; // Выходим из метода
            }

            // Если это не было перетаскивание, то это клик
            if (!_isDragging)
            {
                var latLng = MainMap.FromLocalToLatLng((int)_dragStartPoint.X, (int)_dragStartPoint.Y);

                if (RbPointA.IsChecked == true)
                {
                    _viewModel.PointA = latLng;
                }
                else if (RbPointB.IsChecked == true)
                {
                    _viewModel.PointB = latLng;
                }

                // После установки точки, переключим RadioButton
                // если была выбрана точка А, следующий клик будет для точки Б
                if (RbPointA.IsChecked == true) RbPointB.IsChecked = true;
            }
            _isDragging = false;
        }


        

        // Добавим подписку на изменение точек в ViewModel, чтобы перерисовывать карту
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(ClientViewModel.PointA) || e.PropertyName == nameof(ClientViewModel.PointB)))
            {
                // Обновляем маркеры
                UpdateMarkers();

                if (!_viewModel.PointA.IsEmpty && !_viewModel.PointB.IsEmpty)
                {
                    // Рисуем маршрут
                    DrawRoute(_viewModel.PointA, _viewModel.PointB);
                }
            }
        }

        private void UpdateMarkers()
        {
            if (_viewModel == null || MainMap == null) return;

            // Удаляем старые маркеры
            if (_markerA != null) MainMap.Markers.Remove(_markerA);
            if (_markerB != null) MainMap.Markers.Remove(_markerB);
            if (_currentRoute != null) MainMap.Markers.Remove(_currentRoute);

            // Добавляем маркер А, если есть координата
            if (!_viewModel.PointA.IsEmpty)
            {
                _markerA = new GMapMarker(_viewModel.PointA)
                {
                    Shape = new Ellipse { Width = 20, Height = 20, Fill = Brushes.Blue, Stroke = Brushes.White, StrokeThickness = 2 }
                };
                MainMap.Markers.Add(_markerA);
            }

            // Добавляем маркер Б
            if (!_viewModel.PointB.IsEmpty)
            {
                _markerB = new GMapMarker(_viewModel.PointB)
                {
                    Shape = new Ellipse { Width = 20, Height = 20, Fill = Brushes.Red, Stroke = Brushes.White, StrokeThickness = 2 }
                };
                MainMap.Markers.Add(_markerB);
            }

            // Рисуем маршрут, если обе точки установлены
            if (!_viewModel.PointA.IsEmpty && !_viewModel.PointB.IsEmpty)
            {
                DrawRoute(_viewModel.PointA, _viewModel.PointB);
            }
        }


    }



}

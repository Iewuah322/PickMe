using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using GMap.NET;
using TaxiWPF.Models;
using GMap.NET.MapProviders;
using TaxiWPF.Services;
using System.Linq;
using System.Windows;
using TaxiWPF.Repositories;


namespace TaxiWPF.ViewModels
{
    public class DriverViewModel : INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly Car _currentCar;
        private DispatcherTimer _orderTimer;
        private readonly WalletRepository _walletRepository;
        

        private bool _isOnline = true;
        private bool _isOnOrder = false;
        private string _panelTitle = "Доступные заказы";
        private string _statusMessage;
        private readonly RatingRepository _ratingRepository;

        private Order _selectedOrder;
        private Order _acceptedOrder;

        public event Action<PointLatLng, PointLatLng> OnRouteRequired;
        public PointLatLng DriverLocation { get; set; }
        public PointLatLng PassengerLocation { get; set; }

        public ObservableCollection<Order> AvailableOrders { get; set; }

        // --- НОВОЕ: Логика для оценки клиента ---
        private bool _isRatingClient = false;
        private int _clientRating = 0;
        public bool IsRatingClient
        {
            get => _isRatingClient;
            set { _isRatingClient = value; OnPropertyChanged(); UpdatePanelVisibility(); }
        }
        public int ClientRating
        {
            get => _clientRating;
            set { _clientRating = value; OnPropertyChanged(); (RateClientCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }
        public ICommand RateClientCommand { get; }
        public ICommand SkipRateClientCommand { get; }
        // ---------------------------------------


        public Order SelectedOrder
        {
            get => _selectedOrder;
            set 
            { 
                // Сбрасываем IsSelected для всех заказов
                foreach (var order in AvailableOrders)
                {
                    order.IsSelected = false;
                }
                
                _selectedOrder = value; 
                
                // Устанавливаем IsSelected для выбранного заказа
                if (_selectedOrder != null)
                {
                    _selectedOrder.IsSelected = true;
                }
                
                OnPropertyChanged(); 
                UpdateCommandStates(); 
            }

        }
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }

        }

        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                _isOnline = value;
                OnPropertyChanged();
                UpdateStatus();
            }
        }

        public bool IsOnOrder
        {
            get => _isOnOrder;
            set
            {
                _isOnOrder = value;
                OnPropertyChanged();
                UpdatePanelVisibility();
                UpdateCommandStates(); // <-- Вот это добавленное исправление
            }
        }

        public Order AcceptedOrder
        {
            get => _acceptedOrder;
            set { _acceptedOrder = value; OnPropertyChanged(); }
        }

        public string PanelTitle
        {
            get => _panelTitle;
            set { _panelTitle = value; OnPropertyChanged(); }
        }

        // --- Свойства для панели профиля ---
        private bool _isProfilePanelVisible = false;
        public bool IsProfilePanelVisible
        {
            get => _isProfilePanelVisible;
            set { _isProfilePanelVisible = value; OnPropertyChanged(); }
        }

        public User CurrentDriver => _currentUser;

        public ICommand AcceptOrderCommand { get; }
        public ICommand DeclineOrderCommand { get; }
        // --- НОВЫЕ КОМАНДЫ ---
        public ICommand ArrivedCommand { get; }
        public ICommand StartTripCommand { get; }
        public ICommand CompleteOrderCommand { get; }
        public ICommand GoToDashboardCommand { get; }
        public ICommand StopAcceptingOrdersCommand { get; }
        public ICommand ToggleProfilePanelCommand { get; }
        public ICommand ContactSupportCommand { get; }
        public ICommand StartAcceptingOrdersCommand { get; }

        // ---------------------

        // --- НОВЫЕ СВОЙСТВА ДЛЯ ВИДИМОСТИ КНОПОК ---
        public bool CanShowAcceptDecline => IsOnline && !IsOnOrder && SelectedOrder != null;
        public bool CanShowArrived => IsOnOrder && AcceptedOrder?.Status == OrderState.DriverEnRoute;
        public bool CanShowStartTrip => IsOnOrder && AcceptedOrder?.Status == OrderState.DriverArrived;
        public bool CanShowCompleteTrip => IsOnOrder && AcceptedOrder?.Status == OrderState.TripInProgress;
        // -----------------------------------------


        public DriverViewModel(User user, Car car)
        {
            _currentUser = user;
            _currentCar = car;
            _walletRepository = new WalletRepository();
            _ratingRepository = new RatingRepository();
            StatusMessage = $"На линии на {_currentCar.ModelName}. Ожидание заказов...";
            AvailableOrders = new ObservableCollection<Order>();

            // --- ИЗМЕНЕНЫ CanExecute ---
            AcceptOrderCommand = new RelayCommand<Order>(async (order) => await AcceptOrder(order), (order) => order != null && IsOnline && !IsOnOrder);
            DeclineOrderCommand = new RelayCommand<Order>((order) => DeclineOrder(order), (order) => order != null && IsOnline && !IsOnOrder);

            // --- НОВЫЕ КОМАНДЫ ---
            ArrivedCommand = new RelayCommand(DriverArrived, () => CanShowArrived);
            StartTripCommand = new RelayCommand(StartTrip, () => CanShowStartTrip);
            CompleteOrderCommand = new RelayCommand(CompleteOrder, () => CanShowCompleteTrip);
            RateClientCommand = new RelayCommand(RateClient, () => ClientRating > 0);
            SkipRateClientCommand = new RelayCommand(SkipRateClient);
            GoToDashboardCommand = new RelayCommand(GoToDashboard, () => !IsOnOrder);
            StopAcceptingOrdersCommand = new RelayCommand(StopAcceptingOrders, () => !IsOnOrder);
            ToggleProfilePanelCommand = new RelayCommand(() => IsProfilePanelVisible = !IsProfilePanelVisible);
            ContactSupportCommand = new RelayCommand(ContactSupport);
            StartAcceptingOrdersCommand = new RelayCommand(StartAcceptingOrders, () => !IsOnline);
            // ---------------------

            DriverLocation = new PointLatLng(55.17, 61.38); // Заглушка

            _orderTimer = new DispatcherTimer();
            _orderTimer.Interval = TimeSpan.FromSeconds(5);
            _orderTimer.Tick += FetchNewOrders;
            if (IsOnline) _orderTimer.Start();

            // --- НОВОЕ: Подписываемся на "радиостанцию" ---
            OrderService.Instance.OrderUpdated += OnOrderUpdated;
        }

        // --- НОВЫЙ МЕТОД: Обработчик событий от OrderService ---
        private void OnOrderUpdated(Order updatedOrder)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Если это наш текущий заказ, обновляем его статус
                if (AcceptedOrder != null && updatedOrder.order_id == AcceptedOrder.order_id)
                {
                    AcceptedOrder = updatedOrder; // Получаем обновленный заказ
                    OnPropertyChanged(nameof(AcceptedOrder));
                    UpdateStatus(); // Обновляем StatusMessage
                    UpdateCommandStates(); // Обновляем видимость кнопок
                }
                else if (IsOnline && updatedOrder.Status == OrderState.Searching && !AvailableOrders.Any(o => o.order_id == updatedOrder.order_id))
                {
                    // Добавляем новый заказ в список
                    AvailableOrders.Add(updatedOrder);
                }
                else if (updatedOrder.Status == OrderState.Archived)
                {
                    // Если заказ отменен, убираем его
                    var orderToRemove = AvailableOrders.FirstOrDefault(o => o.order_id == updatedOrder.order_id);
                    if (orderToRemove != null) AvailableOrders.Remove(orderToRemove);
                }
            });
        }

        private void UpdateStatus()
        {
            if (IsOnline)
            {
                if (IsOnOrder)
                {
                    _orderTimer.Stop();
                    // --- НОВОЕ: Более детальный статус ---
                    switch (AcceptedOrder.Status)
                    {
                        case OrderState.DriverEnRoute:
                            StatusMessage = $"Едем к клиенту: {AcceptedOrder.PointA}";
                            break;
                        case OrderState.DriverArrived:
                            StatusMessage = "Ожидаем клиента...";
                            break;
                        case OrderState.TripInProgress:
                            StatusMessage = $"Везем клиента в: {AcceptedOrder.PointB}";
                            break;
                        case OrderState.TripCompleted:
                            StatusMessage = "Поездка завершена. Оцените клиента.";
                            break;
                    }
                }
                else
                {
                    _orderTimer.Start();
                    StatusMessage = "Вы на линии. Ожидание заказов...";
                    FetchNewOrders(null, null); // Сразу ищем
                }
            }
            else
            {
                _orderTimer.Stop();
                AvailableOrders.Clear();
                StatusMessage = "Вы не на линии.";
            }
        }

        private void FetchNewOrders(object sender, EventArgs e)
        {
            if (!IsOnline || IsOnOrder) return;

            var orders = OrderService.Instance.GetAvailableOrders();

            // --- УМНОЕ ОБНОВЛЕНИЕ СПИСКА ---

            // 1. Находим ID заказов, которые УЖЕ есть в списке
            var existingOrderIds = AvailableOrders.Select(o => o.order_id).ToList();

            // 2. Находим ID заказов, которые ПРИШЛИ с сервера
            var newOrderIds = orders.Select(o => o.order_id).ToList();

            // 3. Добавляем новые (которых нет в списке)
            foreach (var order in orders)
            {
                if (!existingOrderIds.Contains(order.order_id))
                {
                    AvailableOrders.Add(order);
                }
            }

            // 4. Удаляем пропавшие (которые есть в списке, но не пришли с сервера)
            // (Мы должны создать копию .ToList() для безопасного удаления из коллекции)
            foreach (var order in AvailableOrders.ToList())
            {
                if (!newOrderIds.Contains(order.order_id))
                {
                    // Если это был выбранный заказ, сбрасываем выбор
                    if (SelectedOrder == order)
                    {
                        SelectedOrder = null;
                    }
                    AvailableOrders.Remove(order);
                }
            }
        }

        private async Task AcceptOrder(Order order)
        {
            if (order == null) return;

            AcceptedOrder = order;
            IsOnOrder = true; // Это скроет список и покажет панель заказа

            // --- ИЗМЕНЕНО: Просто вызываем сервис ---
            var driverInfo = new Driver
            {
                driver_id = _currentUser.user_id,
                full_name = _currentUser.full_name,
                car_model = _currentCar.ModelName,
                license_plate = _currentCar.LicensePlate,
                DriverPhotoUrl = _currentUser.DriverPhotoUrl, // Передаем фото водителя
                CarPhotoUrl = _currentCar.MainImageUrl
                // (Здесь можно добавить и фото)
            };
            OrderService.Instance.AcceptOrder(AcceptedOrder, driverInfo);
            // -------------------------------------

            var point = await GetPointFromAddress(AcceptedOrder.PointA);
            if (!point.IsEmpty)
            {
                PassengerLocation = point;
                OnRouteRequired?.Invoke(DriverLocation, PassengerLocation);
            }

            AvailableOrders.Remove(order);
            SelectedOrder = null;
        }

        private void DeclineOrder(Order order)
        {
            if (order == null) return;
            AvailableOrders.Remove(order);
            if (SelectedOrder == order)
            {
                SelectedOrder = null;
            }
        }

        // --- НОВЫЕ МЕТОДЫ ДЛЯ КНОПОК ---
        private void DriverArrived()
        {
            OrderService.Instance.DriverArrived(AcceptedOrder);
        }

        private void StartTrip()
        {
            OrderService.Instance.StartTrip(AcceptedOrder);
            // (Очищаем маршрут до клиента и строим до точки Б)
            DriverLocation = PassengerLocation; // Мы "телепортировались" к клиенту
            GetPointFromAddress(AcceptedOrder.PointB).ContinueWith(task => {
                Application.Current.Dispatcher.Invoke(() => // Обернули в Dispatcher
                {
                    if (!task.Result.IsEmpty)
                    {
                        PassengerLocation = task.Result;
                        OnRouteRequired?.Invoke(DriverLocation, PassengerLocation);
                    }
                });
            });
        }

        private void CompleteOrder()
        {
            OrderService.Instance.CompleteOrder(AcceptedOrder);

            // --- НАЧАЛО ИЗМЕНЕНИЙ ---
            // Регистрируем транзакцию в любом случае, но передаем способ оплаты.
            // Репозиторий сам решит, влиять ли ей на баланс.
            _walletRepository.AddEarning(
                _currentUser.user_id,
                AcceptedOrder.TotalPrice,
                AcceptedOrder.order_id,
                AcceptedOrder.PaymentMethod
            );
            // --- КОНЕЦ ИЗМЕНЕНИЙ ---

            OnRouteRequired?.Invoke(PointLatLng.Empty, PointLatLng.Empty);
            IsRatingClient = true;
        }

        // --- НОВЫЕ МЕТОДЫ ОЦЕНКИ ---
        private void RateClient()
        {
            // --- НАЧАЛО ИЗМЕНЕНИЙ ---
            bool success = _ratingRepository.AddRating(
                AcceptedOrder,
                _currentUser.user_id,
                AcceptedOrder.OrderClient.client_id,
                ClientRating,
                false, false, false // Эти флаги только для оценки водителя
            );

            if (success)
            {
                MessageBox.Show($"Клиенту поставлена оценка: {ClientRating}");
            }
            else
            {
                MessageBox.Show("Не удалось сохранить оценку.", "Ошибка");
            }

            ResetAfterRating();
            // --- КОНЕЦ ИЗМЕНЕНИЙ ---
        }

        private void SkipRateClient()
        {
            MessageBox.Show("Оценка клиента пропущена.");
            ResetAfterRating();
        }

        private void ResetAfterRating()
        {
            AcceptedOrder.DriverRated = true;
            // (Если и клиент оценил, можно архивировать)
            if (AcceptedOrder.ClientRated) OrderService.Instance.ArchiveOrder(AcceptedOrder);

            AcceptedOrder = null;
            IsOnOrder = false;
            IsRatingClient = false;
            ClientRating = 0;
            UpdateStatus(); // Возвращаемся на линию
        }
        // -----------------------------


        // --- НОВЫЕ МЕТОДЫ ОБНОВЛЕНИЯ UI ---
        private void UpdateCommandStates()
        {
            (AcceptOrderCommand as RelayCommand<Order>)?.RaiseCanExecuteChanged();
            (DeclineOrderCommand as RelayCommand<Order>)?.RaiseCanExecuteChanged();
            (ArrivedCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartTripCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CompleteOrderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (GoToDashboardCommand as RelayCommand)?.RaiseCanExecuteChanged();

            OnPropertyChanged(nameof(CanShowAcceptDecline));
            OnPropertyChanged(nameof(CanShowArrived));
            OnPropertyChanged(nameof(CanShowStartTrip));
            OnPropertyChanged(nameof(CanShowCompleteTrip));
        }

        private void UpdatePanelVisibility()
        {
            OnPropertyChanged(nameof(IsOnOrder));
            OnPropertyChanged(nameof(IsRatingClient));
            PanelTitle = IsOnOrder ? (IsRatingClient ? "Оценка клиента" : "Текущий заказ") : "Доступные заказы";
        }
        // ---------------------------------

        private async Task<PointLatLng> GetPointFromAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return PointLatLng.Empty;
            return await Task.Run(() =>
            {
                var pointLatLng = GMapProviders.OpenStreetMap.GetPoint(address, out var status);
                if (status == GeoCoderStatusCode.OK && pointLatLng.HasValue)
                {
                    return pointLatLng.Value;
                }
                return PointLatLng.Empty;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void GoToDashboard()
        {
            // 1. Находим скрытое окно Дашборда по его типу ViewModel
            var dashboardWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext is DriverDashboardViewModel);

            if (dashboardWindow != null)
            {
                dashboardWindow.Show(); // Показываем его
            }

            // 2. Закрываем текущее окно (DriverView)
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    // Отписываемся от сервиса, чтобы не было утечек памяти
                    OrderService.Instance.OrderUpdated -= OnOrderUpdated;
                    window.Close();
                    break;
                }
            }
        }

        private void StopAcceptingOrders()
        {
            IsOnline = false;
            _orderTimer?.Stop();
            
            // Возвращаемся на Dashboard
            GoToDashboard();
        }

        private void StartAcceptingOrders()
        {
            IsOnline = true;
            _orderTimer?.Start();
            (StartAcceptingOrdersCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopAcceptingOrdersCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ContactSupport()
        {
            MessageBox.Show("Для связи с поддержкой напишите на support@taxiwpf.ru или позвоните +7 (800) 123-45-67", 
                "Поддержка", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

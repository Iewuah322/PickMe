using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GMap.NET;
using TaxiWPF.Models;
using TaxiWPF.Services;
using GMap.NET.MapProviders;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TaxiWPF.Repositories;
using TaxiWPF.Views;
using Newtonsoft.Json.Linq;


namespace TaxiWPF.ViewModels
{
    // Мы убрали enum OrderState отсюда. Теперь он в Models/OrderState.cs

    public enum BookingStep
    {
        Idle,
        AddressInput,
        TariffSelection,
        PaymentSelection,
        ReadyToOrder
    }

    public class ClientViewModel : INotifyPropertyChanged
    {


        private readonly WalletRepository _walletRepository;
        private bool _isCardPayment = true;
        private PaymentCard _selectedSavedCard;
        private bool _rememberCard;

        // Поля для форматирования и валидации
        private string _cardNumber;
        private string _cardExpiry;
        private string _cardCVV;
        private string _cardNumberError;
        private string _cardExpiryError;
        private string _cvvError;

        public bool IsCardPayment { get => _isCardPayment; set { _isCardPayment = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCashPayment)); } }
        public bool IsCashPayment { get => !_isCardPayment; set { _isCardPayment = !value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCashPayment)); } }
        public ObservableCollection<PaymentCard> SavedCards { get; set; }

        public bool IsNewCardSelected => SelectedSavedCard != null && string.IsNullOrEmpty(SelectedSavedCard.CardNumber);
        public bool RememberCard { get => _rememberCard; set { _rememberCard = value; OnPropertyChanged(); } }


        // --- Private поля ---
        private string _fromAddress;
        private string _toAddress;
        private string _selectedTariff;
        private decimal _totalPrice;
        private string _statusMessage;
        private PointLatLng _pointA;
        private PointLatLng _pointB;
        private double _distanceKm;
        private User _currentUser;
        private bool _isProfilePanelVisible = false;
        private OrderState _currentOrderState = OrderState.Idle;
        private Order _currentOrder;
        private DispatcherTimer _orderTimer;
        private readonly RatingRepository _ratingRepository;
        private readonly OrderRepository _orderRepository;
        private readonly Random _random = new Random();
       

        // Таймеры УДАЛЕНЫ

        private int _driverDistance;
        private int _currentRating = 0;
        private bool _wasPolite;
        private bool _wasClean;
        private bool _goodDriving;

        public bool WasPolite { get => _wasPolite; set { _wasPolite = value; OnPropertyChanged(); } }
        public bool WasClean { get => _wasClean; set { _wasClean = value; OnPropertyChanged(); } }
        public bool GoodDriving { get => _goodDriving; set { _goodDriving = value; OnPropertyChanged(); } }

        public PaymentCard SelectedSavedCard
        {
            get => _selectedSavedCard;
            set
            {
                _selectedSavedCard = value;
                OnPropertyChanged();
                LoadCardFromSelection(value);
                OnPropertyChanged(nameof(IsNewCardSelected));
            }
        }
        public string CardNumber
        {
            get => _cardNumber;
            set
            {
                string digits = new string(value.Where(char.IsDigit).ToArray());
                if (digits.Length > 16) digits = digits.Substring(0, 16);
                string formatted = string.Join(" ", Enumerable.Range(0, (digits.Length + 3) / 4)
                    .Select(i => digits.Substring(i * 4, Math.Min(4, digits.Length - i * 4))));
                _cardNumber = formatted;
                OnPropertyChanged();
                ValidateCardNumber();
            }
        }

        public string CardExpiry
        {
            get => _cardExpiry;
            set
            {
                string digits = new string(value.Where(char.IsDigit).ToArray());
                if (digits.Length > 4) digits = digits.Substring(0, 4);
                string formatted = digits;
                if (digits.Length > 2) formatted = digits.Insert(2, "/");
                _cardExpiry = formatted;
                OnPropertyChanged();
                ValidateCardExpiry();
            }
        }
        public string CardCVV
        {
            get => _cardCVV;
            set
            {
                string digits = new string(value.Where(char.IsDigit).ToArray());
                if (digits.Length > 3) digits = digits.Substring(0, 3);
                _cardCVV = digits;
                OnPropertyChanged();
                ValidateCvv();
            }
        }
        public string CardNumberError { get => _cardNumberError; set { _cardNumberError = value; OnPropertyChanged(); } }
        public string CardExpiryError { get => _cardExpiryError; set { _cardExpiryError = value; OnPropertyChanged(); } }
        public string CvvError { get => _cvvError; set { _cvvError = value; OnPropertyChanged(); } }


        public ICommand CancelSearchCommand { get; }
        public ICommand SkipRatingCommand { get; }

        public OrderState CurrentOrderState
        {
            get => _currentOrderState;
            set 
            { 
                _currentOrderState = value; 
                OnPropertyChanged(); 
                // Обновляем все связанные свойства
                OnPropertyChanged(nameof(IsInputMode));
                OnPropertyChanged(nameof(IsOrderSummaryVisible));
                OnPropertyChanged(nameof(IsSearchingState));
                OnPropertyChanged(nameof(IsRatingVisible));
                OnPropertyChanged(nameof(CanRequestNewOrder));
                OnPropertyChanged(nameof(IsSearchDriverButtonVisible));
                UpdateButtonStates(); 
            }
        }
        public Order CurrentOrder
        {
            get => _currentOrder;
            set 
            { 
                _currentOrder = value; 
                OnPropertyChanged();
                // Обновляем отображение информации о водителе
                OnPropertyChanged(nameof(DriverInfoText));
            }
        }

        // --- Свойства для UI ---
        public bool IsInputMode => CurrentOrderState == OrderState.Idle;
        public bool IsOrderSummaryVisible => CurrentOrderState != OrderState.Idle && CurrentOrderState != OrderState.TripCompleted;
        public bool IsSearchingState => CurrentOrderState == OrderState.Searching;
        public bool IsRatingVisible => CurrentOrderState == OrderState.TripCompleted;
        public bool CanRequestNewOrder => CurrentOrderState == OrderState.Idle;

        // --- Новые состояния для нового интерфейса ---
        private BookingStep _currentBookingStep = BookingStep.Idle;
        public BookingStep CurrentBookingStep
        {
            get => _currentBookingStep;
            set { _currentBookingStep = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsAddressInputVisible)); OnPropertyChanged(nameof(IsTariffSelectionVisible)); OnPropertyChanged(nameof(IsPaymentSelectionVisible)); OnPropertyChanged(nameof(IsReadyToOrder)); OnPropertyChanged(nameof(IsSearchDriverButtonVisible)); }
        }

        public bool IsAddressInputVisible => CurrentBookingStep == BookingStep.AddressInput;
        public bool IsTariffSelectionVisible => CurrentBookingStep == BookingStep.TariffSelection;
        public bool IsPaymentSelectionVisible => CurrentBookingStep == BookingStep.PaymentSelection;
        public bool IsReadyToOrder => CurrentBookingStep == BookingStep.ReadyToOrder;
        public bool IsSearchDriverButtonVisible => CurrentBookingStep == BookingStep.ReadyToOrder && CurrentOrderState == OrderState.Idle;
        
        private bool _isSearchDriverButtonEnabled = true;
        public bool IsSearchDriverButtonEnabled
        {
            get => _isSearchDriverButtonEnabled;
            set { _isSearchDriverButtonEnabled = value; OnPropertyChanged(); }
        }

        public ICommand StartBookingCommand { get; }
        public ICommand SelectTariffCommand { get; }
        public ICommand ConfirmPaymentCommand { get; }
        public ICommand EditOrderCommand { get; }

        public string DriverInfoText { get; private set; }
        public int CurrentRating
        {
            get => _currentRating;
            set { 
                _currentRating = value; OnPropertyChanged();
                (RateDriverCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // Кнопка StartTripCommand УДАЛЕНА
        public ICommand RateDriverCommand { get; }
        public event Action<PointLatLng, PointLatLng> VirtualDriverAssigned;

        public User CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }
        public bool IsProfilePanelVisible
        {
            get => _isProfilePanelVisible;
            set { _isProfilePanelVisible = value; OnPropertyChanged(); }
        }
        public ObservableCollection<Order> PastTrips { get; set; }
        public ICommand ToggleProfilePanelCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenPromoCodeCommand { get; }
        public ICommand ClosePromoCodePanelCommand { get; }
        public ICommand ActivatePromoCodeCommand { get; }
        
        private bool _isSettingsPanelVisible = false;
        public bool IsSettingsPanelVisible
        {
            get => _isSettingsPanelVisible;
            set { _isSettingsPanelVisible = value; OnPropertyChanged(); }
        }
        
        // Промокод
        private bool _isPromoCodePanelVisible = false;
        public bool IsPromoCodePanelVisible
        {
            get => _isPromoCodePanelVisible;
            set { _isPromoCodePanelVisible = value; OnPropertyChanged(); }
        }
        
        private string _promoCodeInput = "";
        public string PromoCodeInput
        {
            get => _promoCodeInput;
            set { _promoCodeInput = value; OnPropertyChanged(); }
        }
        
        private string _promoCodeStatusMessage = "";
        public string PromoCodeStatusMessage
        {
            get => _promoCodeStatusMessage;
            set { _promoCodeStatusMessage = value; OnPropertyChanged(); }
        }
        
        private string _promoCodeStatusBackground = "#FFF3E0";
        public string PromoCodeStatusBackground
        {
            get => _promoCodeStatusBackground;
            set { _promoCodeStatusBackground = value; OnPropertyChanged(); }
        }
        
        private string _promoCodeStatusForeground = "#E65100";
        public string PromoCodeStatusForeground
        {
            get => _promoCodeStatusForeground;
            set { _promoCodeStatusForeground = value; OnPropertyChanged(); }
        }
        
        private bool _hasActivePromoCode = false;
        public bool HasActivePromoCode
        {
            get => _hasActivePromoCode;
            set { _hasActivePromoCode = value; OnPropertyChanged(); }
        }
        
        private int _promoCodeRidesLeft = 0;
        public int PromoCodeRidesLeft
        {
            get => _promoCodeRidesLeft;
            set { _promoCodeRidesLeft = value; OnPropertyChanged(); }
        }
        
        private const string VALID_PROMO_CODE = "Магазин_электроники_Никитенко_15";
        public string FromAddress
        {
            get => _fromAddress;
            set { _fromAddress = value; OnPropertyChanged(); }
        }
        public ICommand UpdatePointACommand { get; }
        public ICommand UpdatePointBCommand { get; }
        public PointLatLng PointA
        {
            get => _pointA;
            set
            {
                _pointA = value;
                OnPropertyChanged();
                UpdateAddressFromPoint(value, (address) => FromAddress = address).ConfigureAwait(false);
                RecalculatePrice();
            }
        }
        public PointLatLng PointB
        {
            get => _pointB;
            set
            {
                _pointB = value;
                OnPropertyChanged();
                UpdateAddressFromPoint(value, (address) => ToAddress = address).ConfigureAwait(false);
                RecalculatePrice();
            }
        }
        public double DistanceKm
        {
            get => _distanceKm;
            set { _distanceKm = value; OnPropertyChanged(); }
        }
        public string ToAddress
        {
            get => _toAddress;
            set { _toAddress = value; OnPropertyChanged(); }
        }
        public string SelectedTariff
        {
            get => _selectedTariff;
            set { _selectedTariff = value; OnPropertyChanged(); RecalculatePrice(); }
        }
        public decimal TotalPrice
        {
            get => _totalPrice;
            set { _totalPrice = value; OnPropertyChanged(); }
        }
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }
        public ObservableCollection<string> Tariffs { get; set; }
        public ICommand FindTaxiCommand { get; }
        public ICommand OpenTripDetailsCommand { get; }

        public ICommand ContactSupportCommand { get; }
        private readonly SupportRepository _supportRepository;

        public ClientViewModel(User loggedInUser)
        {
            _supportRepository = new SupportRepository();
            _walletRepository = new WalletRepository();
            SavedCards = new ObservableCollection<PaymentCard>();
            _supportRepository = new SupportRepository();
            _ratingRepository = new RatingRepository();
            _orderRepository = new OrderRepository();
            CurrentUser = loggedInUser;
            PastTrips = new ObservableCollection<Order>();
            ToggleProfilePanelCommand = new RelayCommand(ToggleProfilePanel);
            OpenSettingsCommand = new RelayCommand(() => IsSettingsPanelVisible = true);
            OpenPromoCodeCommand = new RelayCommand(() => { IsPromoCodePanelVisible = true; IsProfilePanelVisible = false; });
            ClosePromoCodePanelCommand = new RelayCommand(() => { IsPromoCodePanelVisible = false; PromoCodeStatusMessage = ""; });
            ActivatePromoCodeCommand = new RelayCommand(ActivatePromoCode);
            LoadSavedCards();
            Tariffs = new ObservableCollection<string> { "Эконом", "Комфорт", "Бизнес" };
            SelectedTariff = Tariffs[0];
            UpdatePointACommand = new RelayCommand(async () => await UpdatePointFromAddress(FromAddress, p => PointA = p));
            UpdatePointBCommand = new RelayCommand(async () => await UpdatePointFromAddress(ToAddress, p => PointB = p));

            FindTaxiCommand = new RelayCommand(FindTaxi, () => CanRequestNewOrder && !string.IsNullOrWhiteSpace(FromAddress) && !string.IsNullOrWhiteSpace(ToAddress));

            // Команда StartTripCommand УДАЛЕНА

            RateDriverCommand = new RelayCommand(RateDriver, () => CurrentRating > 0);
            CancelSearchCommand = new RelayCommand(CancelSearch, () => CurrentOrderState == OrderState.Searching);
            SkipRatingCommand = new RelayCommand(SkipRating);
            
            // Новые команды для нового интерфейса
            StartBookingCommand = new RelayCommand(() => CurrentBookingStep = BookingStep.AddressInput);
            SelectTariffCommand = new RelayCommand<string>(SelectTariff);
            ConfirmPaymentCommand = new RelayCommand(ConfirmPayment);
            EditOrderCommand = new RelayCommand(EditOrder);

            // --- НОВОЕ: Подписываемся на "радиостанцию" ---
            OrderService.Instance.OrderUpdated += OnOrderUpdated;
            ContactSupportCommand = new RelayCommand(OpenSupportChat);
            OpenTripDetailsCommand = new RelayCommand<Order>(OpenTripDetails, order => order != null);
        }

        private void OpenSupportChat()
        {
            // Этот вызов теперь будет работать без ошибок
            var ticket = SupportService.Instance.GetOrCreateTicketForUser(CurrentUser);

            ticket.UserInfo = CurrentUser;

            var chatView = new SupportChatView(CurrentUser, ticket);
            chatView.Show();
        }

        private void OpenTripDetails(Order order)
        {
            if (order == null)
            {
                return;
            }

            var orderDetails = _orderRepository.GetOrderById(order.order_id);
            if (orderDetails == null)
            {
                MessageBox.Show("Не удалось загрузить данные поездки.");
                return;
            }

            var detailsView = new TripDetailsView(CurrentUser, orderDetails);
            detailsView.Show();
        }

        private void LoadSavedCards()
        {
            SavedCards.Clear();
            SavedCards.Add(new PaymentCard { CardNumber = null }); // Для выбора "Новая карта"
            var cardsFromRepo = _walletRepository.GetSavedCards(CurrentUser.user_id);
            foreach (var card in cardsFromRepo)
            {
                SavedCards.Add(card);
            }
            SelectedSavedCard = SavedCards.FirstOrDefault();
        }

        private void LoadCardFromSelection(PaymentCard card)
        {
            if (card == null || string.IsNullOrEmpty(card.CardNumber))
            {
                CardNumber = "";
                CardExpiry = "";
                CardCVV = "";
                RememberCard = true;
            }
            else
            {
                CardNumber = card.CardNumber;
                CardExpiry = card.CardExpiry;
                CardCVV = "";
                RememberCard = false;
            }
        }



        // --- НОВЫЙ МЕТОД: Обработчик событий от OrderService ---
        private void OnOrderUpdated(Order updatedOrder)
        {
            System.Diagnostics.Debug.WriteLine($"[ClientViewModel] OnOrderUpdated получен: заказ #{updatedOrder.order_id}, статус: {updatedOrder.Status}");
            System.Diagnostics.Debug.WriteLine($"[ClientViewModel] CurrentOrder: {(CurrentOrder != null ? $"#{CurrentOrder.order_id}" : "null")}");
            
            // Если это не наш заказ, игнорируем
            if (CurrentOrder == null || updatedOrder.order_id != CurrentOrder.order_id)
            {
                System.Diagnostics.Debug.WriteLine($"[ClientViewModel] Заказ #{updatedOrder.order_id} проигнорирован (не наш)");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[ClientViewModel] Обновляем статус заказа на {updatedOrder.Status}");
            
            // Важно: Обновляем UI в основном потоке
            Application.Current.Dispatcher.Invoke(async () =>
            {
                // Обновляем и заказ, и состояние
                CurrentOrder = updatedOrder;
                CurrentOrderState = updatedOrder.Status;
                System.Diagnostics.Debug.WriteLine($"[ClientViewModel] CurrentOrderState обновлен на {CurrentOrderState}");

                // Обновляем текстовые сообщения для клиента
                switch (updatedOrder.Status)
                {
                    case OrderState.DriverEnRoute:
                        // (Здесь можно было бы симулировать DriverDistance)
                        _driverDistance = 5; // Заглушка
                        UpdateDriverInfoText();
                        StatusMessage = "Водитель в пути";
                        break;
                    case OrderState.DriverArrived:
                        StatusMessage = "Водитель прибыл. Ожидайте начала поездки.";
                        DriverInfoText = $"{CurrentOrder.AssignedDriver.full_name} ожидает";
                        OnPropertyChanged(nameof(DriverInfoText));
                        break;
                    case OrderState.TripInProgress:
                        StatusMessage = "Поездка началась...";
                        DriverInfoText = $"Вас везет {CurrentOrder.AssignedDriver.full_name}";
                        OnPropertyChanged(nameof(DriverInfoText));
                        break;
                    case OrderState.TripCompleted:
                        StatusMessage = "Поездка завершена! Оцените водителя.";
                        CurrentRating = 0; // Сбрасываем рейтинг
                        // Обновляем историю поездок, если панель профиля открыта
                        if (IsProfilePanelVisible)
                        {
                            await LoadPastTrips();
                        }
                        break;
                    case OrderState.Archived:
                        // Заказ ушел в архив (например, водитель отменил)
                        ResetToIdleState();
                        break;
                }
            });
        }

        private void ConfirmPayment()
        {
            if (IsCardPayment)
            {
                bool isCardValid = true;
                if (IsNewCardSelected)
                {
                    ValidateCardNumber(true);
                    ValidateCardExpiry(true);
                    ValidateCvv(true);
                    isCardValid = string.IsNullOrEmpty(CardNumberError) &&
                                  string.IsNullOrEmpty(CardExpiryError) &&
                                  string.IsNullOrEmpty(CvvError);
                }

                if (!isCardValid)
                {
                    MessageBox.Show("Пожалуйста, введите корректные данные карты.", "Ошибка оплаты");
                    return;
                }
                
                // Если карта новая и пользователь хочет ее запомнить, добавляем ее сразу
                if (IsNewCardSelected && RememberCard)
                {
                    var card = new PaymentCard { CardNumber = this.CardNumber, CardExpiry = this.CardExpiry };
                    _walletRepository.AddCard(CurrentUser.user_id, card);
                    // Обновляем список сохраненных карт после добавления новой
                    LoadSavedCards();
                    // Выбираем только что добавленную карту
                    SelectedSavedCard = SavedCards.LastOrDefault();
                }
            }

            CurrentBookingStep = BookingStep.ReadyToOrder;
        }

        // --- ИЗМЕНЕНО: Метод FindTaxi (убрали async/await и таймеры) ---
        private async void FindTaxi()
        {
            if (!CanRequestNewOrder) return;

            // Валидация уже прошла на этапе ConfirmPayment, но на всякий случай оставим проверку заполненности
            if (IsCardPayment && IsNewCardSelected)
            {
                 // Если вдруг пользователь вернулся и что-то стер (хотя UI это не позволяет легко сделать без смены шага)
                 // Можно добавить повторную валидацию, если нужно, но ConfirmPayment должен гарантировать
            }

            CurrentOrderState = OrderState.Searching;
            StatusMessage = "Ищем водителя...";

            // Уменьшаем счетчик промокода, если используется тариф со скидкой
            if (HasActivePromoCode && PromoCodeRidesLeft > 0 && (SelectedTariff == "Комфорт" || SelectedTariff == "Бизнес"))
            {
                PromoCodeRidesLeft--;
                if (PromoCodeRidesLeft == 0)
                {
                    HasActivePromoCode = false;
                    PromoCodeStatusMessage = "";
                }
            }

            var newOrder = new Order
            {
                PointA = this.FromAddress,
                PointB = this.ToAddress,
                Tariff = this.SelectedTariff,
                TotalPrice = this.TotalPrice,
                OrderClient = new Client { client_id = CurrentUser.user_id, full_name = CurrentUser.full_name },
                PaymentMethod = IsCardPayment ? "Карта" : "Наличные"
            };

            CurrentOrder = await OrderService.Instance.SubmitOrderAsync(newOrder);
            StartOrderAssignmentTimer();
        }

        private void StartOrderAssignmentTimer()
        {
            if (_orderTimer != null)
            {
                _orderTimer.Stop();
                _orderTimer.Tick -= OrderTimer_Tick;
            }

            var delaySeconds = _random.Next(10, 31);
            _orderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(delaySeconds)
            };
            _orderTimer.Tick += OrderTimer_Tick;
            _orderTimer.Start();
        }

        private void OrderTimer_Tick(object sender, EventArgs e)
        {
            _orderTimer.Stop();
            _orderTimer.Tick -= OrderTimer_Tick;

            if (CurrentOrderState != OrderState.Searching || CurrentOrder == null)
            {
                return;
            }

            var virtualDriver = new Driver
            {
                driver_id = _random.Next(1000, 9999),
                full_name = "Виртуальный водитель",
                car_model = "Такси",
                license_plate = "А000АА74",
                rating = 4.8m,
                phone = "+7 (900) 000-00-00"
            };

            OrderService.Instance.AcceptOrder(CurrentOrder, virtualDriver);
            VirtualDriverAssigned?.Invoke(PointA, PointB);
        }

        private void ValidateCardNumber(bool force = false)
        {
            CardNumberError = (string.IsNullOrWhiteSpace(CardNumber) || CardNumber.Replace(" ", "").Length < 16) ? "Введите полный номер карты." : null;
            if (force) (FindTaxiCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        private void ValidateCardExpiry(bool force = false)
        {
            CardExpiryError = (string.IsNullOrWhiteSpace(CardExpiry) || CardExpiry.Length < 5) ? "Введите срок." : null;
            if (force) (FindTaxiCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        private void ValidateCvv(bool force = false)
        {
            CvvError = (string.IsNullOrWhiteSpace(CardCVV) || CardCVV.Length < 3) ? "Введите CVV." : null;
            if (force) (FindTaxiCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void CancelSearch()
        {
            // (В идеале, надо оповестить сервис, но пока просто сбросим у себя)
            OrderService.Instance.ArchiveOrder(CurrentOrder); // Используем Archive
            if (_orderTimer != null)
            {
                _orderTimer.Stop();
                _orderTimer.Tick -= OrderTimer_Tick;
            }
            ResetToIdleState();
        }

        public void HandlePickupReached()
        {
            if (CurrentOrder == null)
            {
                return;
            }

            OrderService.Instance.DriverArrived(CurrentOrder);
            var startTripTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            startTripTimer.Tick += (s, e) =>
            {
                startTripTimer.Stop();
                OrderService.Instance.StartTrip(CurrentOrder);
            };
            startTripTimer.Start();
        }

        public void HandleTripCompleted()
        {
            if (CurrentOrder == null)
            {
                return;
            }

            OrderService.Instance.CompleteOrder(CurrentOrder);
        }

        private async void RateDriver()
        {
            // --- НАЧАЛО ИЗМЕНЕНИЙ ---
            bool success = await _ratingRepository.AddRatingAsync(
                CurrentOrder,
                _currentUser.user_id,
                CurrentOrder.AssignedDriver.driver_id,
                CurrentRating,
                WasPolite,
                WasClean,
                GoodDriving
            );

            if (success)
            {
                MessageBox.Show($"Спасибо за вашу оценку: {CurrentRating} звезд(ы)!", "Рейтинг");
            }
            else
            {
                MessageBox.Show("Не удалось сохранить оценку.", "Ошибка");
            }

            CurrentOrder.ClientRated = true;
            OrderService.Instance.ArchiveOrder(CurrentOrder);
            ResetToIdleState();
            
            // Обновляем историю поездок после оценки
            if (IsProfilePanelVisible)
            {
                await LoadPastTrips();
            }

        }

        private async void SkipRating()
        {
            MessageBox.Show("Оценка пропущена.", "Рейтинг");
            CurrentOrder.ClientRated = true;
            OrderService.Instance.ArchiveOrder(CurrentOrder);
            ResetToIdleState();
            
            // Обновляем историю поездок после пропуска оценки
            if (IsProfilePanelVisible)
            {
                await LoadPastTrips();
            }
        }

        private void ResetToIdleState()
        {
            CurrentOrderState = OrderState.Idle;
            CurrentOrder = null;
            StatusMessage = "";
            CurrentRating = 0;
            WasPolite = false;
            WasClean = false;
            GoodDriving = false;
            
            // Сбрасываем шаг бронирования на начальный
            CurrentBookingStep = BookingStep.Idle;
            
            // Очищаем адреса для нового заказа
            FromAddress = "";
            ToAddress = "";
            SelectedTariff = null;
            TotalPrice = 0;
        }

        private void UpdateButtonStates()
        {
            // OnPropertyChanged(nameof(IsStartTripButtonVisible)); // <-- УДАЛЕНО
            OnPropertyChanged(nameof(IsRatingVisible));
            OnPropertyChanged(nameof(CanRequestNewOrder));
            OnPropertyChanged(nameof(IsInputMode));
            OnPropertyChanged(nameof(IsOrderSummaryVisible));
            OnPropertyChanged(nameof(IsSearchingState));
            (FindTaxiCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelSearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }


        // --- ТВОИ СТАРЫЕ МЕТОДЫ (ОСТАЮТСЯ БЕЗ ИЗМЕНЕНИЙ) ---
        #region (Методы, которые не менялись)
        private async void ToggleProfilePanel()
        {
            IsProfilePanelVisible = !IsProfilePanelVisible;
            if (IsProfilePanelVisible)
            {
                // --- НАЧАЛО ИЗМЕНЕНИЙ ---
                // 1. Загружаем самую свежую версию пользователя из БД (с обновленным рейтингом)
                var updatedUser = await new UserRepository().GetUserByUsernameAsync(CurrentUser.username);
                if (updatedUser != null)
                {
                    CurrentUser = updatedUser;
                }

                // 2. Загружаем актуальную историю поездок
                await LoadPastTrips();
                // --- КОНЕЦ ИЗМЕНЕНИЙ ---
            }
        }

        private async Task LoadPastTrips()
        {
            System.Diagnostics.Debug.WriteLine($"[LoadPastTrips] Загрузка истории поездок для пользователя user_id={CurrentUser?.user_id}");
            PastTrips.Clear();
            var trips = await _orderRepository.GetPastOrdersByClientIdAsync(CurrentUser.user_id);
            System.Diagnostics.Debug.WriteLine($"[LoadPastTrips] Получено {trips.Count} поездок из репозитория");
            foreach (var trip in trips)
            {
                PastTrips.Add(trip);
                System.Diagnostics.Debug.WriteLine($"[LoadPastTrips] Добавлена поездка #{trip.order_id}, статус: {trip.Status}, откуда: {trip.PointA}, куда: {trip.PointB}");
            }
            System.Diagnostics.Debug.WriteLine($"[LoadPastTrips] В PastTrips теперь {PastTrips.Count} поездок");
        }

        private void CloseSettingsPanel()
        {
            IsSettingsPanelVisible = false;
        }

        private decimal _economyPrice;
        private decimal _comfortPrice;
        private decimal _businessPrice;

        public decimal EconomyPrice { get => _economyPrice; set { _economyPrice = value; OnPropertyChanged(); } }
        public decimal ComfortPrice { get => _comfortPrice; set { _comfortPrice = value; OnPropertyChanged(); } }
        public decimal BusinessPrice { get => _businessPrice; set { _businessPrice = value; OnPropertyChanged(); } }

        private void RecalculatePrice()
        {
            if (PointA.IsEmpty || PointB.IsEmpty)
            {
                TotalPrice = 0;
                EconomyPrice = 0;
                ComfortPrice = 0;
                BusinessPrice = 0;
                return;
            }

            var route = GMap.NET.MapProviders.OpenStreetMapProvider.Instance.GetRoute(PointA, PointB, false, false, 15);
            if (route != null)
            {
                DistanceKm = route.Distance; // Расстояние в км
                decimal basePrice = 50;
                decimal pricePerKm = 15;
                
                // Рассчитываем базовые цены для всех тарифов
                decimal economyBase = basePrice + ((decimal)DistanceKm * pricePerKm * 1.0m);
                decimal comfortBase = basePrice + ((decimal)DistanceKm * pricePerKm * 1.5m);
                decimal businessBase = basePrice + ((decimal)DistanceKm * pricePerKm * 2.0m);
                
                EconomyPrice = economyBase;
                
                // Применяем скидку 15% для Комфорт и Бизнес, если есть активный промокод
                if (HasActivePromoCode && PromoCodeRidesLeft > 0)
                {
                    ComfortPrice = Math.Round(comfortBase * 0.85m, 2);
                    BusinessPrice = Math.Round(businessBase * 0.85m, 2);
                }
                else
                {
                    ComfortPrice = comfortBase;
                    BusinessPrice = businessBase;
                }
                
                // Устанавливаем TotalPrice в зависимости от выбранного тарифа
                switch (SelectedTariff)
                {
                    case "Комфорт":
                        TotalPrice = ComfortPrice;
                        break;
                    case "Бизнес":
                        TotalPrice = BusinessPrice;
                        break;
                    default:
                        TotalPrice = EconomyPrice;
                        break;
                }
            }
            else
            {
                TotalPrice = 0;
                EconomyPrice = 0;
                ComfortPrice = 0;
                BusinessPrice = 0;
                StatusMessage = "Не удалось построить маршрут.";
            }
        }


        private void UpdateDriverInfoText()
        {
            if (CurrentOrder?.AssignedDriver != null)
            {
                DriverInfoText = $"{CurrentOrder.AssignedDriver.full_name} ({CurrentOrder.AssignedDriver.car_model ?? "Машина"}) - {_driverDistance} мин до вас";
            }
            else
            {
                DriverInfoText = "";
            }
            OnPropertyChanged(nameof(DriverInfoText));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            // Автоматический переход к выбору тарифа при заполнении адресов
            if ((propertyName == nameof(FromAddress) || propertyName == nameof(ToAddress) || 
                 propertyName == nameof(PointA) || propertyName == nameof(PointB)) && 
                CurrentBookingStep == BookingStep.AddressInput)
            {
                CheckAddressesAndProceed();
            }
        }

        private async Task UpdateAddressFromPoint(PointLatLng point, Action<string> setAddressAction)
        {
            var addresses = await Task.Run(async () =>
            {
                // Используем Google Geocoding API для получения точных адресов с номерами домов
                // Примечание: Google Geocoding API требует API ключ для коммерческого использования
                // Для тестирования можно использовать без ключа с ограничениями
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                        var lat = point.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var lng = point.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        // Google Geocoding API - формат: широта, долгота (lat, lng)
                        // Примечание: для работы нужен API ключ, но можно попробовать без него
                        var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&language=ru&result_type=street_address|route|premise";
                        
                        var response = await httpClient.GetStringAsync(url);
                        
                        if (string.IsNullOrWhiteSpace(response))
                        {
                            throw new Exception("Empty response");
                        }
                        
                        var json = JObject.Parse(response);
                        
                        // Проверяем статус ответа Google API
                        var status = json["status"]?.ToString();
                        if (status != "OK")
                        {
                            System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Google API status: {status}");
                            throw new Exception($"Google API status: {status}");
                        }
                        
                        var results = json["results"] as JArray;
                        if (results != null && results.Count > 0)
                        {
                            // Берем первый результат (самый точный)
                            var firstResult = results[0];
                            var formattedAddress = firstResult["formatted_address"]?.ToString();
                            
                            if (!string.IsNullOrWhiteSpace(formattedAddress))
                            {
                                System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Using Google formatted address: {formattedAddress}");
                                return formattedAddress;
                            }
                            
                            // Если formatted_address нет, собираем из address_components
                            var addressComponents = firstResult["address_components"] as JArray;
                            if (addressComponents != null)
                            {
                                var parts = new List<string>();
                                string streetNumber = null;
                                string route = null;
                                string locality = null;
                                
                                foreach (var component in addressComponents)
                                {
                                    var types = component["types"] as JArray;
                                    var longName = component["long_name"]?.ToString();
                                    
                                    if (types != null && !string.IsNullOrWhiteSpace(longName))
                                    {
                                        foreach (var type in types)
                                        {
                                            var typeStr = type.ToString();
                                            if (typeStr == "street_number")
                                            {
                                                streetNumber = longName;
                                            }
                                            else if (typeStr == "route")
                                            {
                                                route = longName;
                                            }
                                            else if (typeStr == "locality" || typeStr == "administrative_area_level_1")
                                            {
                                                if (string.IsNullOrWhiteSpace(locality))
                                                    locality = longName;
                                            }
                                        }
                                    }
                                }
                                
                                // Собираем адрес: номер дома, улица, город
                                if (!string.IsNullOrWhiteSpace(streetNumber))
                                    parts.Add($"д. {streetNumber}");
                                if (!string.IsNullOrWhiteSpace(route))
                                    parts.Add(route);
                                if (!string.IsNullOrWhiteSpace(locality))
                                    parts.Add(locality);
                                
                                if (parts.Count > 0)
                                {
                                    var result = string.Join(", ", parts);
                                    System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Using Google components address: {result}");
                                    return result;
                                }
                            }
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    // Логируем ошибки HTTP для отладки
                    System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Google API HTTP error: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    // Логируем другие ошибки для отладки
                    System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Google API error: {ex.Message}");
                }
                
                // Fallback на Nominatim (OpenStreetMap) если Google не сработал
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "TaxiWPF/1.0");
                        
                        var lat = point.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var lon = point.Lng.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        var url = $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}&addressdetails=1&accept-language=ru&zoom=18";
                        
                        var response = await httpClient.GetStringAsync(url);
                        var json = JObject.Parse(response);
                        
                        var address = json["address"];
                        if (address != null)
                        {
                            var parts = new List<string>();
                            
                            // Пробуем получить номер дома
                            var houseNumber = address["house_number"]?.ToString();
                            var road = address["road"]?.ToString() ?? address["pedestrian"]?.ToString();
                            var city = address["city"] ?? address["town"] ?? address["village"] ?? address["municipality"] ?? address["county"];
                            
                            if (!string.IsNullOrWhiteSpace(houseNumber))
                                parts.Add($"д. {houseNumber}");
                            if (!string.IsNullOrWhiteSpace(road))
                                parts.Add(road);
                            if (city != null && !string.IsNullOrWhiteSpace(city.ToString()))
                                parts.Add(city.ToString());
                            
                            if (parts.Count > 0)
                            {
                                var result = string.Join(", ", parts);
                                System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Using Nominatim address: {result}");
                                return result;
                            }
                            
                            // Если не получилось собрать из компонентов, используем display_name, но обрезаем его
                            var displayName = json["display_name"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(displayName))
                            {
                                // Берем только первые части адреса (до запятой с городом)
                                var nameParts = displayName.Split(',');
                                if (nameParts.Length >= 2)
                                {
                                    var result = string.Join(", ", nameParts.Take(Math.Min(3, nameParts.Length)));
                                    System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Using Nominatim display_name (trimmed): {result}");
                                    return result;
                                }
                                System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Using Nominatim display_name: {displayName}");
                                return displayName;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] Nominatim fallback error: {ex.Message}");
                }
                
                // Если ничего не сработало, возвращаем координаты
                System.Diagnostics.Debug.WriteLine($"[UpdateAddressFromPoint] All geocoding failed, returning coordinates");
                return $"Координаты: {point.Lat:F6}, {point.Lng:F6}";
            });
            setAddressAction(addresses);
        }

        private async Task UpdatePointFromAddress(string address, Action<PointLatLng> setPointAction)
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            var point = await Task.Run(async () =>
            {
                // Используем Google Geocoding API для более точного поиска адресов с номерами домов
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.Timeout = TimeSpan.FromSeconds(5);
                        var encodedAddress = Uri.EscapeDataString(address);
                        // Google Geocoding API - формат: адрес
                        // Примечание: для работы нужен API ключ, но можно попробовать без него
                        var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&language=ru";
                        
                        var response = await httpClient.GetStringAsync(url);
                        var json = JObject.Parse(response);
                        
                        var status = json["status"]?.ToString();
                        if (status == "OK")
                        {
                            var results = json["results"] as JArray;
                            if (results != null && results.Count > 0)
                            {
                                var firstResult = results[0];
                                var location = firstResult["geometry"]?["location"];
                                if (location != null)
                                {
                                    var lat = location["lat"]?.ToObject<double>();
                                    var lng = location["lng"]?.ToObject<double>();
                                    if (lat.HasValue && lng.HasValue)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[UpdatePointFromAddress] Google found: {address} -> {lat.Value}, {lng.Value}");
                                        return new PointLatLng(lat.Value, lng.Value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[UpdatePointFromAddress] Google API status: {status}");
                        }
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdatePointFromAddress] Google API HTTP error: {httpEx.Message}");
                }
                catch (Exception ex) 
                { 
                    System.Diagnostics.Debug.WriteLine($"[UpdatePointFromAddress] Google API error: {ex.Message}");
                }
                
                // Fallback на Nominatim (OpenStreetMap) если Yandex не сработал
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "TaxiWPF/1.0");
                        var encodedAddress = Uri.EscapeDataString(address);
                        var url = $"https://nominatim.openstreetmap.org/search?format=json&q={encodedAddress}&addressdetails=1&limit=1&accept-language=ru";
                        var response = await httpClient.GetStringAsync(url);
                        var jsonArray = JArray.Parse(response);
                        
                        if (jsonArray.Count > 0)
                        {
                            var firstResult = jsonArray[0];
                            var lat = firstResult["lat"]?.ToObject<double>();
                            var lon = firstResult["lon"]?.ToObject<double>();
                            if (lat.HasValue && lon.HasValue)
                            {
                                return new PointLatLng(lat.Value, lon.Value);
                            }
                        }
                    }
                }
                catch (Exception) { /* Молчание */ }
                
                return PointLatLng.Empty;
            });
            if (!point.IsEmpty) { setPointAction(point); }
        }

        // --- Новые методы для нового интерфейса ---
        private void EditOrder()
        {
            // Возвращаемся к вводу адресов
            CurrentBookingStep = BookingStep.AddressInput;
        }

        private void ActivatePromoCode()
        {
            if (string.IsNullOrWhiteSpace(PromoCodeInput))
            {
                PromoCodeStatusMessage = "Введите промокод";
                PromoCodeStatusBackground = "#FFF3E0";
                PromoCodeStatusForeground = "#E65100";
                return;
            }

            if (PromoCodeInput.Trim() == VALID_PROMO_CODE)
            {
                HasActivePromoCode = true;
                PromoCodeRidesLeft = 3;
                PromoCodeStatusMessage = "Промокод активирован! Скидка 15% на 3 поездки в классах Комфорт и Бизнес.";
                PromoCodeStatusBackground = "#E8F5E9";
                PromoCodeStatusForeground = "#2E7D32";
                PromoCodeInput = "";
                
                // Пересчитываем цены с учетом скидки
                RecalculatePrice();
            }
            else
            {
                PromoCodeStatusMessage = "Неверный промокод";
                PromoCodeStatusBackground = "#FFEBEE";
                PromoCodeStatusForeground = "#C62828";
            }
        }

        private void SelectTariff(string tariff)
        {
            SelectedTariff = tariff;
            // Пересчитываем цены (скидка применится автоматически)
            RecalculatePrice();
            CurrentBookingStep = BookingStep.PaymentSelection;
        }

        private void CheckAddressesAndProceed()
        {
            if (!string.IsNullOrWhiteSpace(FromAddress) && !string.IsNullOrWhiteSpace(ToAddress) && 
                !PointA.IsEmpty && !PointB.IsEmpty && CurrentBookingStep == BookingStep.AddressInput)
            {
                CurrentBookingStep = BookingStep.TariffSelection;
            }
        }
        #endregion
    }
}

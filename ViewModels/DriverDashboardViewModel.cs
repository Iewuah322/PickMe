using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using TaxiWPF.Models;
using TaxiWPF.Repositories;
using TaxiWPF.Views;
using Microsoft.Win32;
using System.IO;
using TaxiWPF.Services;




namespace TaxiWPF.ViewModels
{
    public class DriverDashboardViewModel : INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly WalletRepository _walletRepository;

        private decimal _totalBalance;
        private string _cardNumber;
        private string _cardExpiry;
        private string _cardCVV;
        private bool _rememberCard;
        private decimal _withdrawAmount;
        private bool _isWithdrawPanelVisible = false;
        private string _amountError;
        private string _cardNumberError;
        private string _cardExpiryError;
        private string _cvvError;
        private readonly UserRepository _userRepository;
        private bool _isProfilePanelVisible = false;
        private bool _isCarSelectionVisible = false;
        private bool _isCarDetailsVisible = false;
        private Car _selectedCar;
        private Car _carForEditing;
        private readonly CarRepository _carRepository;
        
        public ObservableCollection<ChartDataPoint> DailyEarnings { get; set; }
        public ObservableCollection<string> YAxisLabels { get; set; }
        public decimal ChartMaxY { get; set; }
        public User CurrentUser { get => _currentUser; }
        public bool IsProfilePanelVisible { get => _isProfilePanelVisible; set { _isProfilePanelVisible = value; OnPropertyChanged(); } }
        public bool IsCarSelectionVisible { get => _isCarSelectionVisible; set { _isCarSelectionVisible = value; OnPropertyChanged(); } }
        public bool IsCarDetailsVisible { get => _isCarDetailsVisible; set { _isCarDetailsVisible = value; OnPropertyChanged(); } }
        public ObservableCollection<Car> Cars { get; set; }
        public Car SelectedCar { get => _selectedCar; set { _selectedCar = value; OnPropertyChanged(); (SelectCarCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        public Car CarForEditing 
        { 
            get => _carForEditing; 
            set 
            { 
                _carForEditing = value; 
                OnPropertyChanged(); 
                (SaveCarCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCarCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RemovePhotoUrlCommand as RelayCommand)?.RaiseCanExecuteChanged();
            } 
        }
        public ICommand DeleteCardCommand { get; }
        public ICommand ToggleProfilePanelCommand { get; }
        public ICommand UpdateProfilePhotoCommand { get; }
        public ICommand UpdateLicensePhotoCommand { get; }
        public ICommand ContactSupportCommand { get; }
        private readonly SupportRepository _supportRepository;


        public ObservableCollection<PaymentCard> SavedCards { get; set; }
        private PaymentCard _selectedSavedCard;

        public PaymentCard SelectedSavedCard
        {
            get => _selectedSavedCard;
            set
            {
                _selectedSavedCard = value;
                OnPropertyChanged();
                LoadCardFromSelection(value);

                // ==== ДОБАВЬ ЭТУ СТРОКУ ====
                (DeleteCardCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }


        public string AmountError { get => _amountError; set { _amountError = value; OnPropertyChanged(); } }
        public string CardNumberError { get => _cardNumberError; set { _cardNumberError = value; OnPropertyChanged(); } }
        public string CardExpiryError { get => _cardExpiryError; set { _cardExpiryError = value; OnPropertyChanged(); } }
        public string CvvError { get => _cvvError; set { _cvvError = value; OnPropertyChanged(); } }
        public ObservableCollection<Transaction> RecentEarnings { get; set; }
        public ObservableCollection<Transaction> RecentWithdrawals { get; set; }

        // Данные для графика (Заглушка)
        

        public decimal TotalBalance { get => _totalBalance; set { _totalBalance = value; OnPropertyChanged(); } }
        public string CardNumber
        {
            get => _cardNumber;
            set
            {
                // ... (твой код форматирования "1234 5678 ...")
                string digits = new string(value.Where(char.IsDigit).ToArray());
                if (digits.Length > 16) digits = digits.Substring(0, 16);
                string formatted = string.Join(" ", Enumerable.Range(0, (digits.Length + 3) / 4)
                    .Select(i => digits.Substring(i * 4, Math.Min(4, digits.Length - i * 4))));
                _cardNumber = formatted;
                // ... (конец форматирования)

                OnPropertyChanged();
                ValidateCardNumber(); // Валидируем только это поле
            }
        }

        public string CardExpiry
        {
            get => _cardExpiry;
            set
            {
                // ... (твой код форматирования "MM/YY")
                string digits = new string(value.Where(char.IsDigit).ToArray());
                if (digits.Length > 4) digits = digits.Substring(0, 4);
                string formatted = digits;
                if (digits.Length > 2)
                {
                    formatted = digits.Insert(2, "/");
                }
                _cardExpiry = formatted;
                // ... (конец форматирования)

                OnPropertyChanged();
                ValidateCardExpiry(); // Валидируем только это поле
            }
        }

        public string CardCVV
        {
            get => _cardCVV;
            set
            {
                // ... (твой код форматирования CVV)
                string digits = new string(value.Where(char.IsDigit).ToArray());
                if (digits.Length > 3) digits = digits.Substring(0, 3);
                _cardCVV = digits;
                // ... (конец форматирования)

                OnPropertyChanged();
                ValidateCvv(); // Валидируем только это поле
            }
        }

        public bool RememberCard { get => _rememberCard; set { _rememberCard = value; OnPropertyChanged(); } }
        public decimal WithdrawAmount
        {
            get => _withdrawAmount;
            set
            {
                _withdrawAmount = value;
                OnPropertyChanged();
                ValidateAmount(); // Валидируем только это поле
            }
        }
        public bool IsWithdrawPanelVisible { get => _isWithdrawPanelVisible; set { _isWithdrawPanelVisible = value; OnPropertyChanged(); } }


        private void ValidateAmount(bool forceUpdate = false)
        {
            if (WithdrawAmount <= 0)
                AmountError = "Сумма должна быть больше нуля.";
            else if (WithdrawAmount > TotalBalance)
                AmountError = "Недостаточно средств на балансе.";
            else
                AmountError = null;

            if (forceUpdate) (WithdrawCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateCardNumber(bool forceUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(CardNumber) || CardNumber.Length < 19) // 16 цифр + 3 пробела
                CardNumberError = "Введите полный номер карты (16 цифр).";
            else
                CardNumberError = null;

            if (forceUpdate) (WithdrawCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateCardExpiry(bool forceUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(CardExpiry) || CardExpiry.Length < 5) // MM/YY
                CardExpiryError = "Введите срок действия (ММ/ГГ).";
            else
                CardExpiryError = null;

            if (forceUpdate) (WithdrawCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateCvv(bool forceUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(CardCVV) || CardCVV.Length < 3)
                CvvError = "Введите CVV (3 цифры).";
            else
                CvvError = null;

            if (forceUpdate) (WithdrawCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }



        private bool CanWithdraw()
        {
            // Пере-валидируем все поля на случай, если TotalBalance изменился
            ValidateAmount();
            ValidateCardNumber();
            ValidateCardExpiry();
            ValidateCvv();

            return string.IsNullOrEmpty(AmountError) &&
                   string.IsNullOrEmpty(CardNumberError) &&
                   string.IsNullOrEmpty(CardExpiryError) &&
                   string.IsNullOrEmpty(CvvError);
        }

        public ICommand ToggleWithdrawPanelCommand { get; }
        public ICommand WithdrawCommand { get; }
        public ICommand GoToMapCommand { get; }
        public ICommand LoadSavedCardCommand { get; }
        public ICommand SelectCarCommand { get; }
        public ICommand AddCarCommand { get; }
        public ICommand ViewCarDetailsCommand { get; }
        public ICommand CloseCarSelectionCommand { get; }
        public ICommand SaveCarCommand { get; }
        public ICommand DeleteCarCommand { get; }
        public ICommand CloseCarDetailsCommand { get; }
        public ICommand SelectMainPhotoCommand { get; }
        public ICommand AddPhotoToGalleryCommand { get; }
        public ICommand RemovePhotoUrlCommand { get; }

        public DriverDashboardViewModel(User user)
        {
            _currentUser = user;
            _walletRepository = new WalletRepository();
            _userRepository = new UserRepository();
            _supportRepository = new SupportRepository();
            _carRepository = new CarRepository();

            RecentEarnings = new ObservableCollection<Transaction>();
            RecentWithdrawals = new ObservableCollection<Transaction>();
            DailyEarnings = new ObservableCollection<ChartDataPoint>();
            YAxisLabels = new ObservableCollection<string>();
            Cars = new ObservableCollection<Car>();

            // ==== ВОТ ЭТА СТРОКА, СКОРЕЕ ВСЕГО, ПРОПУЩЕНА ====
            // ==== Убедись, что она здесь есть ====
            SavedCards = new ObservableCollection<PaymentCard>();
            // ===============================================

            ToggleProfilePanelCommand = new RelayCommand(() => IsProfilePanelVisible = !IsProfilePanelVisible);
            UpdateProfilePhotoCommand = new RelayCommand(() => UpdatePhoto("profile"));
            UpdateLicensePhotoCommand = new RelayCommand(() => UpdatePhoto("license"));
            ContactSupportCommand = new RelayCommand(OpenSupportChat);
            ToggleWithdrawPanelCommand = new RelayCommand(() => IsWithdrawPanelVisible = !IsWithdrawPanelVisible);
            WithdrawCommand = new RelayCommand(Withdraw, CanWithdraw);
            GoToMapCommand = new RelayCommand(GoToMap);
            DeleteCardCommand = new RelayCommand(DeleteCard, CanDeleteCard);
            SelectCarCommand = new RelayCommand(SelectCar, () => SelectedCar != null);
            AddCarCommand = new RelayCommand(AddCar);
            ViewCarDetailsCommand = new RelayCommand(ViewCarDetails, () => SelectedCar != null);
            CloseCarSelectionCommand = new RelayCommand(() => IsCarSelectionVisible = false);
            SaveCarCommand = new RelayCommand(SaveCar, () => CanSaveCar());
            DeleteCarCommand = new RelayCommand(DeleteCarFromList, () => CanDeleteCarFromList());
            CloseCarDetailsCommand = new RelayCommand(() => { IsCarDetailsVisible = false; CarForEditing = null; });
            SelectMainPhotoCommand = new RelayCommand(SelectMainPhoto);
            AddPhotoToGalleryCommand = new RelayCommand(AddPhotoToGallery);
            RemovePhotoUrlCommand = new RelayCommand(RemovePhotoUrl, () => CarForEditing != null && CarForEditing.PhotoGallery.Count > 0);

            LoadDashboardData();
            LoadCars();
        }

        private void OpenSupportChat()
        {
            // Этот вызов теперь будет работать без ошибок
            var ticket = SupportService.Instance.GetOrCreateTicketForUser(CurrentUser);

            ticket.UserInfo = CurrentUser;

            var chatView = new SupportChatView(CurrentUser, ticket);
            chatView.Show();
        }

        private void UpdatePhoto(string photoType)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpeg;*.jpg" };
            if (openFileDialog.ShowDialog() == true)
            {
                string newPath = CopyImageToAppData(openFileDialog.FileName); // Этот метод нужно будет скопировать сюда
                if (newPath != null)
                {
                    if (photoType == "profile")
                    {
                        _currentUser.DriverPhotoUrl = newPath;
                    }
                    else // license
                    {
                        _currentUser.LicensePhotoPath = newPath;
                    }

                    _userRepository.UpdateUser(_currentUser);
                    OnPropertyChanged(nameof(CurrentUser)); // Обновляем UI
                    MessageBox.Show("Фото успешно обновлено!");
                }
            }
        }

            
        private bool CanDeleteCard()
        {
            // Мы можем удалить карту, только если она выбрана И это не "Новая карта"
            return SelectedSavedCard != null && !string.IsNullOrEmpty(SelectedSavedCard.CardNumber);
        }

        private void DeleteCard()
        {
            if (!CanDeleteCard()) return;

            var cardToDelete = SelectedSavedCard;

            // 1. Говорим репозиторию удалить ее (для БД)
            _walletRepository.DeleteCard(_currentUser.user_id, cardToDelete);

            // 2. Удаляем ее из нашего списка в UI
            SavedCards.Remove(cardToDelete);

            // 3. Сбрасываем выбор на "Новая карта"
            SelectedSavedCard = SavedCards.FirstOrDefault(c => string.IsNullOrEmpty(c.CardNumber));
        }

        private void LoadCardFromSelection(PaymentCard card)
        {
            if (card == null || card.MaskedName == "Новая карта")
            {
                // Выбрана "Новая карта"
                CardNumber = "";
                CardExpiry = "";
                CardCVV = "";
                RememberCard = true;
            }
            else
            {
                // Выбрана сохраненная карта
                // ИСПРАВЛЕНИЕ: Теперь CardNumber будет содержать полный номер из БД
                CardNumber = card.CardNumber;
                CardExpiry = card.CardExpiry;
                CardCVV = "";
                RememberCard = false;
            }
        }


        public void LoadDashboardData()
        {
            // Загрузка баланса, транзакций и карт (этот код у вас уже есть и работает)
            TotalBalance = _walletRepository.GetBalance(_currentUser.user_id);
            var allTransactions = _walletRepository.GetTransactions(_currentUser.user_id);

            SavedCards.Clear();
            SavedCards.Add(new PaymentCard { MaskedName = "Новая карта" });
            var cardsFromRepo = _walletRepository.GetSavedCards(_currentUser.user_id);
            foreach (var card in cardsFromRepo)
            {
                SavedCards.Add(card);
            }

            RecentEarnings.Clear();
            allTransactions.Where(t => t.Type == TransactionType.Earning).Take(5).ToList().ForEach(t => RecentEarnings.Add(t));

            RecentWithdrawals.Clear();
            allTransactions.Where(t => t.Type == TransactionType.Withdrawal).Take(3).ToList().ForEach(w => RecentWithdrawals.Add(w));

            // --- НАЧАЛО НОВОЙ ЛОГИКИ ДЛЯ ГРАФИКА ---
            DailyEarnings.Clear();
            YAxisLabels.Clear();

            // 1. Получаем реальные данные о заработке из репозитория
            var earningsData = _walletRepository.GetLastSevenDaysEarnings(_currentUser.user_id);

            // 2. Создаем полную сетку данных за последние 7 дней (даже если заработка не было)
            var processedEarnings = new List<ChartDataPoint>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.Now.AddDays(-i).Date;
                // Если для этого дня есть запись в БД, берем ее, иначе - 0
                decimal amount = earningsData.ContainsKey(day) ? earningsData[day] : 0;
                processedEarnings.Add(new ChartDataPoint
                {
                    Label = day.ToString("ddd\ndd.MM"), // Формат: "Пн\n28.10"
                    Value = amount
                });
            }

            // 3. Находим максимальное значение в данных для масштабирования графика
            decimal maxValue = processedEarnings.Any() ? processedEarnings.Max(d => d.Value) : 0;
            if (maxValue == 0) maxValue = 100; // Минимальная высота шкалы, если заработка не было

            // 4. Вычисляем "красивое" круглое число для верха шкалы (например, 1350 -> 1400)
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10((double)maxValue)));
            ChartMaxY = (decimal)(Math.Ceiling((double)maxValue / magnitude) * magnitude);
            if (ChartMaxY < 100) ChartMaxY = 100; // Минимальная шкала всегда 100

            // 5. Создаем метки для оси Y (верхняя и средняя)
            YAxisLabels.Add($"{ChartMaxY:N0}");    // Верхняя метка (например, "1 400")
            YAxisLabels.Add($"{ChartMaxY / 2:N0}"); // Средняя метка (например, "700")

            // 6. Масштабируем высоту каждого столбца относительно максимальной высоты (140px)
            const double maxBarHeight = 140.0;
            foreach (var earning in processedEarnings)
            {
                // Рассчитываем высоту столбца в пикселях
                earning.ScaledHeight = ChartMaxY > 0 ? (double)(earning.Value / ChartMaxY) * maxBarHeight : 0;
                DailyEarnings.Add(earning);
            }
            // --- КОНЕЦ НОВОЙ ЛОГИКИ ДЛЯ ГРАФИКА ---
        }





        private void Withdraw()
        {
            var card = new PaymentCard
            {
                CardNumber = this.CardNumber,
                CardExpiry = this.CardExpiry,
                CardCVV = this.CardCVV // В реальном приложении CVV не передается на сервер
            };

            if (_walletRepository.WithdrawFunds(_currentUser.user_id, WithdrawAmount, card, RememberCard))
            {
                MessageBox.Show($"Запрос на вывод {WithdrawAmount:C} успешно создан.\nСредства поступят в течение 3-5 рабочих дней.", "Успех");
                LoadDashboardData(); // Обновляем баланс и списки
                IsWithdrawPanelVisible = false; // Скрываем панель
                WithdrawAmount = 0;
                CardCVV = "";
            }
            else
            {
                MessageBox.Show("Ошибка вывода. Проверьте сумму и баланс.", "Ошибка");
            }
        }

        private void GoToMap()
        {
            // Показываем панель выбора авто вместо открытия отдельного окна
            LoadCars();
            IsCarSelectionVisible = true;
        }

        private void LoadCars()
        {
            Cars.Clear();
            var carsFromRepo = _carRepository.GetCarsByDriverId(_currentUser.user_id);
            foreach (var car in carsFromRepo)
            {
                Cars.Add(car);
            }
        }

        public void SelectCarInList(Car car)
        {
            foreach (var c in Cars)
            {
                c.IsSelected = false;
            }
            car.IsSelected = true;
            SelectedCar = car;
        }

        private void SelectCar()
        {
            if (SelectedCar == null) return;
            
            // Скрываем панель выбора авто, чтобы при возврате её не было
            IsCarSelectionVisible = false;

            // Открываем карту с выбранной машиной
            var driverVM = new DriverViewModel(_currentUser, SelectedCar);
            var driverView = new DriverView();
            driverView.DataContext = driverVM;
            driverView.Show();

            // Закрываем дашборд
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Hide();
                    break;
                }
            }
        }

        private void AddCar()
        {
            CarForEditing = new Car 
            { 
                DriverId = _currentUser.user_id,
                Color = "Не указан",
                Tariff = "Эконом"
            };
            IsCarDetailsVisible = true;
            IsCarSelectionVisible = false;
        }

        private void ViewCarDetails()
        {
            if (SelectedCar == null) return;
            // Создаём копию для редактирования, чтобы не менять оригинал до сохранения
            CarForEditing = new Car
            {
                CarId = SelectedCar.CarId,
                DriverId = SelectedCar.DriverId,
                ModelName = SelectedCar.ModelName,
                LicensePlate = SelectedCar.LicensePlate,
                MainImageUrl = SelectedCar.MainImageUrl,
                Color = SelectedCar.Color ?? "Не указан",
                Tariff = SelectedCar.Tariff ?? "Эконом",
                EngineInfo = SelectedCar.EngineInfo,
                InsuranceInfo = SelectedCar.InsuranceInfo,
                PhotoGallery = SelectedCar.PhotoGallery != null ? new List<string>(SelectedCar.PhotoGallery) : new List<string>()
            };
            IsCarDetailsVisible = true;
            IsCarSelectionVisible = false;
        }

        private bool CanSaveCar()
        {
            if (CarForEditing == null) return false;
            bool isValid = !string.IsNullOrWhiteSpace(CarForEditing.ModelName) &&
                          !string.IsNullOrWhiteSpace(CarForEditing.LicensePlate);
            return isValid;
        }

        private void SaveCar()
        {
            if (CarForEditing == null) return;

            CarForEditing.DriverId = _currentUser.user_id;
            
            if (CarForEditing.CarId == 0)
            {
                // Новое авто
                var addedCar = _carRepository.AddCar(CarForEditing);
                if (addedCar != null)
                {
                    MessageBox.Show("Автомобиль успешно добавлен!", "Успех");
                }
                else
                {
                    MessageBox.Show("Ошибка при добавлении автомобиля.", "Ошибка");
                    return;
                }
            }
            else
            {
                // Обновление существующего
                if (_carRepository.UpdateCar(CarForEditing))
                {
                    MessageBox.Show("Автомобиль успешно обновлён!", "Успех");
                }
                else
                {
                    MessageBox.Show("Ошибка при обновлении автомобиля.", "Ошибка");
                    return;
                }
            }

            LoadCars();
            IsCarDetailsVisible = false;
            CarForEditing = null;
            // Возвращаемся к панели выбора, если она была открыта
            if (Cars.Count > 0)
            {
                IsCarSelectionVisible = true;
            }
        }

        private bool CanDeleteCarFromList()
        {
            return CarForEditing != null && CarForEditing.CarId > 0;
        }

        private void DeleteCarFromList()
        {
            if (CarForEditing == null || CarForEditing.CarId == 0) return;

            if (MessageBox.Show("Вы уверены, что хотите удалить этот автомобиль?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (_carRepository.DeleteCar(CarForEditing))
                {
                    LoadCars();
                    IsCarDetailsVisible = false;
                    CarForEditing = null;
                    MessageBox.Show("Автомобиль успешно удалён!", "Успех");
                }
                else
                {
                    MessageBox.Show("Ошибка при удалении автомобиля.", "Ошибка");
                }
            }
        }

        private void SelectMainPhoto()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpeg;*.jpg" };
            if (openFileDialog.ShowDialog() == true)
            {
                string newPath = CopyImageToAppData(openFileDialog.FileName);
                if (newPath != null && CarForEditing != null)
                {
                    CarForEditing.MainImageUrl = newPath;
                    OnPropertyChanged(nameof(CarForEditing));
                }
            }
        }

        private void AddPhotoToGallery()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpeg;*.jpg" };
            if (openFileDialog.ShowDialog() == true)
            {
                string newPath = CopyImageToAppData(openFileDialog.FileName);
                if (newPath != null && CarForEditing != null)
                {
                    if (CarForEditing.PhotoGallery == null)
                        CarForEditing.PhotoGallery = new List<string>();
                    CarForEditing.PhotoGallery.Add(newPath);
                    OnPropertyChanged(nameof(CarForEditing));
                    (RemovePhotoUrlCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private void RemovePhotoUrl()
        {
            if (CarForEditing?.PhotoGallery != null && CarForEditing.PhotoGallery.Count > 0)
            {
                CarForEditing.PhotoGallery.RemoveAt(CarForEditing.PhotoGallery.Count - 1);
                OnPropertyChanged(nameof(CarForEditing));
                (RemovePhotoUrlCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string CopyImageToAppData(string sourceImagePath)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string destFolder = Path.Combine(baseDirectory, "UserData", "Images");
                Directory.CreateDirectory(destFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(sourceImagePath);
                string destinationPath = Path.Combine(destFolder, uniqueFileName);
                File.Copy(sourceImagePath, destinationPath);
                return Path.Combine("UserData", "Images", uniqueFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении фото: {ex.Message}");
                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class ChartDataPoint
        {
            public string Label { get; set; }
            public decimal Value { get; set; }
            public double ScaledHeight { get; set; }
        }

    }
}

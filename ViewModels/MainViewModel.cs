using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.Repositories;


namespace TaxiWPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _repository;
        private User _selectedDriver;

        public ObservableCollection<User> Drivers { get; set; }
        public User SelectedDriver
        {
            get => _selectedDriver;
            set
            {
                _selectedDriver = value;
                OnPropertyChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }


        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public MainViewModel()
        {
            // БЫЛО:
            // _repository = new DriverRepository();
            // СТАЛО:
            _repository = new UserRepository(); // Используем новый репозиторий

            Drivers = new ObservableCollection<User>(); // Используем User
            LoadDrivers();

            // Команды остаются, но CanExecute нужно будет проверить/адаптировать
            AddCommand = new RelayCommand(AddDriver);
            SaveCommand = new RelayCommand(SaveDriver, CanSaveOrUpdate); // Переименуем CanSave
            DeleteCommand = new RelayCommand(DeleteDriver, CanDelete);
        }

        private void LoadDrivers()
        {
            // --- НУЖНО ДОБАВИТЬ МЕТОД В UserRepository ---
            // Пока сделаем заглушку, потом добавим метод GetAllUsersByRole
            // var drivers = _repository.GetAllUsersByRole("Driver");
            var drivers = new List<User>(); // Временная заглушка

            Drivers.Clear();
            foreach (var driver in drivers)
            {
                Drivers.Add(driver);
            }
            // Если список пуст, можно добавить нового пустого для редактирования
            if (Drivers.Count == 0)
            {
                // AddDriver(); // Пока не вызываем, чтобы не было рекурсии
            }
        }

        private void AddDriver()
        {
            SelectedDriver = new User // Создаем User
            {
                role = "Driver", // Устанавливаем роль
                username = "", // Инициализируем обязательные поля
                email = "",
                password = "", // Пароль - ? Возможно, его не нужно здесь задавать
                driver_status = "Свободен", // Используем driver_status
                                            // Остальные поля можно оставить пустыми (NULL)
            };
        }

        private void SaveDriver()
        {
            if (SelectedDriver.user_id == 0) // Новый водитель (ID еще не присвоен)
            {
                // Здесь пароль нужно как-то задать, возможно, генерировать или просить ввести
                if (string.IsNullOrEmpty(SelectedDriver.password)) SelectedDriver.password = "temp123"; // Временный пароль!

                _repository.AddUser(SelectedDriver);
            }
            else // Существующий водитель
            {
                _repository.UpdateUser(SelectedDriver);
            }
            LoadDrivers(); // Перезагружаем список
        }

        private bool CanSaveOrUpdate() => SelectedDriver != null &&
                                  !string.IsNullOrWhiteSpace(SelectedDriver.username) &&
                                  !string.IsNullOrWhiteSpace(SelectedDriver.email) && // Добавили email
                                                                                      // !string.IsNullOrWhiteSpace(SelectedDriver.password) && // Пароль может быть пустым при обновлении
                                  !string.IsNullOrWhiteSpace(SelectedDriver.full_name) &&
                                  !string.IsNullOrWhiteSpace(SelectedDriver.phone);
        // Поля машины больше не проверяем здесь

        private void DeleteDriver()
        {
            if (SelectedDriver != null && SelectedDriver.user_id > 0)
            {
                // --- НУЖНО ДОБАВИТЬ МЕТОД В UserRepository ---
                // _repository.DeleteUser(SelectedDriver.user_id);

                LoadDrivers();
                SelectedDriver = null;
            }
        }

        private bool CanDelete() => SelectedDriver != null && SelectedDriver.user_id > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

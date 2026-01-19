using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.Repositories;
using System.ComponentModel;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.IO;


namespace TaxiWPF.ViewModels
{
    // --- ОБНОВЛЕНО: Добавлен INotifyPropertyChanged ---
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private string _username;
        private string _password;
        private string _confirmPassword;
        private string _email;
        private string _fullName;

        // --- НОВОЕ: Свойства для режима регистрации водителя ---
        private bool _isDriverRegistration = false;
        private string _licensePhotoPath;
        private string _profilePhotoPath;
        // --------------------------------------------------

        public string Username { get => _username; set { _username = value; OnPropertyChanged(nameof(Username)); (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(nameof(Password)); (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(nameof(Email)); (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(nameof(FullName)); (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        // --- НОВОЕ: Свойство для переключения видимости ---
        public bool IsDriverRegistration
        {
            get => _isDriverRegistration;
            set
            {
                _isDriverRegistration = value;
                OnPropertyChanged(nameof(IsDriverRegistration));
                // Обновляем состояние кнопки RegisterCommand
                (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // --- НОВОЕ: Свойства для путей к фото ---
        public string LicensePhotoPath
        {
            get => _licensePhotoPath;
            set { _licensePhotoPath = value; OnPropertyChanged(nameof(LicensePhotoPath)); (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string ProfilePhotoPath
        {
            get => _profilePhotoPath;
            set { _profilePhotoPath = value; OnPropertyChanged(nameof(ProfilePhotoPath)); (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        // --- НОВЫЕ КОМАНДЫ ---
        public ICommand ToggleRegistrationModeCommand { get; }
        public ICommand SelectLicensePhotoCommand { get; }
        public ICommand SelectProfilePhotoCommand { get; }
        public ICommand GoToLoginCommand { get; set; }
        // -------------------

        public ICommand RegisterCommand { get; }

        public RegistrationViewModel()
        {
            _userRepository = new UserRepository();

            // --- ОБНОВЛЕНО: Передаем CanRegister ---
            RegisterCommand = new RelayCommand(Register, CanRegister);

            // --- НОВОЕ: Инициализация команд ---
            ToggleRegistrationModeCommand = new RelayCommand(() => IsDriverRegistration = !IsDriverRegistration);
            SelectLicensePhotoCommand = new RelayCommand(SelectLicensePhoto);
            SelectProfilePhotoCommand = new RelayCommand(SelectProfilePhoto);
        }

        // --- ОБНОВЛЕНО: Логика проверки ---
        private bool CanRegister()
        {
            bool baseValid = !string.IsNullOrWhiteSpace(Username) &&
                             !string.IsNullOrWhiteSpace(Password) &&
                             !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                             Password == ConfirmPassword &&
                             !string.IsNullOrWhiteSpace(Email) && Email.Contains("@") &&
                             !string.IsNullOrWhiteSpace(FullName);

            if (IsDriverRegistration)
            {
                // Для водителя ОБА фото обязательны
                return baseValid &&
                       !string.IsNullOrWhiteSpace(LicensePhotoPath) &&
                       !string.IsNullOrWhiteSpace(ProfilePhotoPath);
            }

            // Для клиента все фото необязательны
            return baseValid;
        }

        // --- ДОБАВЛЕН НОВЫЙ ВСПОМОГАТЕЛЬНЫЙ МЕТОД ---
        private string CopyImageToAppData(string sourceImagePath)
        {
            try
            {
                // 1. Определяем папку назначения внутри папки с .exe файлом
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string destFolder = Path.Combine(baseDirectory, "UserData", "Images");

                // 2. Создаем папку, если ее не существует
                Directory.CreateDirectory(destFolder);

                // 3. Генерируем уникальное имя файла, чтобы избежать конфликтов
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(sourceImagePath);
                string destinationPath = Path.Combine(destFolder, uniqueFileName);

                // 4. Копируем файл
                File.Copy(sourceImagePath, destinationPath);

                // 5. Возвращаем ОТНОСИТЕЛЬНЫЙ путь для сохранения в БД
                return Path.Combine("UserData", "Images", uniqueFileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении фото: {ex.Message}");
                return null;
            }
        }


        private void Register()
        {
            var newUser = new User
            {
                username = this.Username,
                password = this.Password,
                email = this.Email,
                full_name = this.FullName,
                role = IsDriverRegistration ? "Driver" : "Client"
            };

            // --- НАЧАЛО ИСПРАВЛЕНИЯ ---
            // Присваиваем пути к фото, если это регистрация водителя
            if (IsDriverRegistration)
            {
                newUser.LicensePhotoPath = this.LicensePhotoPath;
                newUser.DriverPhotoUrl = this.ProfilePhotoPath; // Эта строка исправляет ошибку
            }

            newUser.DriverPhotoUrl = this.ProfilePhotoPath;
            if (IsDriverRegistration)
            {
                newUser.LicensePhotoPath = this.LicensePhotoPath;
            }

            // --- КОНЕЦ ИСПРАВЛЕНИЯ ---

            if (Password != ConfirmPassword)
            {
                MessageBox.Show("Пароли не совпадают. Пожалуйста, проверьте введённые данные.", "Ошибка регистрации");
                return;
            }

            if (_userRepository.AddUser(newUser))
            {
                MessageBox.Show("Регистрация прошла успешно! Теперь вы можете войти.", "Успех");

                // Возвращаемся к форме входа
                GoToLoginCommand?.Execute(null);
            }
            else
            {
                MessageBox.Show("Пользователь с таким именем или email уже существует.", "Ошибка регистрации");
            }
        }

        // --- НОВЫЙ МЕТОД: Выбор фото прав ---
        private void SelectLicensePhoto()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                // ИЗМЕНЕНО: Вызываем новый метод для копирования
                LicensePhotoPath = CopyImageToAppData(openFileDialog.FileName);
            }
        }

        // --- НОВЫЙ МЕТОД: Выбор фото профиля ---
        private void SelectProfilePhoto()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                // БЫЛО ПРОПУЩЕНО: Присваиваем результат копирования свойству
                ProfilePhotoPath = CopyImageToAppData(openFileDialog.FileName);
            }
        }


        // --- НОВОЕ: Реализация INotifyPropertyChanged ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

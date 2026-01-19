using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.Repositories;
using TaxiWPF.Views;

namespace TaxiWPF.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private string _username;
        private string _password;

        public string Username { get => _username; set { _username = value; OnPropertyChanged(nameof(Username)); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(nameof(Password)); } }

        public ICommand LoginCommand { get; }
        public ICommand GoToRegisterCommand { get; set; }
        public ICommand GoToRecoveryCommand { get; set; }

        public LoginViewModel()
        {
            _userRepository = new UserRepository();
            LoginCommand = new RelayCommand(Login, () => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password));
            // Команды навигации будут установлены из AuthViewModel
        }

        private async void Login()
        {
            var user = await _userRepository.GetUserByUsernameAsync(Username);
            if (user != null && user.password == Password)
            {
                if (user.role == "Driver")
                {
                    var dashboardVM = new DriverDashboardViewModel(user);
                    var dashboardView = new DriverDashboardView();
                    dashboardView.DataContext = dashboardVM;
                    dashboardView.Show();
                }
                // --- НАЧАЛО ИЗМЕНЕНИЙ ---
                else if (user.role == "Manager")
                {
                    // Создаем и открываем окно менеджера
                    var managerView = new ManagerView(user);
                    managerView.Show();
                }
                // --- КОНЕЦ ИЗМЕНЕНИЙ ---
                else // "Client"
                {
                    // Создаем и сразу показываем ClientView (карта начнет загружаться, анимация показывается сразу)
                    var clientVM = new ClientViewModel(user);
                    var clientView = new ClientView();
                    clientView.DataContext = clientVM;
                    clientView.Show();
                    
                    // Закрываем окно входа
                    Application.Current.Windows.OfType<LoginView>().FirstOrDefault()?.Close();
                }

                // Закрываем окно входа
                
                // foreach (Window window in Application.Current.Windows)
                //{
                //   if (window.DataContext == this)
                //  {
                //     window.Close();
                //    break;
                // }
                // }
                
            }
            else
            {
                MessageBox.Show("Неверное имя пользователя или пароль.");
            }
        }

        // Методы GoToRegister и GoToRecovery больше не нужны - навигация через AuthViewModel

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

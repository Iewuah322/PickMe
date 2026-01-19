using System;
using System.ComponentModel;
using System.Windows.Input;
using TaxiWPF.ViewModels;

namespace TaxiWPF.ViewModels
{
    public enum AuthViewType
    {
        Login,
        Registration,
        PasswordRecovery
    }

    public class AuthViewModel : INotifyPropertyChanged
    {
        private object _currentView;
        private AuthViewType _currentViewType;

        public LoginViewModel LoginViewModel { get; }
        public RegistrationViewModel RegistrationViewModel { get; }
        public PasswordRecoveryViewModel PasswordRecoveryViewModel { get; }

        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }

        public AuthViewType CurrentViewType
        {
            get => _currentViewType;
            set
            {
                _currentViewType = value;
                switch (value)
                {
                    case AuthViewType.Login:
                        CurrentView = LoginViewModel;
                        break;
                    case AuthViewType.Registration:
                        CurrentView = RegistrationViewModel;
                        break;
                    case AuthViewType.PasswordRecovery:
                        CurrentView = PasswordRecoveryViewModel;
                        break;
                }
                OnPropertyChanged(nameof(CurrentViewType));
            }
        }

        public ICommand GoToLoginCommand { get; }
        public ICommand GoToRegisterCommand { get; }
        public ICommand GoToRecoveryCommand { get; }

        public AuthViewModel()
        {
            LoginViewModel = new LoginViewModel();
            RegistrationViewModel = new RegistrationViewModel();
            PasswordRecoveryViewModel = new PasswordRecoveryViewModel();

            // Подписываемся на команды навигации из LoginViewModel
            LoginViewModel.GoToRegisterCommand = new RelayCommand(() => CurrentViewType = AuthViewType.Registration);
            LoginViewModel.GoToRecoveryCommand = new RelayCommand(() => CurrentViewType = AuthViewType.PasswordRecovery);

            // Команды для возврата к форме входа
            GoToLoginCommand = new RelayCommand(() => CurrentViewType = AuthViewType.Login);
            
            // Добавляем команды в RegistrationViewModel и PasswordRecoveryViewModel
            RegistrationViewModel.GoToLoginCommand = GoToLoginCommand;
            PasswordRecoveryViewModel.GoToLoginCommand = GoToLoginCommand;

            // Начинаем с формы входа
            CurrentViewType = AuthViewType.Login;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



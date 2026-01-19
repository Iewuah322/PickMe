using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using TaxiWPF.Repositories;
using TaxiWPF.Services;

namespace TaxiWPF.ViewModels
{
    public class PasswordRecoveryViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private readonly EmailService _emailService;

        private string _email;
        private string _token;
        private string _newPassword;

        // --- ИЗМЕНЕНО: Две переменные для управления видимостью ---
        private bool _isEmailEntryVisible = true;
        private bool _isTokenEntryVisible = false;

        public string Email { get => _email; set { _email = value; OnPropertyChanged(nameof(Email)); } }
        public string Token { get => _token; set { _token = value; OnPropertyChanged(nameof(Token)); } }
        public string NewPassword { get => _newPassword; set { _newPassword = value; OnPropertyChanged(nameof(NewPassword)); } }

        // --- ИЗМЕНЕНО: Свойства для привязки в XAML ---
        public bool IsEmailEntryVisible { get => _isEmailEntryVisible; set { _isEmailEntryVisible = value; OnPropertyChanged(nameof(IsEmailEntryVisible)); } }
        public bool IsTokenEntryVisible { get => _isTokenEntryVisible; set { _isTokenEntryVisible = value; OnPropertyChanged(nameof(IsTokenEntryVisible)); } }

        public ICommand RequestTokenCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand GoToLoginCommand { get; set; }

        public PasswordRecoveryViewModel()
        {
            _userRepository = new UserRepository();
            _emailService = new EmailService();

            RequestTokenCommand = new RelayCommand(RequestToken, () => !string.IsNullOrWhiteSpace(Email) && Email.Contains("@"));
            ResetPasswordCommand = new RelayCommand(ResetPassword, () => !string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(NewPassword));
        }

        private async void RequestToken()
        {
            string generatedToken = _userRepository.CreatePasswordResetToken(Email);
            if (generatedToken != null)
            {
                await _emailService.SendPasswordResetEmailAsync(Email, generatedToken);
                //MessageBox.Show("Если пользователь с таким email существует, на него будет отправлен токен для сброса пароля.", "Проверьте почту");

                // --- ИЗМЕНЕНО: Переключаем видимость блоков ---
                IsEmailEntryVisible = false;
                IsTokenEntryVisible = true;
            }
            else
            {
                // Для безопасности не сообщаем, что email не найден
                // MessageBox.Show("Если пользователь с таким email существует, на него будет отправлен токен для сброса пароля.", "Проверьте почту");
                IsEmailEntryVisible = false;
                IsTokenEntryVisible = true;
            }
        }

        private void ResetPassword()
        {
            if (_userRepository.ResetPasswordWithToken(Token, NewPassword))
            {
                MessageBox.Show("Пароль успешно сброшен! Теперь вы можете войти с новым паролем.", "Успех");
                // Возвращаемся к форме входа
                GoToLoginCommand?.Execute(null);
            }
            else
            {
                MessageBox.Show("Неверный или истекший код. Попробуйте запросить новый.", "Ошибка");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

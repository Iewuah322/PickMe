using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TaxiWPF.Models;
using TaxiWPF.Repositories;

namespace TaxiWPF.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly UserRepository _userRepository;
        private User _currentUser;
        private string _currentPassword;
        private string _newPassword;
        private string _confirmPassword;
        private string _avatarPath;
        public User CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set { _currentPassword = value; OnPropertyChanged(); (ChangePasswordCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(); (ChangePasswordCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); (ChangePasswordCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public string AvatarPath
        {
            get => _avatarPath;
            set { _avatarPath = value; OnPropertyChanged(); }
        }

        public ICommand ChangePasswordCommand { get; }
        public ICommand SelectAvatarCommand { get; }
        public ICommand CloseCommand { get; set; }

        public SettingsViewModel(User user)
        {
            _userRepository = new UserRepository();
            CurrentUser = user;
            AvatarPath = user.DriverPhotoUrl;

            ChangePasswordCommand = new RelayCommand(ChangePassword, CanChangePassword);
            SelectAvatarCommand = new RelayCommand(SelectAvatar);
        }

        private bool CanChangePassword()
        {
            return !string.IsNullOrWhiteSpace(CurrentPassword) &&
                   !string.IsNullOrWhiteSpace(NewPassword) &&
                   NewPassword == ConfirmPassword &&
                   NewPassword.Length >= 6;
        }

        private void ChangePassword()
        {
            if (_userRepository.ChangePassword(CurrentUser.user_id, CurrentPassword, NewPassword))
            {
                MessageBox.Show("Пароль успешно изменен!", "Успех");
                CurrentPassword = "";
                NewPassword = "";
                ConfirmPassword = "";
            }
            else
            {
                MessageBox.Show("Неверный текущий пароль!", "Ошибка");
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

        private void SelectAvatar()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string newPath = CopyImageToAppData(openFileDialog.FileName);
                if (newPath != null)
                {
                    AvatarPath = newPath;
                    CurrentUser.DriverPhotoUrl = newPath;
                    _userRepository.UpdateUser(CurrentUser);
                    MessageBox.Show("Аватар успешно обновлен!", "Успех");
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


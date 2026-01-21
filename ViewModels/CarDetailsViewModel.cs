using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using TaxiWPF.Models;
using TaxiWPF.Repositories;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.IO;

namespace TaxiWPF.ViewModels
{
    public class CarDetailsViewModel : INotifyPropertyChanged
    {
        private readonly CarRepository _carRepository;
        private readonly User _driver;
        private Car _car;
        private string _newPhotoUrl;
        private readonly RelayCommand _removePhotoUrlCommand;
        public string NewPhotoUrl { get => _newPhotoUrl; set { _newPhotoUrl = value; OnPropertyChanged(); } }

        private bool _isAddMode; // Режим "Добавление" (true) или "Просмотр" (false)
        private string _selectedPhotoUrl;

        // --- Свойства для привязки ---
        public bool IsAddMode { get => _isAddMode; set { _isAddMode = value; OnPropertyChanged(); } }
        public Car Car { get => _car; set { _car = value; OnPropertyChanged(); } }

        // Галерея
        public ObservableCollection<string> PhotoGallery { get; set; }

        // Текущее большое фото
        public string SelectedPhotoUrl
        {
            get => _selectedPhotoUrl;
            set
            {
                _selectedPhotoUrl = value;
                // --- ИЗМЕНЕНИЕ: Теперь при выборе фото в галерее, оно становится главным ---
                if (!string.IsNullOrEmpty(value))
                {
                    Car.MainImageUrl = value;
                }
                // --- КОНЕЦ ИЗМЕНЕНИЯ ---
                OnPropertyChanged();
                OnPropertyChanged(nameof(Car)); // Оповещаем UI, что свойство Car изменилось
                _removePhotoUrlCommand?.RaiseCanExecuteChanged();
            }
        }

        public ICommand CloseCommand { get; }
        public ICommand SaveCommand { get; }
        
        public ICommand RemovePhotoUrlCommand { get; }
        public ICommand DeleteCarCommand { get; }

        public ICommand SelectMainPhotoCommand { get; }
        public ICommand AddPhotoToGalleryCommand { get; }
        public ICommand SelectPhotoCommand { get; }


        // Событие для закрытия
        public event Action RequestClose;

        // Конструктор
        public CarDetailsViewModel(Car car, User driver)
        {
            _carRepository = new CarRepository();
            _driver = driver;

            // Если переданная машина "пустая" (CarId = 0), это режим добавления
            if (car.CarId == 0)
            {
                IsAddMode = true;
                Car = car; // Работаем с новым пустым объектом
                Car.DriverId = _driver.user_id; // Сразу привязываем к водителю
            }
            else
            {
                IsAddMode = false;
                Car = car; // Работаем с существующим объектом
            }

            // --- Команды ---
            CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
            SaveCommand = new RelayCommand(SaveCar, CanSaveCar);
            DeleteCarCommand = new RelayCommand(DeleteCar, () => !IsAddMode); // Удалять можно только существующую
            SelectMainPhotoCommand = new RelayCommand(SelectMainPhoto);
            AddPhotoToGalleryCommand = new RelayCommand(AddPhotoToGallery);
            SelectPhotoCommand = new RelayCommand<string>(SelectPhoto);

            // --- Заглушки для фото ---

            _removePhotoUrlCommand = new RelayCommand(RemovePhotoUrl, () => !string.IsNullOrEmpty(SelectedPhotoUrl));
            RemovePhotoUrlCommand = _removePhotoUrlCommand;

            // --- Инициализация галереи ---
            if (Car.PhotoGallery == null)
            {
                Car.PhotoGallery = new List<string>();
            }
            PhotoGallery = new ObservableCollection<string>();
            // Добавляем главное фото
            if (!string.IsNullOrEmpty(Car.MainImageUrl))
            {
                PhotoGallery.Add(Car.MainImageUrl);
            }
            // Добавляем остальные фото
            foreach (var photo in Car.PhotoGallery)
            {
                PhotoGallery.Add(photo);
            }

            // Выбираем первое фото для отображения
            SelectedPhotoUrl = PhotoGallery.FirstOrDefault();
        }

        private void SelectMainPhoto()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpeg;*.jpg" };
            if (openFileDialog.ShowDialog() == true)
            {
                string newPath = CopyImageToAppData(openFileDialog.FileName);
                if (newPath != null)
                {
                    Car.MainImageUrl = newPath;
                    OnPropertyChanged(nameof(Car)); // Уведомляем интерфейс об изменении
                }
            }
        }

        private void AddPhotoToGallery()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpeg;*.jpg" };
            if (openFileDialog.ShowDialog() == true)
            {
                string newPath = CopyImageToAppData(openFileDialog.FileName);
                if (newPath != null)
                {
                    // Добавляем и в UI, и в саму модель
                    PhotoGallery.Add(newPath);
                    if (Car.PhotoGallery == null)
                    {
                        Car.PhotoGallery = new List<string>();
                    }
                    Car.PhotoGallery.Add(newPath);
                    
                    // Сразу выбираем новое фото
                    SelectedPhotoUrl = newPath;
                    OnPropertyChanged(nameof(PhotoGallery));
                }
            }
        }

        private void SelectPhoto(string photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl))
            {
                return;
            }

            SelectedPhotoUrl = photoUrl;
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


        private bool CanSaveCar()
        {
            // Простая проверка (в заглушке достаточно)
            return !string.IsNullOrEmpty(Car.ModelName) &&
                   !string.IsNullOrEmpty(Car.LicensePlate) &&
                   !string.IsNullOrEmpty(Car.MainImageUrl);
        }

        private void SaveCar()
        {
            if (IsAddMode)
            {
                _carRepository.AddCar(Car);
                MessageBox.Show("Автомобиль успешно добавлен!", "Успех");
            }
            else
            {
                _carRepository.UpdateCar(Car);
                MessageBox.Show("Изменения сохранены!", "Успех");
            }
            RequestClose?.Invoke();
        }

        private void DeleteCar()
        {
            if (MessageBox.Show($"Вы уверены, что хотите удалить {Car.ModelName}?",
                                "Подтверждение",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _carRepository.DeleteCar(Car);
                MessageBox.Show("Автомобиль удален.");
                RequestClose?.Invoke();
            }
        }

        // --- ЗАГЛУШКИ ДЛЯ УПРАВЛЕНИЯ ФОТО ---
        private void AddPhotoUrl()
        {
            if (string.IsNullOrWhiteSpace(NewPhotoUrl))
            {
                MessageBox.Show("Сначала вставьте URL в поле.");
                return;
            }

            // Добавляем и в UI, и в саму модель
            PhotoGallery.Add(NewPhotoUrl);
            if (Car.PhotoGallery == null)
            {
                Car.PhotoGallery = new List<string>();
            }
            Car.PhotoGallery.Add(NewPhotoUrl);

            // Если это первое фото, делаем его главным
            if (string.IsNullOrEmpty(Car.MainImageUrl))
            {
                Car.MainImageUrl = NewPhotoUrl;
                OnPropertyChanged(nameof(Car)); // Обновляем привязку к главному фото
            }
            NewPhotoUrl = ""; // Очищаем TextBox
        }

        private void RemovePhotoUrl()
        {
            if (string.IsNullOrEmpty(SelectedPhotoUrl)) return;

            // Если удаляем главное фото, выбираем следующее
            if (SelectedPhotoUrl == Car.MainImageUrl)
            {
                Car.MainImageUrl = PhotoGallery.FirstOrDefault(url => url != SelectedPhotoUrl);
                OnPropertyChanged(nameof(Car)); // Обновляем привязку
            }

            // Удаляем из модели и из UI
            Car.PhotoGallery.Remove(SelectedPhotoUrl);
            PhotoGallery.Remove(SelectedPhotoUrl);

            SelectedPhotoUrl = PhotoGallery.FirstOrDefault(); // Выбираем первое
            _removePhotoUrlCommand?.RaiseCanExecuteChanged();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.Repositories;
using TaxiWPF.Views;

namespace TaxiWPF.ViewModels
{
    public class CarSelectionViewModel : INotifyPropertyChanged
    {
        private readonly User _driver;
        private readonly CarRepository _carRepository;
        private Car _selectedCar;

        public ObservableCollection<Car> Cars { get; set; }
        public Car SelectedCar
        {
            get => _selectedCar;
            set { _selectedCar = value; OnPropertyChanged(); (SelectCarCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public ICommand SelectCarCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddCarCommand { get; }
        public ICommand ViewDetailsCommand { get; }

        // Событие, которое попросит View закрыться
        public event Action<bool?> RequestClose;

        public CarSelectionViewModel(User driver)
        {
            _driver = driver;
            _carRepository = new CarRepository();
            Cars = new ObservableCollection<Car>();

            SelectCarCommand = new RelayCommand(SelectCar, () => SelectedCar != null);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false)); // false - отмена

            // --- Заглушки для будущих кнопок ---
            AddCarCommand = new RelayCommand(AddCar);
            ViewDetailsCommand = new RelayCommand(ViewDetails, () => SelectedCar != null);

            LoadCars();
        }

        private void LoadCars()
        {
            Cars.Clear();
            var carsFromRepo = _carRepository.GetCarsByDriverId(_driver.user_id);
            foreach (var car in carsFromRepo)
            {
                Cars.Add(car);
            }
        }

        public void SelectCar(Car car)
        {
            // Сбрасываем выбор у всех
            foreach (var c in Cars)
            {
                c.IsSelected = false;
            }
            // Выбираем указанную
            car.IsSelected = true;
            SelectedCar = car;
        }

        private void SelectCar()
        {
            // true - значит "ОК"
            RequestClose?.Invoke(true);
        }

        private void AddCar()
        {
            // 1. Создаем новое пустое авто
            var newCar = new Car();

            // 2. Создаем и открываем окно Деталей в режиме "Добавления"
            var detailsView = new CarDetailsView(_driver, newCar);

            // 3. Открываем как диалог
            detailsView.ShowDialog();

            // 4. После того, как окно закрылось, ОБНОВЛЯЕМ список машин
            // (новая машина должна появиться)
            LoadCars();
        }

        private void ViewDetails()
        {
            if (SelectedCar == null) return;

            // 1. Берем выбранную машину
            var carToView = SelectedCar;

            // 2. Создаем и открываем окно Деталей в режиме "Просмотра"
            var detailsView = new CarDetailsView(_driver, carToView);

            // 3. Открываем как диалог
            detailsView.ShowDialog();

            // 4. (На всякий случай) Обновляем список, если там что-то поменялось
            LoadCars();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

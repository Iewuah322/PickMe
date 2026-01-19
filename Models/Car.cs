using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class Car : INotifyPropertyChanged
    {
        private int _carId;
        private int _driverId;
        private string _modelName;
        private string _licensePlate;
        private string _mainImageUrl;
        private string _color = "Не указан";
        private string _tariff = "Эконом";
        private string _engineInfo;
        private string _insuranceInfo;
        private bool _isSelected;

        public int CarId 
        { 
            get => _carId; 
            set { _carId = value; OnPropertyChanged(); } 
        }

        public int DriverId 
        { 
            get => _driverId; 
            set { _driverId = value; OnPropertyChanged(); } 
        }

        public string ModelName 
        { 
            get => _modelName; 
            set { _modelName = value; OnPropertyChanged(); } 
        }

        public string LicensePlate 
        { 
            get => _licensePlate; 
            set { _licensePlate = value; OnPropertyChanged(); } 
        }

        public string MainImageUrl 
        { 
            get => _mainImageUrl; 
            set 
            { 
                _mainImageUrl = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PhotoPath)); // Обновляем алиас
            } 
        }
        
        // Дополнительные поля для UI
        public string Color 
        { 
            get => _color; 
            set { _color = value; OnPropertyChanged(); } 
        }

        public string Tariff 
        { 
            get => _tariff; 
            set { _tariff = value; OnPropertyChanged(); } 
        }

        public string PhotoPath => MainImageUrl; // Алиас для конвертера

        public bool IsSelected 
        { 
            get => _isSelected; 
            set { _isSelected = value; OnPropertyChanged(); }
        }

        // --- Поля для "Деталей" (на будущее) ---
        public string EngineInfo 
        { 
            get => _engineInfo; 
            set { _engineInfo = value; OnPropertyChanged(); } 
        }

        public string InsuranceInfo 
        { 
            get => _insuranceInfo; 
            set { _insuranceInfo = value; OnPropertyChanged(); } 
        }

        public List<string> PhotoGallery { get; set; } // Список URL

        public Car()
        {
            PhotoGallery = new List<string>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
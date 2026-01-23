using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.Views;

namespace TaxiWPF.ViewModels
{
    public class TripDetailsViewModel : INotifyPropertyChanged
    {
        private readonly User _currentUser;

        public Order Order { get; }
        public ICommand OpenChatCommand { get; }

        public string ClientName => Order.OrderClient?.full_name ?? "-";
        public string DriverName => Order.AssignedDriver?.full_name ?? "-";
        public string RouteFrom => Order.PointA;
        public string RouteTo => Order.PointB;
        public string Tariff => Order.Tariff;
        public decimal TotalPrice => Order.TotalPrice;
        public string PaymentMethod => string.IsNullOrWhiteSpace(Order.PaymentMethod) ? "Не указано" : Order.PaymentMethod;

        public bool CanChat => Order.AssignedDriver != null && Order.OrderClient != null;

        public TripDetailsViewModel(User currentUser, Order order)
        {
            _currentUser = currentUser;
            Order = order ?? throw new ArgumentNullException(nameof(order));

            OpenChatCommand = new RelayCommand(OpenChat, () => CanChat);
        }

        private void OpenChat()
        {
            if (!CanChat)
            {
                MessageBox.Show("Чат недоступен для этой поездки.");
                return;
            }

            var chatView = new TripChatView(_currentUser, Order);
            chatView.Show();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

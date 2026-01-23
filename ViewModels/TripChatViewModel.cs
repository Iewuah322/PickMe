using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.Services;

namespace TaxiWPF.ViewModels
{
    public class TripChatViewModel : INotifyPropertyChanged
    {
        private readonly User _currentUser;
        private readonly Order _order;
        private string _newMessageText;

        public ObservableCollection<TripChatMessage> Messages { get; }

        public string NewMessageText
        {
            get => _newMessageText;
            set
            {
                _newMessageText = value;
                OnPropertyChanged();
                (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string ChatTitle { get; }
        public string ChatSubTitle { get; }

        public ICommand SendMessageCommand { get; }

        public TripChatViewModel(User currentUser, Order order)
        {
            _currentUser = currentUser;
            _order = order;

            Messages = new ObservableCollection<TripChatMessage>();
            SendMessageCommand = new RelayCommand(SendMessage, () => !string.IsNullOrWhiteSpace(NewMessageText));


            global::TaxiWPF.Services.TripChatService.Instance.OnMessageReceived += HandleMessageReceived;


            var tripNumber = $"Поездка #{_order.order_id}";
            if (_currentUser.IsDriver)
            {
                var clientName = _order.OrderClient?.full_name ?? "Клиент";
                ChatTitle = $"Чат с клиентом: {clientName}";
                ChatSubTitle = tripNumber;
            }
            else
            {
                var driverName = _order.AssignedDriver?.full_name ?? "Водитель";
                ChatTitle = $"Чат с водителем: {driverName}";
                ChatSubTitle = tripNumber;


            }

            LoadMessages();
        }

        private void LoadMessages()
        {
            Messages.Clear();

            var messages = global::TaxiWPF.Services.TripChatService.Instance.GetMessages(_order.order_id);

            foreach (var message in messages)
            {
                message.IsFromCurrentUser = message.SenderId == _currentUser.user_id;
                Messages.Add(message);
            }
        }

        private void HandleMessageReceived(TripChatMessage message)
        {
            if (message.OrderId != _order.order_id)
            {
                return;
            }

            if (Messages.Any(m => m.Timestamp == message.Timestamp && m.SenderId == message.SenderId && m.MessageText == message.MessageText))
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                message.IsFromCurrentUser = message.SenderId == _currentUser.user_id;
                Messages.Add(message);
            });
        }

        private void SendMessage()
        {

            global::TaxiWPF.Services.TripChatService.Instance.SendMessage(_order.order_id, _currentUser.user_id, _currentUser.full_name, NewMessageText);

            NewMessageText = string.Empty;
        }

        public void Cleanup()
        {

            global::TaxiWPF.Services.TripChatService.Instance.OnMessageReceived -= HandleMessageReceived;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

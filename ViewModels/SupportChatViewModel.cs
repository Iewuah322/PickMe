using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaxiWPF.Models;
using TaxiWPF.Repositories;
using TaxiWPF.Services;
using System.Windows;


namespace TaxiWPF.ViewModels
{
    public class SupportChatViewModel : INotifyPropertyChanged
    {
        // ИЗМЕНЕНО: Репозиторий больше не нужен напрямую
        private readonly User _currentUser;
        private readonly SupportTicket _ticket;
        private string _newMessageText;

        public SupportTicket Ticket => _ticket;
        public ObservableCollection<SupportMessage> Messages { get; set; }
        public string NewMessageText
        {
            get => _newMessageText;
            set { _newMessageText = value; OnPropertyChanged(); (SendMessageCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        public bool IsManager => _currentUser.role == "Manager";

        public ICommand SendMessageCommand { get; }
        public ICommand ResolveTicketCommand { get; }
        public event Action RequestClose;

        public string ChatTitle { get; private set; }
        public string ChatSubTitle { get; private set; }

        public SupportChatViewModel(User currentUser, SupportTicket ticket)
        {
            _currentUser = currentUser;
            _ticket = ticket;

            Messages = new ObservableCollection<SupportMessage>();
            SendMessageCommand = new RelayCommand(SendMessage, () => !string.IsNullOrWhiteSpace(NewMessageText));
            ResolveTicketCommand = new RelayCommand(ResolveTicket);

            // --- НАЧАЛО ИЗМЕНЕНИЙ ---
            // 1. Подписываемся на события нашего нового сервиса
            SupportService.Instance.OnMessageReceived += HandleMessageReceived;
            // --- КОНЕЦ ИЗМЕНЕНИЙ ---

            if (IsManager)
            {
                ChatTitle = $"Чат с: {ticket.UserInfo.full_name} ({ticket.UserInfo.username})";
                ChatSubTitle = ticket.UserInfo.email;
            }
            else
            {
                ChatTitle = "Чат со службой поддержки";
                ChatSubTitle = "Мы скоро ответим на ваш вопрос";
            }

            LoadMessages();
        }

        // --- ДОБАВЛЕН НОВЫЙ МЕТОД-ОБРАБОТЧИК ---
        private void HandleMessageReceived(SupportMessage message)
        {
            // Если пришедшее сообщение относится к этому чату
            if (message.TicketID == _ticket.TicketID)
            {
                // Проверяем, нет ли уже такого сообщения в списке (чтобы избежать дублей)
                if (!Messages.Any(m => m.MessageID == message.MessageID))
                {
                    // Выполняем обновление в потоке интерфейса
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        message.IsFromCurrentUser = message.SenderID == _currentUser.user_id;
                        Messages.Add(message);
                    });
                }
            }
        }

        private void LoadMessages()
        {
            Messages.Clear();
            var messages = new SupportRepository().GetMessagesForTicket(_ticket.TicketID); // Загружаем историю
            foreach (var msg in messages)
            {
                msg.IsFromCurrentUser = msg.SenderID == _currentUser.user_id;
                Messages.Add(msg);
            }
        }

        private void SendMessage()
        {
            // --- ИЗМЕНЕНО: Теперь отправка идет через сервис ---
            SupportService.Instance.SendMessage(_ticket.TicketID, _currentUser.user_id, NewMessageText, _currentUser.full_name);
            NewMessageText = ""; // Очищаем поле ввода
        }

        private void ResolveTicket()
        {
            // --- ИЗМЕНЕНО: Используем сервис ---
            SupportService.Instance.ResolveTicket(_ticket.TicketID);
            RequestClose?.Invoke();
        }

        // --- ДОБАВЛЕН МЕТОД ДЛЯ ОТПИСКИ ОТ СОБЫТИЙ ---
        public void Cleanup()
        {
            SupportService.Instance.OnMessageReceived -= HandleMessageReceived;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
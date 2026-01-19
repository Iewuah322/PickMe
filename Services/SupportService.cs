using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiWPF.Models;
using TaxiWPF.Repositories;

namespace TaxiWPF.Services
{
    public class SupportService
    {
        // --- Реализация Singleton (чтобы сервис был один на все приложение) ---
        private static readonly Lazy<SupportService> _instance = new Lazy<SupportService>(() => new SupportService());
        public static SupportService Instance => _instance.Value;
        // ---------------------------------------------------------------------

        private readonly SupportRepository _repository;

        // Событие, которое будет "транслировать" новые сообщения всем подписчикам
        public event Action<SupportMessage> OnMessageReceived;

        private SupportService()
        {
            _repository = new SupportRepository();
        }

        // Главный метод для отправки сообщений
        public void SendMessage(int ticketId, int senderId, string messageText, string senderName)
        {
            // 1. Сохраняем сообщение в базу данных
            var savedMessage = _repository.AddMessage(ticketId, senderId, messageText);

            if (savedMessage != null)
            {
                // 2. Добавляем имя отправителя для удобства
                savedMessage.SenderName = senderName;

                // 3. Оповещаем всех подписчиков (все открытые окна чата) о новом сообщении
                OnMessageReceived?.Invoke(savedMessage);
            }
        }

        // Прокси-методы для удобства, чтобы вся логика была в одном месте
        public SupportTicket GetOrCreateTicketForUser(User user)
        {
            // Теперь этот метод просто вызывает одноименный метод в репозитории
            return _repository.GetOrCreateTicketForUser(user);
        }

        public void ResolveTicket(int ticketId)
        {
            _repository.UpdateTicketStatus(ticketId, "Resolved");
        }
    }
}
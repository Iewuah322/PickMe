using System;
using System.Collections.Generic;
using TaxiWPF.Models;

namespace TaxiWPF.Services
{
    public class TripChatService
    {
        private static readonly Lazy<TripChatService> _instance = new Lazy<TripChatService>(() => new TripChatService());
        public static TripChatService Instance => _instance.Value;

        private readonly Dictionary<int, List<TripChatMessage>> _messagesByOrderId = new Dictionary<int, List<TripChatMessage>>();

        public event Action<TripChatMessage> OnMessageReceived;

        private TripChatService()
        {
        }

        public IReadOnlyList<TripChatMessage> GetMessages(int orderId)
        {
            if (_messagesByOrderId.TryGetValue(orderId, out var messages))
            {
                return new List<TripChatMessage>(messages);
            }

            return new List<TripChatMessage>();
        }

        public void SendMessage(int orderId, int senderId, string senderName, string messageText)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return;
            }

            if (!_messagesByOrderId.TryGetValue(orderId, out var messages))
            {
                messages = new List<TripChatMessage>();
                _messagesByOrderId[orderId] = messages;
            }

            var newMessage = new TripChatMessage
            {
                OrderId = orderId,
                SenderId = senderId,
                SenderName = senderName,
                MessageText = messageText,
                Timestamp = DateTime.Now
            };

            messages.Add(newMessage);
            OnMessageReceived?.Invoke(newMessage);
        }
    }
}

using System;

namespace TaxiWPF.Models
{
    public class TripChatMessage
    {
        public int OrderId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string MessageText { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsFromCurrentUser { get; set; }
    }
}

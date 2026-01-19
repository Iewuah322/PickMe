using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class SupportMessage
    {
        public int MessageID { get; set; }
        public int TicketID { get; set; }
        public int SenderID { get; set; }
        public string MessageText { get; set; }
        public DateTime Timestamp { get; set; }

        // Дополнительные свойства для удобства отображения в чате
        public string SenderName { get; set; }
        public bool IsFromCurrentUser { get; set; }
    }
}

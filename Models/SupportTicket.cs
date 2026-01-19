using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class SupportTicket
    {
        public int TicketID { get; set; }
        public int UserID { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; } // "Open" или "Resolved"
        public DateTime LastUpdated { get; set; }

        // Дополнительное свойство для отображения информации о пользователе в списке
        public User UserInfo { get; set; }
        
        // Свойство для выделения в UI
        public bool IsSelected { get; set; } = false;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class Client
    {
        public int client_id { get; set; }
        public string full_name { get; set; }
        public string phone { get; set; }
        public decimal? rating { get; set; } // Рейтинг клиента
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class User
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string password { get; set; } // Помни про хеширование в будущем
        public string email { get; set; }
        public string role { get; set; } // "Client" или "Driver"

        // --- ДОБАВЛЕННЫЕ ОБЩИЕ ПОЛЯ ---
        public string full_name { get; set; }
        public string phone { get; set; }
        public decimal? rating { get; set; } // decimal? - значит, может быть NULL

        // --- ДОБАВЛЕННЫЕ ПОЛЯ ТОЛЬКО ДЛЯ ВОДИТЕЛЕЙ ---
        public string driver_status { get; set; } // Может быть NULL для клиента
        public string geo_position { get; set; }  // Может быть NULL для клиента
        public string DriverPhotoUrl { get; set; } // Может быть NULL для клиента
        public string LicensePhotoPath { get; set; } // Может быть NULL для клиента

        // --- ДОПОЛНИТЕЛЬНО (Может пригодиться) ---
        // Свойство только для чтения, чтобы легко проверять роль
        public bool IsDriver => role == "Driver";
        public bool IsClient => role == "Client";
    }
}

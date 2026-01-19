using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class Order
    {
        public int order_id { get; set; }
        public string PointA { get; set; } // Откуда
        public string PointB { get; set; } // Куда

        // --- ИЗМЕНЕНО ---
        public OrderState Status { get; set; } // <-- Заменили string на enum
        // --------------

        public string Tariff { get; set; }
        public decimal TotalPrice { get; set; }
        public Client OrderClient { get; set; } // Клиент, который сделал заказ
        public Driver AssignedDriver { get; set; } // Водитель, который принял заказ

        // --- НОВОЕ: Для оценок ---
        public bool ClientRated { get; set; } = false;
        public bool DriverRated { get; set; } = false;
        // ------------------------

        public string PaymentMethod { get; set; }
        
        // --- Для выделения выбранного заказа ---
        public bool IsSelected { get; set; } = false;
        // --------------------------------------

    }
}

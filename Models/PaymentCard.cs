using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class PaymentCard
    {
        // Для хранения полного номера (16 цифр)
        public string CardNumber { get; set; }

        // Для хранения срока действия
        public string CardExpiry { get; set; }

        // Для ввода CVV (в БД не хранится)
        public string CardCVV { get; set; }

        // --- ИЗМЕНЕНИЕ ---
        // Теперь это простое свойство для хранения маски (например, "Visa **** 1234"),
        // которое мы будем загружать из базы данных.
        public string MaskedName { get; set; }
    }
}
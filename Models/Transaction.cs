using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public enum TransactionType
    {
        Earning,    // Поступление
        Withdrawal  // Вывод
    }

    public class Transaction
    {
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; } // Положительное для Earning, отрицательное для Withdrawal
        public TransactionType Type { get; set; }
        public string PaymentMethod { get; set; }
    }
}
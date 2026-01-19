using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiWPF.Models;
using System.Data.SqlClient; // <-- Добавлено
using System.Configuration; // <-- Добавлено
using System.Windows;

namespace TaxiWPF.Repositories
{
    public class WalletRepository
    {
        private readonly string _connectionString; // <-- Добавлено

        // --- ЗАГЛУШКА ДАННЫХ УДАЛЕНА ---

        public WalletRepository() // <-- Добавлен конструктор
        {
            _connectionString = ConfigurationManager.ConnectionStrings["TaxiDB"].ConnectionString;
        }


        // Получаем текущий баланс водителя (сумма всех транзакций)
        public decimal GetBalance(int driverId)
        {
            decimal balance = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // ИЗМЕНЕН ЗАПРОС: Добавлено условие WHERE PaymentMethod = 'Карта'
                    var command = new SqlCommand(
                        "SELECT ISNULL(SUM(Amount), 0) FROM WalletTransactions WHERE driver_id = @driverId AND PaymentMethod = 'Карта'",
                        connection);
                    command.Parameters.AddWithValue("@driverId", driverId);
                    balance = (decimal)command.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении баланса: {ex.Message}");
                    MessageBox.Show($"Не удалось загрузить баланс: {ex.Message}", "Ошибка БД");
                }
            }
            return balance;
        }


        public void AddCard(int userId, PaymentCard card)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Генерация маски
                    string cleanCardNumber = new string(card.CardNumber.Where(char.IsDigit).ToArray());
                    string maskedNumber = "Карта **** " + (cleanCardNumber.Length > 4 ? cleanCardNumber.Substring(cleanCardNumber.Length - 4) : cleanCardNumber);

                    // Проверяем, есть ли уже такая маска
                    var checkCardCmd = new SqlCommand("SELECT COUNT(*) FROM PaymentCards WHERE driver_id = @userId AND MaskedNumber = @MaskedNumber", connection);
                    checkCardCmd.Parameters.AddWithValue("@userId", userId);
                    checkCardCmd.Parameters.AddWithValue("@MaskedNumber", maskedNumber);

                    if ((int)checkCardCmd.ExecuteScalar() == 0)
                    {
                        // ИЗМЕНЕН ЗАПРОС: Добавлено сохранение FullCardNumber
                        var addCardCmd = new SqlCommand(
                           @"INSERT INTO PaymentCards (driver_id, MaskedNumber, CardExpiry, FullCardNumber) 
                      VALUES (@userId, @MaskedNumber, @CardExpiry, @FullCardNumber)", connection);
                        addCardCmd.Parameters.AddWithValue("@userId", userId);
                        addCardCmd.Parameters.AddWithValue("@MaskedNumber", maskedNumber);
                        addCardCmd.Parameters.AddWithValue("@CardExpiry", card.CardExpiry);
                        addCardCmd.Parameters.AddWithValue("@FullCardNumber", cleanCardNumber); // Сохраняем чистый номер
                        addCardCmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении карты: {ex.Message}");
                    MessageBox.Show($"Не удалось сохранить карту: {ex.Message}", "Ошибка БД");
                }
            }
        }

        // Получаем список транзакций (например, последние 20)
        public List<Transaction> GetTransactions(int driverId, int limit = 20)
        {
            var transactions = new List<Transaction>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Выбираем последние 'limit' транзакций, сортируя по дате
                    var command = new SqlCommand(
                        @"SELECT TOP (@limit) * FROM WalletTransactions 
                          WHERE driver_id = @driverId 
                          ORDER BY TransactionDate DESC", connection);
                    command.Parameters.AddWithValue("@driverId", driverId);
                    command.Parameters.AddWithValue("@limit", limit);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(new Transaction
                            {
                                Date = (DateTime)reader["TransactionDate"],
                                Description = reader["Description"].ToString(),
                                Amount = (decimal)reader["Amount"],
                                Type = (TransactionType)(int)reader["TransactionType"] // Преобразуем int в enum
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении транзакций: {ex.Message}");
                    MessageBox.Show($"Не удалось загрузить историю операций: {ex.Message}", "Ошибка БД");
                }
            }
            return transactions;
        }

        // Получаем сохраненные карты водителя
        // ЗАМЕНИТЕ ЭТОТ МЕТОД ЦЕЛИКОМ
        public List<PaymentCard> GetSavedCards(int driverId)
        {
            var cards = new List<PaymentCard>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Запрашиваем все три нужных поля из БД
                    var command = new SqlCommand("SELECT MaskedNumber, CardExpiry, FullCardNumber FROM PaymentCards WHERE driver_id = @driverId", connection);
                    command.Parameters.AddWithValue("@driverId", driverId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cards.Add(new PaymentCard
                            {
                                // --- НАЧАЛО ИСПРАВЛЕНИЯ ---
                                // 1. Полный номер из БД записываем в свойство CardNumber
                                CardNumber = reader["FullCardNumber"]?.ToString(),

                                // 2. Маску из БД записываем в свойство MaskedName
                                MaskedName = reader["MaskedNumber"]?.ToString(),
                                // --- КОНЕЦ ИСПРАВЛЕНИЯ ---

                                CardExpiry = reader["CardExpiry"].ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении карт: {ex.Message}");
                    MessageBox.Show($"Не удалось загрузить сохраненные карты: {ex.Message}", "Ошибка БД");
                }
            }
            return cards;
        }

        // Вывод средств
        public bool WithdrawFunds(int driverId, decimal amount, PaymentCard card, bool saveCard)
        {
            if (amount <= 0) return false;

            using (var connection = new SqlConnection(_connectionString))
            {
                SqlTransaction transaction = null;
                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    // ... (код проверки баланса остается без изменений) ...
                    var balanceCmd = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM WalletTransactions WHERE driver_id = @driverId", connection, transaction);
                    balanceCmd.Parameters.AddWithValue("@driverId", driverId);
                    if (amount > (decimal)balanceCmd.ExecuteScalar())
                    {
                        MessageBox.Show("Недостаточно средств на балансе.", "Ошибка вывода");
                        transaction.Rollback();
                        return false;
                    }

                    // ... (код транзакции вывода остается без изменений) ...
                    var withdrawCmd = new SqlCommand(@"INSERT INTO WalletTransactions (driver_id, Description, Amount, TransactionType, PaymentMethod) VALUES (@driverId, @Description, @Amount, @TransactionType, @PaymentMethod)", connection, transaction);
                    withdrawCmd.Parameters.AddWithValue("@driverId", driverId);
                    withdrawCmd.Parameters.AddWithValue("@Description", "Вывод средств");
                    withdrawCmd.Parameters.AddWithValue("@Amount", -amount);
                    withdrawCmd.Parameters.AddWithValue("@TransactionType", (int)TransactionType.Withdrawal);
                    withdrawCmd.Parameters.AddWithValue("@PaymentMethod", "Карта");
                    withdrawCmd.ExecuteNonQuery();

                    // Сохраняем карту, если нужно
                    if (saveCard)
                    {
                        // --- НАЧАЛО ИСПРАВЛЕНИЯ: Используем существующий метод AddCard ---
                        // Это гарантирует, что и полный номер, и маска будут сохранены правильно
                        AddCard(driverId, card);
                        // --- КОНЕЦ ИСПРАВЛЕНИЯ ---
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при выводе средств: {ex.Message}");
                    MessageBox.Show($"Ошибка при выводе средств: {ex.Message}", "Ошибка БД");
                    try { transaction?.Rollback(); } catch { }
                    return false;
                }
            }
        }

        // Удаление сохраненной карты
        public bool DeleteCard(int driverId, PaymentCard cardToDelete)
        {
            // ИСПРАВЛЕНИЕ: Проверяем и маску, и что это не "Новая карта"
            if (cardToDelete == null || string.IsNullOrEmpty(cardToDelete.MaskedName) || cardToDelete.MaskedName == "Новая карта")
            {
                return false;
            }

            string maskedNumber = cardToDelete.MaskedName;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "DELETE FROM PaymentCards WHERE driver_id = @driverId AND MaskedNumber = @MaskedNumber",
                         connection);
                    command.Parameters.AddWithValue("@driverId", driverId);
                    command.Parameters.AddWithValue("@MaskedNumber", maskedNumber);

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при удалении карты: {ex.Message}");
                    MessageBox.Show($"Ошибка при удалении карты: {ex.Message}", "Ошибка БД");
                    return false;
                }
            }
        }

        // Добавление заработка (вызывается из OrderService или DriverViewModel)
        public void AddEarning(int driverId, decimal amount, int orderId, string paymentMethod)
        {
            if (amount <= 0) return;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(
                       @"INSERT INTO WalletTransactions (driver_id, Description, Amount, TransactionType, PaymentMethod) 
                  VALUES (@driverId, @Description, @Amount, @TransactionType, @PaymentMethod)", connection);
                    command.Parameters.AddWithValue("@driverId", driverId);
                    command.Parameters.AddWithValue("@Description", $"Поездка #{orderId}");
                    command.Parameters.AddWithValue("@Amount", amount);
                    command.Parameters.AddWithValue("@TransactionType", (int)TransactionType.Earning);
                    command.Parameters.AddWithValue("@PaymentMethod", paymentMethod); // Используем переданное значение
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА: Не удалось добавить заработок: {ex.Message}");
                }
            }
        }
        public Dictionary<DateTime, decimal> GetLastSevenDaysEarnings(int driverId)
        {
            var earnings = new Dictionary<DateTime, decimal>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(
        @"SELECT CAST(TransactionDate AS DATE) AS EarningDate, SUM(Amount) AS TotalAmount
          FROM WalletTransactions
          WHERE driver_id = @driverId AND TransactionType = 0 AND TransactionDate >= @sevenDaysAgo
          GROUP BY CAST(TransactionDate AS DATE)", connection);

                    command.Parameters.AddWithValue("@driverId", driverId);
                    command.Parameters.AddWithValue("@sevenDaysAgo", DateTime.Now.AddDays(-6).Date); // -6 to include today

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            earnings[(DateTime)reader["EarningDate"]] = (decimal)reader["TotalAmount"];
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении заработка для графика: {ex.Message}");
                }
            }
            return earnings;
        }
    }
}
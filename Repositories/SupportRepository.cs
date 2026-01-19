using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TaxiWPF.Models;

namespace TaxiWPF.Repositories
{
    public class SupportRepository
    {
        private readonly string _connectionString;

        public SupportRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["TaxiDB"].ConnectionString;
        }

        // Получить все активные (нерешенные) чаты для менеджера
        public List<SupportTicket> GetOpenTickets()
        {
            var tickets = new List<SupportTicket>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(
        @"SELECT t.*, u.username, u.full_name, u.email 
          FROM SupportTickets t 
          JOIN Users u ON t.UserID = u.user_id 
          WHERE t.Status = 'Open' 
          ORDER BY t.LastUpdated ASC", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tickets.Add(new SupportTicket
                            {
                                TicketID = (int)reader["TicketID"],
                                UserID = (int)reader["UserID"],
                                Subject = reader["Subject"].ToString(),
                                Status = reader["Status"].ToString(),
                                LastUpdated = (DateTime)reader["LastUpdated"],
                                UserInfo = new User
                                {
                                    user_id = (int)reader["UserID"],
                                    username = reader["username"].ToString(),
                                    full_name = reader["full_name"].ToString(),
                                    email = reader["email"].ToString()
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки тикетов: {ex.Message}");
                }
            }
            return tickets;
        }

        // --- НАЧАЛО ИЗМЕНЕНИЙ: Реализация новых методов ---

        // Получить все сообщения для конкретного чата
        public List<SupportMessage> GetMessagesForTicket(int ticketId)
        {
            var messages = new List<SupportMessage>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(
                        @"SELECT sm.*, u.full_name 
                          FROM SupportMessages sm
                          JOIN Users u ON sm.SenderID = u.user_id
                          WHERE sm.TicketID = @TicketID
                          ORDER BY sm.Timestamp ASC", connection);
                    command.Parameters.AddWithValue("@TicketID", ticketId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new SupportMessage
                            {
                                MessageID = (int)reader["MessageID"],
                                TicketID = (int)reader["TicketID"],
                                SenderID = (int)reader["SenderID"],
                                MessageText = reader["MessageText"].ToString(),
                                Timestamp = (DateTime)reader["Timestamp"],
                                SenderName = reader["full_name"].ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки сообщений: {ex.Message}");
                }
            }
            return messages;
        }

        public SupportTicket GetOrCreateTicketForUser(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // 1. Ищем ЛЮБОЙ существующий тикет для этого пользователя
                var findCmd = new SqlCommand("SELECT TOP 1 * FROM SupportTickets WHERE UserID = @UserID ORDER BY LastUpdated DESC", connection);
                findCmd.Parameters.AddWithValue("@UserID", user.user_id);

                using (var reader = findCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // Если тикет найден, просто возвращаем его
                        return new SupportTicket
                        {
                            TicketID = (int)reader["TicketID"],
                            UserID = (int)reader["UserID"],
                            Subject = reader["Subject"].ToString(),
                        };
                    }
                }

                // 2. Если тикет не найден, создаем новый с темой по умолчанию
                var createCmd = new SqlCommand(
                    @"INSERT INTO SupportTickets (UserID, Subject, Status, LastUpdated) 
              OUTPUT INSERTED.TicketID
              VALUES (@UserID, @Subject, 'Open', @Timestamp)", connection);

                string subject = $"Обращение от {user.full_name}";
                createCmd.Parameters.AddWithValue("@UserID", user.user_id);
                createCmd.Parameters.AddWithValue("@Subject", subject);
                createCmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);

                int newTicketId = (int)createCmd.ExecuteScalar();

                return new SupportTicket { TicketID = newTicketId, UserID = user.user_id, Subject = subject };
            }
        }

        // Отправить новое сообщение и обновить тикет
        public SupportMessage AddMessage(int ticketId, int senderId, string messageText)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                try
                {
                    // 1. Добавляем новое сообщение
                    var command = new SqlCommand(
                        @"INSERT INTO SupportMessages (TicketID, SenderID, MessageText, Timestamp)
                          VALUES (@TicketID, @SenderID, @MessageText, @Timestamp);
                          SELECT SCOPE_IDENTITY();", connection, transaction);

                    var timestamp = DateTime.Now;
                    command.Parameters.AddWithValue("@TicketID", ticketId);
                    command.Parameters.AddWithValue("@SenderID", senderId);
                    command.Parameters.AddWithValue("@MessageText", messageText);
                    command.Parameters.AddWithValue("@Timestamp", timestamp);

                    int newMessageId = Convert.ToInt32(command.ExecuteScalar());

                    // 2. Обновляем время и статус тикета
                    var updateTicketCmd = new SqlCommand(
                        @"UPDATE SupportTickets 
                          SET LastUpdated = @Timestamp, Status = 'Open'
                          WHERE TicketID = @TicketID", connection, transaction);
                    updateTicketCmd.Parameters.AddWithValue("@Timestamp", timestamp);
                    updateTicketCmd.Parameters.AddWithValue("@TicketID", ticketId);
                    updateTicketCmd.ExecuteNonQuery();

                    transaction.Commit();

                    // Возвращаем созданное сообщение для немедленного отображения в чате
                    return new SupportMessage { MessageID = newMessageId, TicketID = ticketId, SenderID = senderId, MessageText = messageText, Timestamp = timestamp };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Ошибка отправки сообщения: {ex.Message}");
                    return null;
                }
            }
        }

        // Изменить статус тикета (например, на "Resolved")
        public void UpdateTicketStatus(int ticketId, string status)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(
                        "UPDATE SupportTickets SET Status = @Status WHERE TicketID = @TicketID", connection);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@TicketID", ticketId);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления статуса тикета: {ex.Message}");
                }
            }
        }
        // --- КОНЕЦ ИЗМЕНЕНИЙ ---
    }
}
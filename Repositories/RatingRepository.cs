using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using TaxiWPF.Models;

namespace TaxiWPF.Repositories
{
    public class RatingRepository
    {
        private readonly string _connectionString;

        public RatingRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["TaxiDB"].ConnectionString;
        }

        public async Task<bool> AddRatingAsync(Order order, int fromUserId, int toUserId, int ratingValue, bool wasPolite, bool wasClean, bool goodDriving)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlTransaction transaction = null;
                try
                {
                    await connection.OpenAsync();
                    transaction = connection.BeginTransaction();

                    // 1. Добавляем саму оценку в таблицу Ratings
                    var command = new SqlCommand(
                        @"INSERT INTO Ratings (order_id, rating_from_user_id, rating_to_user_id, rating_value, WasPolite, WasClean, GoodDriving)
                          VALUES (@order_id, @from_user, @to_user, @rating, @polite, @clean, @driving)",
                        connection, transaction);

                    command.Parameters.AddWithValue("@order_id", order.order_id);
                    command.Parameters.AddWithValue("@from_user", fromUserId);
                    command.Parameters.AddWithValue("@to_user", toUserId);
                    command.Parameters.AddWithValue("@rating", ratingValue);
                    command.Parameters.AddWithValue("@polite", wasPolite);
                    command.Parameters.AddWithValue("@clean", wasClean);
                    command.Parameters.AddWithValue("@driving", goodDriving);
                    await command.ExecuteNonQueryAsync();

                    // 2. Пересчитываем и обновляем средний рейтинг пользователя
                    var updateRatingCmd = new SqlCommand(
                        @"UPDATE Users 
                          SET rating = (SELECT AVG(CAST(rating_value AS DECIMAL(3,2))) FROM Ratings WHERE rating_to_user_id = @to_user)
                          WHERE user_id = @to_user",
                        connection, transaction);
                    updateRatingCmd.Parameters.AddWithValue("@to_user", toUserId);
                    await updateRatingCmd.ExecuteNonQueryAsync();

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении рейтинга: {ex.Message}");
                    transaction?.Rollback();
                    return false;
                }
            }
        }

        public bool AddRating(Order order, int fromUserId, int toUserId, int ratingValue, bool wasPolite, bool wasClean, bool goodDriving)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlTransaction transaction = null;
                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    // 1. Добавляем саму оценку в таблицу Ratings
                    var command = new SqlCommand(
                        @"INSERT INTO Ratings (order_id, rating_from_user_id, rating_to_user_id, rating_value, WasPolite, WasClean, GoodDriving)
                          VALUES (@order_id, @from_user, @to_user, @rating, @polite, @clean, @driving)",
                        connection, transaction);

                    command.Parameters.AddWithValue("@order_id", order.order_id);
                    command.Parameters.AddWithValue("@from_user", fromUserId);
                    command.Parameters.AddWithValue("@to_user", toUserId);
                    command.Parameters.AddWithValue("@rating", ratingValue);
                    command.Parameters.AddWithValue("@polite", wasPolite);
                    command.Parameters.AddWithValue("@clean", wasClean);
                    command.Parameters.AddWithValue("@driving", goodDriving);
                    command.ExecuteNonQuery();

                    // 2. Пересчитываем и обновляем средний рейтинг пользователя
                    var updateRatingCmd = new SqlCommand(
                        @"UPDATE Users 
                          SET rating = (SELECT AVG(CAST(rating_value AS DECIMAL(3,2))) FROM Ratings WHERE rating_to_user_id = @to_user)
                          WHERE user_id = @to_user",
                        connection, transaction);
                    updateRatingCmd.Parameters.AddWithValue("@to_user", toUserId);
                    updateRatingCmd.ExecuteNonQuery();

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении рейтинга: {ex.Message}");
                    transaction?.Rollback();
                    return false;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using TaxiWPF.Models;
using System.Windows;

namespace TaxiWPF.Repositories
{
    public class UserRepository
    {
        private readonly string _connectionString;

        // --- ЗАГЛУШКИ УДАЛЕНЫ ---

        public UserRepository()
        {
            // --- РАСКОММЕНТИРОВАНО И ОСТАВЛЕНО КАК ЕСТЬ ---
            _connectionString = ConfigurationManager.ConnectionStrings["TaxiDB"].ConnectionString;
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            User user = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    // Выбираем все поля из таблицы Users
                    var command = new SqlCommand("SELECT * FROM Users WHERE username = @username", connection);
                    command.Parameters.AddWithValue("@username", username);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user = new User
                            {
                                user_id = (int)reader["user_id"],
                                username = reader["username"].ToString(),
                                password = reader["password"].ToString(), // В реальном приложении здесь была бы проверка хеша
                                email = reader["email"].ToString(),
                                role = reader["role"].ToString(),
                                full_name = reader["full_name"] as string,
                                phone = reader["phone"] as string,
                                rating = reader["rating"] as decimal?,
                                // --- ДОБАВЛЕНЫ ПОЛЯ ВОДИТЕЛЯ ---
                                driver_status = reader["driver_status"] as string,
                                geo_position = reader["geo_position"] as string,
                                DriverPhotoUrl = reader["DriverPhotoUrl"] as string,
                                LicensePhotoPath = reader["LicensePhotoPath"] as string
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении пользователя: {ex.Message}");
                    MessageBox.Show($"Ошибка при подключении к БД: {ex.Message}", "Ошибка");
                }
            }
            return user;
        }

        public User GetUserByUsername(string username)
        {
            User user = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Выбираем все поля из таблицы Users
                    var command = new SqlCommand("SELECT * FROM Users WHERE username = @username", connection);
                    command.Parameters.AddWithValue("@username", username);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User
                            {
                                user_id = (int)reader["user_id"],
                                username = reader["username"].ToString(),
                                password = reader["password"].ToString(), // В реальном приложении здесь была бы проверка хеша
                                email = reader["email"].ToString(),
                                role = reader["role"].ToString(),
                                full_name = reader["full_name"] as string,
                                phone = reader["phone"] as string,
                                rating = reader["rating"] as decimal?,
                                // --- ДОБАВЛЕНЫ ПОЛЯ ВОДИТЕЛЯ ---
                                driver_status = reader["driver_status"] as string,
                                geo_position = reader["geo_position"] as string,
                                DriverPhotoUrl = reader["DriverPhotoUrl"] as string,
                                LicensePhotoPath = reader["LicensePhotoPath"] as string
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении пользователя: {ex.Message}");
                    MessageBox.Show($"Ошибка при подключении к БД: {ex.Message}", "Ошибка");
                }
            }
            return user;
        }

        public bool AddUser(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Используем новую таблицу Users
                    var command = new SqlCommand(
                        @"INSERT INTO Users (username, password, email, role, full_name, phone, DriverPhotoUrl, LicensePhotoPath) 
                          VALUES (@username, @password, @email, @role, @full_name, @phone, @DriverPhotoUrl, @LicensePhotoPath)", connection);

                    command.Parameters.AddWithValue("@username", user.username);
                    command.Parameters.AddWithValue("@password", user.password); // !!! В РЕАЛЬНОМ ПРИЛОЖЕНИИ ХЕШИРОВАТЬ !!!
                    command.Parameters.AddWithValue("@email", user.email);
                    command.Parameters.AddWithValue("@role", user.role);
                    // Добавляем поля, которые могут быть NULL
                    command.Parameters.AddWithValue("@full_name", (object)user.full_name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@phone", (object)user.phone ?? DBNull.Value);
                    // Добавляем фото (для водителей)
                    command.Parameters.AddWithValue("@DriverPhotoUrl", (object)user.DriverPhotoUrl ?? DBNull.Value);
                    command.Parameters.AddWithValue("@LicensePhotoPath", (object)user.LicensePhotoPath ?? DBNull.Value);


                    command.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException ex)
                {
                    // Обработка ошибок SQL (например, дубликат username или email)
                    System.Diagnostics.Debug.WriteLine($"Ошибка SQL при добавлении пользователя: {ex.Message}");
                    MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка БД");
                    return false;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Общая ошибка при добавлении пользователя: {ex.Message}");
                    MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка");
                    return false;
                }
            }
        }

        public string CreatePasswordResetToken(string email)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var userCmd = new SqlCommand("SELECT user_id FROM Users WHERE email = @email", connection);
                    userCmd.Parameters.AddWithValue("@email", email);
                    var userIdObj = userCmd.ExecuteScalar();
                    if (userIdObj == null) return null;

                    int userId = (int)userIdObj;

                    // --- НАЧАЛО ИЗМЕНЕНИЯ ---
                    // Было: string token = Guid.NewGuid().ToString();
                    // Стало: Генерируем случайное число от 100000 до 999999
                    string token = new Random().Next(100000, 999999).ToString();
                    // --- КОНЕЦ ИЗМЕНЕНИЯ ---

                    var expiration = DateTime.UtcNow.AddHours(1);

                    var tokenCmd = new SqlCommand("INSERT INTO PasswordResetTokens (user_id, token, expiration_date) VALUES (@user_id, @token, @expiration)", connection);
                    tokenCmd.Parameters.AddWithValue("@user_id", userId);
                    tokenCmd.Parameters.AddWithValue("@token", token);
                    tokenCmd.Parameters.AddWithValue("@expiration", expiration);
                    tokenCmd.ExecuteNonQuery();

                    return token;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при создании токена: {ex.Message}");
                    MessageBox.Show($"Ошибка при запросе сброса пароля: {ex.Message}", "Ошибка БД");
                    return null;
                }
            }
        }

        public bool ResetPasswordWithToken(string token, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlTransaction transaction = null; // Используем транзакцию для атомарности
                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    // Ищем user_id по валидному токену
                    var tokenCmd = new SqlCommand("SELECT user_id FROM PasswordResetTokens WHERE token = @token AND expiration_date > @now", connection, transaction);
                    tokenCmd.Parameters.AddWithValue("@token", token);
                    tokenCmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
                    var userIdObj = tokenCmd.ExecuteScalar();

                    if (userIdObj != null)
                    {
                        int userId = (int)userIdObj;

                        // Обновляем пароль в таблице Users
                        var updateCmd = new SqlCommand("UPDATE Users SET password = @password WHERE user_id = @user_id", connection, transaction);
                        updateCmd.Parameters.AddWithValue("@password", newPassword); // !!! В РЕАЛЬНОМ ПРИЛОЖЕНИИ ХЕШИРОВАТЬ !!!
                        updateCmd.Parameters.AddWithValue("@user_id", userId);
                        updateCmd.ExecuteNonQuery();

                        // Удаляем использованный токен из PasswordResetTokens
                        var deleteCmd = new SqlCommand("DELETE FROM PasswordResetTokens WHERE token = @token", connection, transaction);
                        deleteCmd.Parameters.AddWithValue("@token", token);
                        deleteCmd.ExecuteNonQuery();

                        transaction.Commit(); // Фиксируем изменения
                        return true;
                    }
                    else
                    {
                        // Токен не найден или истек
                        transaction.Rollback(); // Откатываем (хотя ничего не меняли)
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при сбросе пароля: {ex.Message}");
                    MessageBox.Show($"Ошибка при сбросе пароля: {ex.Message}", "Ошибка БД");
                    try { transaction?.Rollback(); } catch { /* Игнорируем ошибки отката */ }
                    return false;
                }
            }
        }

        // --- ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ (Могут понадобиться) ---

        // Смена пароля пользователя
        public bool ChangePassword(int userId, string currentPassword, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Проверяем текущий пароль
                    var checkCmd = new SqlCommand("SELECT password FROM Users WHERE user_id = @user_id", connection);
                    checkCmd.Parameters.AddWithValue("@user_id", userId);
                    var currentPasswordFromDb = checkCmd.ExecuteScalar()?.ToString();

                    if (currentPasswordFromDb != currentPassword)
                    {
                        return false; // Текущий пароль неверен
                    }

                    // Обновляем пароль
                    var updateCmd = new SqlCommand("UPDATE Users SET password = @password WHERE user_id = @user_id", connection);
                    updateCmd.Parameters.AddWithValue("@password", newPassword); // !!! В РЕАЛЬНОМ ПРИЛОЖЕНИИ ХЕШИРОВАТЬ !!!
                    updateCmd.Parameters.AddWithValue("@user_id", userId);
                    updateCmd.ExecuteNonQuery();
                    return true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при смене пароля: {ex.Message}");
                    MessageBox.Show($"Ошибка при смене пароля: {ex.Message}", "Ошибка БД");
                    return false;
                }
            }
        }

        // Обновление данных пользователя (например, телефона или рейтинга)
        public bool UpdateUser(User user)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    var command = new SqlCommand(
                        @"UPDATE Users SET 
                            email = @email, 
                            full_name = @full_name, 
                            phone = @phone, 
                            rating = @rating, 
                            driver_status = @driver_status, 
                            geo_position = @geo_position,
                            DriverPhotoUrl = @DriverPhotoUrl,
                            LicensePhotoPath = @LicensePhotoPath
                          WHERE user_id = @user_id", connection);

                    command.Parameters.AddWithValue("@user_id", user.user_id);
                    command.Parameters.AddWithValue("@email", user.email);
                    command.Parameters.AddWithValue("@full_name", (object)user.full_name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@phone", (object)user.phone ?? DBNull.Value);
                    command.Parameters.AddWithValue("@rating", (object)user.rating ?? DBNull.Value);
                    // Водительские поля
                    command.Parameters.AddWithValue("@driver_status", (object)user.driver_status ?? DBNull.Value);
                    command.Parameters.AddWithValue("@geo_position", (object)user.geo_position ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DriverPhotoUrl", (object)user.DriverPhotoUrl ?? DBNull.Value);
                    command.Parameters.AddWithValue("@LicensePhotoPath", (object)user.LicensePhotoPath ?? DBNull.Value);
                    // Пароль и username обычно не меняют в общем методе Update

                    int rowsAffected = command.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении пользователя: {ex.Message}");
                    MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка БД");
                    return false;
                }
            }
        }
    }
}
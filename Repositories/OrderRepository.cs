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
    // Репозиторий для работы с таблицей Orders
    public class OrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["TaxiDB"].ConnectionString;
        }

        // Создание нового заказа

        public async Task<List<Order>> GetPastOrdersByClientIdAsync(int clientId)
        {
            var orders = new List<Order>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientIdAsync] Начало поиска заказов для clientId={clientId}");
                    
                    var command = new SqlCommand(
                        @"SELECT o.*, 
                            ISNULL(c.full_name, '') as client_name, 
                            ISNULL(c.rating, 0) as client_rating, 
                            d.full_name as driver_name, 
                            d.DriverPhotoUrl,
                            car.ModelName as car_model, 
                            car.LicensePlate as license_plate, 
                            car.MainImageUrl as CarPhotoUrl
                   FROM Orders o
                   LEFT JOIN Users c ON o.client_id = c.user_id 
                   LEFT JOIN Users d ON o.driver_id = d.user_id
                   LEFT JOIN Cars car ON d.user_id = car.DriverId
                   WHERE o.client_id = @clientId 
                     AND o.Status NOT IN (@idle, @searching)
                   ORDER BY o.order_id DESC", connection);
                    command.Parameters.AddWithValue("@clientId", clientId);
                    command.Parameters.AddWithValue("@idle", (int)OrderState.Idle);
                    command.Parameters.AddWithValue("@searching", (int)OrderState.Searching);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var order = MapReaderToOrder(reader);
                            orders.Add(order);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientIdAsync] Ошибка при получении истории заказов: {ex.Message}");
                }
            }
            return orders;
        }

        public List<Order> GetPastOrdersByClientId(int clientId)
        {
            var orders = new List<Order>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    
                    System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Начало поиска заказов для clientId={clientId}");
                    
                    // Сначала проверим все заказы этого клиента
                    var checkAllCmd = new SqlCommand(
                        @"SELECT order_id, client_id, Status, PointA, PointB FROM Orders WHERE client_id = @clientId ORDER BY order_id DESC",
                        connection);
                    checkAllCmd.Parameters.AddWithValue("@clientId", clientId);
                    using (var checkReader = checkAllCmd.ExecuteReader())
                    {
                        System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Все заказы клиента {clientId} в БД:");
                        int totalCount = 0;
                        while (checkReader.Read())
                        {
                            totalCount++;
                            var orderId = checkReader["order_id"];
                            var dbClientId = checkReader["client_id"];
                            var status = checkReader["Status"];
                            var statusInt = (int)status;
                            var statusEnum = (OrderState)statusInt;
                            var pointA = checkReader["PointA"]?.ToString();
                            var pointB = checkReader["PointB"]?.ToString();
                            System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId]   Заказ #{orderId}, client_id в БД: {dbClientId}, статус: {statusInt} ({statusEnum}), откуда: {pointA}, куда: {pointB}");
                        }
                        System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Всего заказов в БД для этого клиента: {totalCount}");
                    }

                    // Теперь ищем заказы с нужными статусами
                    // Показываем все заказы, которые завершены (TripCompleted) или архивированы (Archived)
                    // Также включаем заказы, которые были в процессе, но завершились (TripInProgress может быть, если что-то пошло не так)
                    var completedStatus = (int)OrderState.TripCompleted;
                    var archivedStatus = (int)OrderState.Archived;
                    System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Ищем заказы со статусами: TripCompleted={completedStatus}, Archived={archivedStatus}");
                    
                    // Изменяем запрос: показываем все заказы, которые НЕ в статусе Searching или Idle
                    // Это включает TripCompleted, Archived, и любые другие завершенные статусы
                    // Используем LEFT JOIN для Users, чтобы не потерять заказы, если нет записи в Users
                    var command = new SqlCommand(
                        @"SELECT o.*, 
                            ISNULL(c.full_name, '') as client_name, 
                            ISNULL(c.rating, 0) as client_rating, 
                            d.full_name as driver_name, 
                            d.DriverPhotoUrl,
                            car.ModelName as car_model, 
                            car.LicensePlate as license_plate, 
                            car.MainImageUrl as CarPhotoUrl
                   FROM Orders o
                   LEFT JOIN Users c ON o.client_id = c.user_id 
                   LEFT JOIN Users d ON o.driver_id = d.user_id
                   LEFT JOIN Cars car ON d.user_id = car.DriverId
                   WHERE o.client_id = @clientId 
                     AND o.Status NOT IN (@idle, @searching)
                   ORDER BY o.order_id DESC", connection);
                    command.Parameters.AddWithValue("@clientId", clientId);
                    command.Parameters.AddWithValue("@idle", (int)OrderState.Idle);
                    command.Parameters.AddWithValue("@searching", (int)OrderState.Searching);
                    
                    System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Выполняем запрос для clientId={clientId}, исключаем статусы: Idle={(int)OrderState.Idle}, Searching={(int)OrderState.Searching}");

                    using (var reader = command.ExecuteReader())
                    {
                        int count = 0;
                        while (reader.Read())
                        {
                            var order = MapReaderToOrder(reader);
                            orders.Add(order);
                            count++;
                            System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Найден заказ #{order.order_id}, статус: {order.Status}, клиент: {order.OrderClient?.client_id}");
                        }
                        System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Всего найдено заказов с нужными статусами: {count}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] Ошибка при получении истории заказов: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[GetPastOrdersByClientId] StackTrace: {ex.StackTrace}");
                }
            }
            return orders;
        }

        public async Task<Order> CreateOrderAsync(Order newOrder)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    // ИЗМЕНЕН SQL-ЗАПРОС: Добавлены поле PaymentMethod и параметр @PaymentMethod
                    var command = new SqlCommand(
                        @"INSERT INTO Orders (client_id, PointA, PointB, Status, Tariff, TotalPrice, PaymentMethod) 
                  OUTPUT INSERTED.order_id 
                  VALUES (@client_id, @PointA, @PointB, @Status, @Tariff, @TotalPrice, @PaymentMethod)", connection);

                    command.Parameters.AddWithValue("@client_id", newOrder.OrderClient.client_id);
                    command.Parameters.AddWithValue("@PointA", newOrder.PointA);
                    command.Parameters.AddWithValue("@PointB", newOrder.PointB);
                    command.Parameters.AddWithValue("@Status", (int)newOrder.Status);
                    command.Parameters.AddWithValue("@Tariff", newOrder.Tariff);
                    command.Parameters.AddWithValue("@TotalPrice", newOrder.TotalPrice);
                    // ДОБАВЛЕНО: Передаем способ оплаты в базу данных
                    command.Parameters.AddWithValue("@PaymentMethod", (object)newOrder.PaymentMethod ?? DBNull.Value);

                    System.Diagnostics.Debug.WriteLine($"[CreateOrderAsync] Создание заказа: client_id={newOrder.OrderClient.client_id}, Status={newOrder.Status}");

                    var result = await command.ExecuteScalarAsync();
                    newOrder.order_id = (int)result;
                    System.Diagnostics.Debug.WriteLine($"[CreateOrderAsync] Заказ создан с order_id={newOrder.order_id}");
                    return newOrder;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при создании заказа: {ex.Message}");
                    MessageBox.Show($"Не удалось создать заказ: {ex.Message}", "Ошибка БД");
                    return null;
                }
            }
        }

        public Order CreateOrder(Order newOrder)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // ИЗМЕНЕН SQL-ЗАПРОС: Добавлены поле PaymentMethod и параметр @PaymentMethod
                    var command = new SqlCommand(
                        @"INSERT INTO Orders (client_id, PointA, PointB, Status, Tariff, TotalPrice, PaymentMethod) 
                  OUTPUT INSERTED.order_id 
                  VALUES (@client_id, @PointA, @PointB, @Status, @Tariff, @TotalPrice, @PaymentMethod)", connection);

                    command.Parameters.AddWithValue("@client_id", newOrder.OrderClient.client_id);
                    command.Parameters.AddWithValue("@PointA", newOrder.PointA);
                    command.Parameters.AddWithValue("@PointB", newOrder.PointB);
                    command.Parameters.AddWithValue("@Status", (int)newOrder.Status);
                    command.Parameters.AddWithValue("@Tariff", newOrder.Tariff);
                    command.Parameters.AddWithValue("@TotalPrice", newOrder.TotalPrice);
                    // ДОБАВЛЕНО: Передаем способ оплаты в базу данных
                    command.Parameters.AddWithValue("@PaymentMethod", (object)newOrder.PaymentMethod ?? DBNull.Value);

                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Создание заказа: client_id={newOrder.OrderClient.client_id}, Status={newOrder.Status}, PointA={newOrder.PointA}, PointB={newOrder.PointB}");

                    newOrder.order_id = (int)command.ExecuteScalar();
                    System.Diagnostics.Debug.WriteLine($"[CreateOrder] Заказ создан с order_id={newOrder.order_id}");
                    return newOrder;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при создании заказа: {ex.Message}");
                    MessageBox.Show($"Не удалось создать заказ: {ex.Message}", "Ошибка БД");
                    return null;
                }
            }
        }

        // Обновление статуса и/или водителя заказа
        public bool UpdateOrder(Order order)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Максимально упрощенный и прямой запрос для диагностики
                    var command = new SqlCommand(
                        @"UPDATE Orders SET 
                    Status = @Status, 
                    driver_id = @driver_id 
                  WHERE order_id = @order_id", connection);

                    command.Parameters.AddWithValue("@order_id", order.order_id);
                    command.Parameters.AddWithValue("@Status", (int)order.Status);

                    // Самый надежный способ передать ID водителя или NULL
                    if (order.AssignedDriver != null && order.AssignedDriver.driver_id > 0)
                    {
                        if (DriverExists(connection, order.AssignedDriver.driver_id))
                        {
                            command.Parameters.AddWithValue("@driver_id", order.AssignedDriver.driver_id);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[UpdateOrder] Водитель {order.AssignedDriver.driver_id} не найден в Users. Сохраняем driver_id = NULL.");
                            command.Parameters.AddWithValue("@driver_id", DBNull.Value);
                        }
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@driver_id", DBNull.Value);
                    }

                    int rowsAffected = command.ExecuteNonQuery();

                    // Логирование результата в консоль отладки
                    if (rowsAffected == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpdateOrder] ПРЕДУПРЕЖДЕНИЕ: Запрос UPDATE не затронул ни одной строки для order_id {order.order_id}. Проверьте, существует ли такой заказ.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpdateOrder] УСПЕХ: Обновлено {rowsAffected} строк для order_id {order.order_id}. Новый статус: {order.Status}, driver_id: {order.AssignedDriver?.driver_id}");
                        
                        // Проверяем, что заказ действительно обновился в БД
                        var verifyOrder = GetOrderById(order.order_id);
                        if (verifyOrder != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[UpdateOrder] Проверка: заказ #{verifyOrder.order_id} в БД имеет статус {verifyOrder.Status}, client_id={verifyOrder.OrderClient?.client_id}");
                        }
                    }

                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    // Логирование критической ошибки
                    System.Diagnostics.Debug.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА в UpdateOrder для заказа {order.order_id}: {ex.Message}");
                    MessageBox.Show($"Ошибка при обновлении заказа: {ex.Message}", "Ошибка БД");
                    return false;
                }
            }
        }

        // Получение заказа по ID (включая данные клиента и водителя, если есть)
        public Order GetOrderById(int orderId)
        {
            Order order = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Используем LEFT JOIN, чтобы получить заказ, даже если водитель еще не назначен
                    // Добавляем JOIN с Cars для получения информации о машине
                    var command = new SqlCommand(
   @"SELECT o.*, c.full_name as client_name, c.rating as client_rating, 
            d.full_name as driver_name, d.DriverPhotoUrl,
            car.ModelName as car_model, car.LicensePlate as license_plate, car.MainImageUrl as CarPhotoUrl
     FROM Orders o
     JOIN Users c ON o.client_id = c.user_id 
     LEFT JOIN Users d ON o.driver_id = d.user_id
     LEFT JOIN Cars car ON d.user_id = car.DriverId
     WHERE o.order_id = @order_id", connection);
                    command.Parameters.AddWithValue("@order_id", orderId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            order = MapReaderToOrder(reader);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении заказа {orderId}: {ex.Message}");
                }
            }
            return order;
        }

        private bool DriverExists(SqlConnection connection, int driverId)
        {
            using (var checkCmd = new SqlCommand("SELECT COUNT(1) FROM Users WHERE user_id = @driver_id", connection))
            {
                checkCmd.Parameters.AddWithValue("@driver_id", driverId);
                return (int)checkCmd.ExecuteScalar() > 0;
            }
        }

        // Получение списка доступных заказов (для водителя)
        public List<Order> GetAvailableOrders()
        {
            var orders = new List<Order>();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    // Выбираем заказы со статусом Searching (1) и присоединяем имя клиента и рейтинг
                    var command = new SqlCommand(
                        @"SELECT o.*, c.full_name as client_name, c.rating as client_rating 
                           FROM Orders o
                           JOIN Users c ON o.client_id = c.user_id 
                           WHERE o.Status = @Status AND o.driver_id IS NULL", connection);
                    command.Parameters.AddWithValue("@Status", (int)OrderState.Searching);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(MapReaderToOrder(reader));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при получении доступных заказов: {ex.Message}");
                }
            }
            return orders;
        }

        // Вспомогательный метод для маппинга данных из SqlDataReader в объект Order
        private Order MapReaderToOrder(SqlDataReader reader)
        {
            var order = new Order
            {
                order_id = (int)reader["order_id"],
                PointA = reader["PointA"].ToString(),
                PointB = reader["PointB"].ToString(),
                Status = (OrderState)(int)reader["Status"], // Преобразуем int в enum
                Tariff = reader["Tariff"].ToString(),
                TotalPrice = (decimal)reader["TotalPrice"],

                PaymentMethod = reader["PaymentMethod"] as string,
                OrderClient = new Client
                {
                    client_id = (int)reader["client_id"],
                    full_name = reader["client_name"]?.ToString(), // Имя клиента из JOIN
                    rating = reader["client_rating"] as decimal? // Рейтинг клиента из JOIN
                }
            };
            

            // Проверяем, есть ли водитель (driver_id не NULL?)
            if (reader["driver_id"] != DBNull.Value)
            {
                order.AssignedDriver = new Driver
                {
                    driver_id = (int)reader["driver_id"],
                    full_name = reader["driver_name"]?.ToString(),
                    DriverPhotoUrl = reader["DriverPhotoUrl"] as string,
                    car_model = reader["car_model"] as string ?? string.Empty,
                    license_plate = reader["license_plate"] as string ?? string.Empty,
                    CarPhotoUrl = reader["CarPhotoUrl"] as string
                };
            }

            return order;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaxiWPF.Models;
using TaxiWPF.Repositories;


namespace TaxiWPF.Services
{
    // Сервис остается Singleton, но теперь работает через OrderRepository
    public class OrderService
    {
        private readonly OrderRepository _orderRepository; // <-- Новый репозиторий
                                                           // --- СПИСОК _activeOrders УДАЛЕН ---

        private static readonly Lazy<OrderService> _instance = new Lazy<OrderService>(() => new OrderService());

        public event Action<Order> OrderUpdated;

        // Приватный конструктор для Singleton
        private OrderService()
        {
            _orderRepository = new OrderRepository(); // Создаем репозиторий один раз
        }

        public static OrderService Instance => _instance.Value;

        public async Task<Order> SubmitOrderAsync(Order newOrder)
        {
            newOrder.Status = OrderState.Searching; // Устанавливаем начальный статус

            // Создаем заказ в БД через репозиторий асинхронно
            var createdOrder = await _orderRepository.CreateOrderAsync(newOrder);

            if (createdOrder != null)
            {
                Notify(createdOrder); // Оповещаем о НОВОМ заказе
            }
            return createdOrder; // Возвращаем заказ с ID
        }

        // Клиент вызывает этот метод
        public Order SubmitOrder(Order newOrder)
        {
            newOrder.Status = OrderState.Searching; // Устанавливаем начальный статус

            // Создаем заказ в БД через репозиторий
            var createdOrder = _orderRepository.CreateOrder(newOrder);

            if (createdOrder != null)
            {
                Notify(createdOrder); // Оповещаем о НОВОМ заказе
            }
            return createdOrder; // Возвращаем заказ с ID
        }

        // Водитель вызывает, когда принимает заказ
        public void AcceptOrder(Order order, Driver driver)
        {
            // --- НАЧАЛО ИЗМЕНЕНИЙ ---
            // Добавляем проверку, чтобы убедиться, что у нас есть корректные ID
            if (order == null || order.order_id == 0 || driver == null || driver.driver_id == 0)
            {
                System.Diagnostics.Debug.WriteLine("ОШИБКА: AcceptOrder получил неверный ID заказа или водителя.");
                return;
            }

            var existingOrder = _orderRepository.GetOrderById(order.order_id);
            // --- КОНЕЦ ИЗМЕНЕНИЙ ---

            if (existingOrder != null && existingOrder.Status == OrderState.Searching)
            {
                existingOrder.AssignedDriver = driver;
                existingOrder.Status = OrderState.DriverEnRoute;

                if (_orderRepository.UpdateOrder(existingOrder))
                {
                    // Перезагружаем заказ из БД, чтобы получить полные данные о водителе и машине
                    var updatedOrder = _orderRepository.GetOrderById(order.order_id);
                    if (updatedOrder != null)
                    {
                        if (updatedOrder.AssignedDriver == null && existingOrder.AssignedDriver != null)
                        {
                            updatedOrder.AssignedDriver = existingOrder.AssignedDriver;
                        }
                        Notify(updatedOrder);
                    }
                    else
                    {
                        // Если не удалось загрузить, отправляем то, что есть
                        Notify(existingOrder);
                    }
                }
            }
        }

        // Водитель нажал "Прибыл"
        public void DriverArrived(Order order)
        {
            UpdateOrderStatus(order, OrderState.DriverEnRoute, OrderState.DriverArrived);
        }

        // Водитель нажал "Начать поездку"
        public void StartTrip(Order order)
        {
            UpdateOrderStatus(order, OrderState.DriverArrived, OrderState.TripInProgress);
        }

        // Водитель нажал "Завершить"
        public void CompleteOrder(Order order)
        {
            UpdateOrderStatus(order, OrderState.TripInProgress, OrderState.TripCompleted);
        }

        // Вызывается, когда заказ можно убрать из вида (например, отменен клиентом или обе стороны поставили оценку)
        public void ArchiveOrder(Order order)
        {
            // --- ИЗМЕНЕНИЕ: Получаем самую свежую версию заказа из БД ---
            var freshOrder = _orderRepository.GetOrderById(order.order_id);
            if (freshOrder == null) return;

            if (freshOrder.Status == OrderState.TripCompleted)
            {
                UpdateOrderStatus(freshOrder, OrderState.TripCompleted, OrderState.Archived);
            }
            else if (freshOrder.Status == OrderState.Searching)
            {
                UpdateOrderStatus(freshOrder, OrderState.Searching, OrderState.Archived);
            }
        }


        // Водитель вызывает этот метод (делегируем репозиторию)
        public List<Order> GetAvailableOrders()
        {
            // Просто возвращаем результат из репозитория
            return _orderRepository.GetAvailableOrders();
        }

        // Вспомогательный метод для обновления статуса заказа в БД
        private void UpdateOrderStatus(Order orderWithDriverInfo, OrderState expectedCurrentState, OrderState newState)
        {
            var existingOrder = _orderRepository.GetOrderById(orderWithDriverInfo.order_id);

            if (existingOrder != null && existingOrder.AssignedDriver == null && orderWithDriverInfo.AssignedDriver != null)
            {
                existingOrder.AssignedDriver = orderWithDriverInfo.AssignedDriver;
            }

            // Обновляем, только если текущий статус совпадает с ожидаемым
            if (existingOrder != null && existingOrder.Status == expectedCurrentState)
            {
                existingOrder.Status = newState;

                if (_orderRepository.UpdateOrder(existingOrder))
                {
                    // Перезагружаем заказ из БД, чтобы получить полные данные
                    var updatedOrder = _orderRepository.GetOrderById(orderWithDriverInfo.order_id);
                    if (updatedOrder != null)
                    {
                        if (updatedOrder.AssignedDriver == null && existingOrder.AssignedDriver != null)
                        {
                            updatedOrder.AssignedDriver = existingOrder.AssignedDriver;
                        }
                        Notify(updatedOrder); // Оповещаем об изменении
                    }
                    else
                    {
                        Notify(existingOrder); // Если не удалось загрузить, отправляем то, что есть
                    }
                }
            }
        }


        // Главный оповещатель (остался без изменений)
        private void Notify(Order order)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderService] Notify вызван для заказа #{order.order_id}, статус: {order.Status}, подписчиков: {OrderUpdated?.GetInvocationList()?.Length ?? 0}");
            OrderUpdated?.Invoke(order);
        }
    }
}

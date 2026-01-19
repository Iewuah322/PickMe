using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    // Этот enum будет управлять ВСЕМ процессом
    public enum OrderState
    {
        Idle,           // Исходное состояние (у клиента)
        Searching,      // Идет поиск (у клиента и в сервисе)
        DriverEnRoute,  // Водитель едет (у клиента и водителя)
        DriverArrived,  // Водитель прибыл (у клиента и водителя)
        TripInProgress, // Поездка идет (у клиента и водителя)
        TripCompleted,  // Поездка завершена (оба ставят оценки)
        Archived        // Заказ закрыт
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaxiWPF.Models
{
    public class Driver
    {
        public int driver_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string car_model { get; set; } = string.Empty;
        public string license_plate { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string geo_position { get; set; }
        public decimal? rating { get; set; }
        public string phone { get; set; } = string.Empty;
        public string DriverPhotoUrl { get; set; } // URL для фото водителя
        public string CarPhotoUrl { get; set; }
    }
}

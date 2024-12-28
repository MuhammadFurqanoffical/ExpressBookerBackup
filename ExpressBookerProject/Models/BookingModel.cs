using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExpressBookerProject.Models
{
    public class BookingModel
    {
        public int ScheduleId { get; set; }
        public string BusNumber { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public int NumSeats { get; set; }
        public decimal TotalPrice { get; set; }

    }
}
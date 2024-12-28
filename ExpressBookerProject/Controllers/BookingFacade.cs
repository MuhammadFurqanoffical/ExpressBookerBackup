using System;
using System.Collections.Generic;
using System.Linq;
using ExpressBookerProject.Models;

namespace ExpressBookerProject.Services
{
    public class BookingFacade
    {
        private readonly expressbookerEntities _context;

        public BookingFacade()
        {
            _context = new expressbookerEntities();
        }

        public user AuthenticateUser(string username, string password)
        {
            return _context.users.FirstOrDefault(u => u.username == username && u.password == password);
        }

        public decimal CalculatePrice(busschedule schedule)
        {
            decimal pricePerKilometer = 10.0m;
            decimal distance = schedule.route.distance;
            return distance * pricePerKilometer;
        }

        public List<busschedule> GetBusSchedules()
        {
            var schedules = _context.busschedules.Include("route").ToList();
            foreach (var schedule in schedules)
            {
                schedule.Price = CalculatePrice(schedule);
            }
            return schedules;
        }

        public busschedule GetBusSchedule(int scheduleId)
        {
            return _context.busschedules
                .Include("bus")
                .Include("route")
                .FirstOrDefault(s => s.scheduleid == scheduleId);
        }

        public int GetAvailableSeats(busschedule schedule)
        {
            int totalBookedSeats = _context.bookings.Count(b => b.busid == schedule.busid);
            return schedule.bus.capacity - totalBookedSeats;
        }

        public bool BookSeats(int userId, int scheduleId, int seats, out List<string> bookedSeatNumbers)
        {
            bookedSeatNumbers = new List<string>();

            var schedule = GetBusSchedule(scheduleId);
            if (schedule == null)
            {
                return false;
            }

            int availableSeats = GetAvailableSeats(schedule);
            if (seats > availableSeats)
            {
                return false;
            }

            for (int i = 0; i < seats; i++)
            {
                string seatNumber = GetNextAvailableSeat(schedule.busid);
                if (string.IsNullOrEmpty(seatNumber))
                {
                    return false;
                }

                booking newBooking = new booking
                {
                    userid = userId,
                    busid = schedule.busid,
                    routeid = schedule.routeid,
                    bookingdate = DateTime.Now,
                    seatnumber = seatNumber,
                    status = "Confirmed"
                };

                _context.bookings.Add(newBooking);
                _context.SaveChanges();

                bookedSeatNumbers.Add(seatNumber);
            }

            return true;
        }

        public bool ProcessPayment(int userId, int scheduleId, int numSeats, string paymentMethod)
        {
            var schedule = GetBusSchedule(scheduleId);
            if (schedule == null)
            {
                return false;
            }

            decimal totalAmount = numSeats * schedule.route.distance * 0.1m;

            payment newPayment = new payment
            {
                bookingid = _context.bookings.First(b => b.userid == userId && b.busid == schedule.busid && b.routeid == schedule.routeid).bookingid,
                amount = totalAmount,
                paymentdate = DateTime.Now,
                paymentmethod = paymentMethod
            };

            _context.payments.Add(newPayment);
            _context.SaveChanges();

            return true;
        }

        private string GetNextAvailableSeat(int busId)
        {
            var bookedSeatNumbers = _context.bookings.Where(b => b.busid == busId).Select(b => b.seatnumber).ToList();
            var totalSeats = _context.buses.Find(busId).capacity;

            for (int seatNumber = 1; seatNumber <= totalSeats; seatNumber++)
            {
                if (!bookedSeatNumbers.Contains(seatNumber.ToString()))
                {
                    return seatNumber.ToString();
                }
            }

            return null;
        }

        public int GetUserId(string username)
        {
            return _context.users.FirstOrDefault(u => u.username == username)?.userid ?? 0;
        }
    }
}

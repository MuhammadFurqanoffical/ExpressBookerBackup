using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ExpressBookerProject.Models;
using ExpressBookerProject.Services;
using ExpressBookerProject.Utilities; // Add this for UserSessionAuthorize


namespace ExpressBookerProject.Controllers
{
    [UserSessionAuthorize] // Restrict access to logged-in users
    public class UserController : Controller
    {
        private readonly BookingFacade _facade;

        public UserController()
        {
            _facade = new BookingFacade();
        }
        public UserController(BookingFacade facade)
        {
            _facade = facade;
        }


        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            DisableCaching();
            base.OnActionExecuting(filterContext);
        }

        private void DisableCaching()
        {
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
        }

        [AllowAnonymous]
        public ActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(user model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _facade.AuthenticateUser(model.username, model.password);

            if (user == null || user.roleid == 1)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View(model);
            }

            Session["UserID"] = user.userid;
            return RedirectToAction("BusSchedule");
        }

        public ActionResult BusSchedule()
        {
            var schedules = _facade.GetBusSchedules();
            return View(schedules);
        }

        public ActionResult BookSeats(int id)
        {
            var schedule = _facade.GetBusSchedule(id);
            if (schedule == null) return HttpNotFound();

            ViewBag.AvailableSeats = _facade.GetAvailableSeats(schedule);
            return View(schedule);
        }

        [HttpPost]
        public ActionResult BookSeats(int scheduleId, int seats)
        {
            var schedule = _facade.GetBusSchedule(scheduleId);
            if (schedule == null)
            {
                return HttpNotFound();
            }

            int availableSeats = _facade.GetAvailableSeats(schedule);
            if (seats > availableSeats)
            {
                ModelState.AddModelError("", "Not enough available seats.");
                ViewBag.AvailableSeats = availableSeats;
                return View(schedule);
            }

            decimal totalPrice = _facade.CalculatePrice(schedule) * seats;

            ViewBag.ScheduleId = schedule.scheduleid;
            ViewBag.BusNumber = schedule.bus.busnumber;
            ViewBag.DepartureTime = schedule.departuretime;
            ViewBag.ArrivalTime = schedule.arrivaltime;
            ViewBag.Source = schedule.route.source;
            ViewBag.Destination = schedule.route.destination;
            ViewBag.NumSeats = seats;
            ViewBag.TotalPrice = totalPrice;

            return View("ConfirmBooking");
        }

    private void SetModelStateError(string message, int availableSeats, busschedule schedule)
        {
            ModelState.AddModelError("", message);
            ViewBag.AvailableSeats = availableSeats;
        }

        private void SetConfirmBookingViewData(busschedule schedule, int seats, decimal totalPrice)
        {
            ViewBag.ScheduleId = schedule.scheduleid;
            ViewBag.BusNumber = schedule.bus.busnumber;
            ViewBag.DepartureTime = schedule.departuretime;
            ViewBag.ArrivalTime = schedule.arrivaltime;
            ViewBag.Source = schedule.route.source;
            ViewBag.Destination = schedule.route.destination;
            ViewBag.NumSeats = seats;
            ViewBag.TotalPrice = totalPrice;
        }

        [HttpPost]
        public ActionResult ConfirmBooking(int scheduleId, int numSeats)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login");

            var success = _facade.BookSeats(userId, scheduleId, numSeats, out var bookedSeatNumbers);
            if (!success) return View("Error");

            return RedirectToAction("Payment", new { scheduleId, numSeats, bookedSeatNumbers });
        }

        public ActionResult Payment(int scheduleId, int numSeats)
        {
            ViewBag.ScheduleId = scheduleId;
            ViewBag.NumSeats = numSeats;
            return View();
        }

        [HttpPost]
        public ActionResult Payment(int scheduleId, int numSeats, string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
            {
                ModelState.AddModelError("", "Please select a valid payment method.");
                ViewBag.ScheduleId = scheduleId;
                ViewBag.NumSeats = numSeats;
                return View();
            }

            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login");

            var success = _facade.ProcessPayment(userId, scheduleId, numSeats, paymentMethod);
            if (!success) return View("Error");

            return RedirectToAction("BookingSuccess");
        }

        public ActionResult BookingSuccess() => View();

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }

        private int GetCurrentUserId()
        {
            return Session["UserID"] is int userId ? userId : 0;
        }
    }
}
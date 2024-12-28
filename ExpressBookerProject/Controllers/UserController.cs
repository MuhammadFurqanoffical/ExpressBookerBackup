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

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(filterContext);
        }

        [AllowAnonymous] // Allow access to the login page without authentication
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous] // Allow access to login POST without authentication
        [ValidateAntiForgeryToken]
        public ActionResult Login(user model)
        {
            if (ModelState.IsValid)
            {
                var user = _facade.AuthenticateUser(model.username, model.password);

                if (user != null)
                {
                    if (user.roleid == 1) // Prevent admin login here
                    {
                        ViewBag.ErrorMessage = "Wrong credentials";
                        return View(model);
                    }

                    // Store user ID in session
                    Session["UserID"] = user.userid;
                    return RedirectToAction("BusSchedule", "User");
                }
                else
                {
                    ViewBag.ErrorMessage = "Invalid username or password.";
                    return View(model);
                }
            }
            return View(model);
        }
        public ActionResult BusSchedule()
        {
            var schedules = _facade.GetBusSchedules();
            return View(schedules);
        }

        public ActionResult BookSeats(int id)
        {
            var schedule = _facade.GetBusSchedule(id);
            if (schedule == null)
            {
                return HttpNotFound();
            }

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

            var availableSeats = _facade.GetAvailableSeats(schedule);
            if (seats > availableSeats)
            {
                ModelState.AddModelError("", "Not enough available seats.");
                ViewBag.AvailableSeats = availableSeats;
                return View(schedule);
            }

            var totalPrice = _facade.CalculatePrice(schedule) * seats;

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

        [HttpPost]
        public ActionResult ConfirmBooking(int scheduleId, int numSeats)
        {
            int userId = (int)Session["UserID"]; // Retrieve the actual user ID from session

            var success = _facade.BookSeats(userId, scheduleId, numSeats, out var bookedSeatNumbers);
            if (!success)
            {
                ModelState.AddModelError("", "Not enough available seats.");
                return View("Error");
            }

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
                return View(); // Reload the payment page
            }

            int userId = (int)Session["UserID"]; // Retrieve the actual user ID from session

            // Simulate processing payment
            var success = _facade.ProcessPayment(userId, scheduleId, numSeats, paymentMethod);
            if (!success)
            {
                ModelState.AddModelError("", "Payment processing failed. Try again.");
                return View("Error");
            }

            return RedirectToAction("BookingSuccess");
        }

        public ActionResult BookingSuccess()
        {
            return View();
        }

        public ActionResult Logout()
        {
            Session["UserID"] = null; // Clear the session
            return RedirectToAction("Login", "User");
        }
    }
}

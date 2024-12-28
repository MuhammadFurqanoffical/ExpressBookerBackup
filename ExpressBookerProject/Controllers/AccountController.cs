using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ExpressBookerProject.Models;
using ExpressBookerProject.Utilities; // Add this using directive for the SessionManager

namespace ExpressBookerProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly expressbookerEntities _context;

        public AccountController()
        {
            _context = new expressbookerEntities();
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            if (AdminSessionManager.GetCurrentAdminSession() != null)
            {
                ViewBag.AdminLoginNotification = "Another admin is already logged in. Please try again later.";
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (AdminSessionManager.GetCurrentAdminSession() != null)
            {
                ViewBag.AdminLoginNotification = "Another admin is already logged in. Please try again later.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Authenticate the user based on username and password
            var user = _context.users.FirstOrDefault(u => u.username == model.Username && u.password == model.Password);

            if (user != null)
            {
                // Get role name dynamically using roleid (instead of hard-coding roleid values)
                var role = _context.roles.FirstOrDefault(r => r.roleid == user.roleid)?.rolename;

                if (role != "Admin") // Ensure user is an Admin (you can adjust this logic as needed)
                {
                    model.Password = ""; // Clear the password field
                    ViewBag.ErrorMessage = "You do not have permission to access the admin panel.";
                    return View(model); // Return the model with the error message and retain the entered username
                }

                // If the user is an admin, start an admin session
                var adminSession = new AdminSession { UserId = user.userid, Username = user.username };
                if (!AdminSessionManager.SetCurrentAdminSession(adminSession))
                {
                    ViewBag.AdminLoginNotification = "Another admin is already logged in. Please try again later.";
                    return View(model);
                }

                return RedirectToAction("AdminDashboard", "Admin");
            }
            else
            {
                // Invalid username or password, clear password field
                model.Password = ""; // Ensure password is cleared
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View(model); // Return the model with the error message
            }
        }

        public ActionResult Logout()
        {
            // Clear the admin session using the session manager
            AdminSessionManager.ClearCurrentAdminSession();

            // Optionally clear all session data
            Session.Clear();
            Session.Abandon();

            // Redirect to the login page
            return RedirectToAction("Login", "Home");
        }

    }
}

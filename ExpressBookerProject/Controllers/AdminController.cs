using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Web.Mvc;
using ExpressBookerProject.Utilities; // For AdminSessionAuthorize
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

using ExpressBookerProject.Models;

namespace ExpressBookerProject.Controllers
{
    [AdminSessionAuthorize] // Use the custom authorization
    public class AdminController : Controller
    {
        private readonly expressbookerEntities _context;

        public AdminController()
        {
            _context = new expressbookerEntities();
        }
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            base.OnActionExecuting(filterContext);
        }
        // Admin dashboard action
        public ActionResult AdminDashboard()
        {
            return View();
        }




        // Bus Management
        public ActionResult AddBus()
        {
            var drivers = _context.drivers.ToList();
            ViewBag.DriverList = drivers.Select(d => new SelectListItem
            {
                Value = d.driverid.ToString(),
                Text = d.name
            });

            // Generate the dropdown list for capacity
            ViewBag.CapacityList = Enumerable.Range(20, 11) // Range from 20 to 30
                                              .Select(c => new SelectListItem
                                              {
                                                  Value = c.ToString(),
                                                  Text = c.ToString()
                                              });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBus(bus model)
        {
            if (ModelState.IsValid)
            {
                // Save the bus model to the database
                _context.buses.Add(model);
                _context.SaveChanges();
                return RedirectToAction("ViewBus");
            }

            // Repopulate dropdown lists if model state is invalid
            var drivers = _context.drivers.ToList();
            ViewBag.DriverList = drivers.Select(d => new SelectListItem
            {
                Value = d.driverid.ToString(),
                Text = d.name
            });

            ViewBag.CapacityList = Enumerable.Range(20, 11)
                                              .Select(c => new SelectListItem
                                              {
                                                  Value = c.ToString(),
                                                  Text = c.ToString()
                                              });

            return View(model);
        }


        public ActionResult ViewBus()
        {
            var buses = _context.buses.ToList();
            return View(buses);
        }

        // EditBus action
        public ActionResult EditBus(int id)
        {
            var bus = _context.buses.Find(id);
            if (bus == null)
            {
                return HttpNotFound();
            }
            var drivers = _context.drivers.ToList();
            ViewBag.DriverList = new SelectList(drivers, "driverid", "name", bus.driverid); // Modify this line
            return View(bus);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBus(bus model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Entry(model).State = EntityState.Modified;
                    _context.SaveChanges();
                    return RedirectToAction("ViewBus");
                }
                catch (Exception e)
                {
                    TempData["Message"] = "<script>alert('Data not updated: " + e.Message + "')</script>";
                }
            }
            var drivers = _context.drivers.ToList();
            ViewBag.DriverList = new SelectList(drivers, "driverid", "name", model.driverid);
            return View(model);
        }

        // DeleteBus action
        public ActionResult DeleteBus(int id)
        {
            var bus = _context.buses.Find(id);
            if (bus == null)
            {
                return HttpNotFound();
            }
            return View(bus);
        }

        [HttpPost, ActionName("DeleteBus")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBusConfirmed(int id)
        {
            try
            {
                var bus = _context.buses.Find(id);
                if (bus != null)
                {
                    _context.buses.Remove(bus);
                    _context.SaveChanges();
                }
                else
                {
                    TempData["Message"] = "<script>alert('Data not found')</script>";
                }
            }
            catch (Exception e)
            {
                TempData["Message"] = "<script>alert('Data not deleted: " + e.Message + "')</script>";
            }
            return RedirectToAction("ViewBus");
        }


        public ActionResult AddRoute()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRoute(route model)
        {
            if (ModelState.IsValid)
            {
                // Server-side validation to check if distance is negative
                if (model.distance < 0)
                {
                    ModelState.AddModelError("distance", "Distance cannot be negative.");
                    return View(model);
                }

                try
                {
                    _context.routes.Add(model);
                    _context.SaveChanges();
                    return RedirectToAction("ViewRoute");
                }
                catch (Exception e)
                {
                    TempData["Message"] = "<script>alert('Failed to add route: " + e.Message + "')</script>";
                }
            }
            return View(model);
        }


        public ActionResult ViewRoute()
        {
            var routes = _context.routes.ToList();
            return View(routes);
        }

        public ActionResult EditRoute(int id)
        {
            var route = _context.routes.Find(id);
            if (route == null)
            {
                return HttpNotFound();
            }
            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoute(route model)
        {
            if (ModelState.IsValid)
            {
                // Server-side validation to ensure distance is not negative
                if (model.distance < 0)
                {
                    ModelState.AddModelError("distance", "Distance cannot be negative.");
                    return View(model);  // Return the view with validation message
                }

                try
                {
                    _context.Entry(model).State = EntityState.Modified;
                    _context.SaveChanges();
                    return RedirectToAction("ViewRoute");
                }
                catch (Exception e)
                {
                    TempData["Message"] = "<script>alert('Failed to update route: " + e.Message + "')</script>";
                }
            }
            return View(model);
        }


        public ActionResult DeleteRoute(int id)
        {
            var route = _context.routes.Find(id);
            if (route == null)
            {
                return HttpNotFound();
            }
            return View(route);
        }

        [HttpPost, ActionName("DeleteRoute")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRouteConfirmed(int id)
        {
            try
            {
                var route = _context.routes.Find(id);
                if (route == null)
                {
                    TempData["Message"] = "<script>alert('Route not found')</script>";
                    return RedirectToAction("ViewRoute");
                }

                // Check if the route is in use in any bus schedule
                var isRouteInUse = _context.busschedules.Any(bs => bs.routeid == route.routeid);
                if (isRouteInUse)
                {
                    // If the route is in use, show a message
                    TempData["Message"] = "<script>alert('This route is currently used in a bus schedule. Please remove it from the schedule first.')</script>";
                    return RedirectToAction("ViewRoute");
                }

                // Proceed with route deletion if it's not in use
                _context.routes.Remove(route);
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                TempData["Message"] = "<script>alert('Failed to delete route: " + e.Message + "')</script>";
            }
            return RedirectToAction("ViewRoute");
        }

        // ViewBusSchedule action
        public ActionResult ViewBusSchedule()
        {
            var busSchedules = _context.busschedules.Include(b => b.bus).Include(r => r.route).ToList();
            return View(busSchedules);
        }


        // AddBusSchedule actions
        public ActionResult AddBusSchedule()
        {
            ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber");
            ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
            {
                routeid = r.routeid,
                RouteDisplay = r.source + " - " + r.destination
            }), "routeid", "RouteDisplay");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBusSchedule(busschedule model)
        {
            if (ModelState.IsValid)
            {
                // Validation: Check that Arrival Time is after Departure Time
                if (model.departuretime >= model.arrivaltime)
                {
                    TempData["Message"] = "<script>alert('Arrival time must be after the departure time.')</script>";
                    ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
                    ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
                    {
                        routeid = r.routeid,
                        RouteDisplay = r.source + " - " + r.destination
                    }), "routeid", "RouteDisplay", model.routeid);
                    return View(model);
                }

                _context.busschedules.Add(model);
                _context.SaveChanges();
                return RedirectToAction("ViewBusSchedule");
            }

            ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
            ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
            {
                routeid = r.routeid,
                RouteDisplay = r.source + " - " + r.destination
            }), "routeid", "RouteDisplay", model.routeid);
            return View(model);
        }


        // EditBusSchedule actions
        public ActionResult EditBusSchedule(int id)
        {
            var busSchedule = _context.busschedules.Find(id);
            if (busSchedule == null)
            {
                return HttpNotFound();
            }

            ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", busSchedule.busid);
            ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
            {
                routeid = r.routeid,
                RouteDisplay = r.source + " - " + r.destination
            }), "routeid", "RouteDisplay", busSchedule.routeid);

            return View(busSchedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBusSchedule(busschedule model)
        {
            if (ModelState.IsValid)
            {
                // Server-side validation to ensure arrival time is after departure time
                if (model.arrivaltime <= model.departuretime)
                {
                    // Add a custom error to the ModelState if the arrival time is not after the departure time
                    ModelState.AddModelError("", "Arrival time must be after departure time.");

                    // Reload the dropdown lists if validation fails
                    ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
                    ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
                    {
                        routeid = r.routeid,
                        RouteDisplay = r.source + " - " + r.destination
                    }), "routeid", "RouteDisplay", model.routeid);

                    return View(model);  // Return the view with the error message
                }

                try
                {
                    _context.Entry(model).State = EntityState.Modified;
                    _context.SaveChanges();
                    TempData["Message"] = "<script>alert('Bus schedule updated successfully!')</script>";
                    return RedirectToAction("ViewBusSchedule");
                }
                catch (Exception e)
                {
                    TempData["Message"] = $"<script>alert('Error updating bus schedule: {e.Message}')</script>";
                }
            }

            // Reload the dropdown lists if validation fails
            ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
            ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
            {
                routeid = r.routeid,
                RouteDisplay = r.source + " - " + r.destination
            }), "routeid", "RouteDisplay", model.routeid);

            return View(model);
        }



        // DeleteBusSchedule actions
        public ActionResult DeleteBusSchedule(int id)
        {
            var busSchedule = _context.busschedules.Find(id);
            if (busSchedule == null)
            {
                return HttpNotFound();
            }
            return View(busSchedule);
        }

        [HttpPost, ActionName("DeleteBusSchedule")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBusScheduleConfirmed(int id)
        {
            try
            {
                var busSchedule = _context.busschedules.Find(id);
                if (busSchedule != null)
                {
                    _context.busschedules.Remove(busSchedule);
                    _context.SaveChanges();
                }
                else
                {
                    TempData["Message"] = "<script>alert('Bus schedule not found')</script>";
                }
            }
            catch (Exception e)
            {
                TempData["Message"] = "<script>alert('Bus schedule not deleted: " + e.Message + "')</script>";
            }
            return RedirectToAction("ViewBusSchedule");

        }
        private readonly string _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["expressbooker"].ConnectionString;


       
        public ActionResult ViewDriver()
        {
            List<driver> drivers = new List<driver>();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM drivers";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        drivers.Add(new driver
                        {
                            driverid = (int)reader["driverid"],
                            name = reader["name"].ToString(),
                            licensenumber = reader["licensenumber"].ToString(),
                            phone = reader["phone"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "<script>alert('Error loading drivers: " + ex.Message + "')</script>";
            }
            return View(drivers);
        }

        public ActionResult AddDriver()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddDriver(driver model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        string query = "INSERT INTO drivers (name, licensenumber, phone) VALUES (@Name, @LicenseNumber, @Phone)";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Name", model.name);
                        cmd.Parameters.AddWithValue("@LicenseNumber", model.licensenumber);
                        cmd.Parameters.AddWithValue("@Phone", model.phone);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    return RedirectToAction("ViewDriver");
                }
                catch (SqlException ex)
                {
                    TempData["Message"] = "<script>alert('Error adding driver: " + ex.Message + "')</script>";
                }
            }
            return View(model);
        }

        public ActionResult EditDriver(int id)
        {
            driver driver = null;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM drivers WHERE driverid = @DriverId";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DriverId", id);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        driver = new driver
                        {
                            driverid = (int)reader["driverid"],
                            name = reader["name"].ToString(),
                            licensenumber = reader["licensenumber"].ToString(),
                            phone = reader["phone"].ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "<script>alert('Error loading driver details: " + ex.Message + "')</script>";
            }
            if (driver == null)
            {
                return HttpNotFound();
            }
            return View(driver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditDriver(driver model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        string query = "UPDATE drivers SET name = @Name, licensenumber = @LicenseNumber, phone = @Phone WHERE driverid = @DriverId";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Name", model.name);
                        cmd.Parameters.AddWithValue("@LicenseNumber", model.licensenumber);
                        cmd.Parameters.AddWithValue("@Phone", model.phone);
                        cmd.Parameters.AddWithValue("@DriverId", model.driverid);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    return RedirectToAction("ViewDriver");
                }
                catch (Exception ex)
                {
                    TempData["Message"] = "<script>alert('Error updating driver: " + ex.Message + "')</script>";
                }
            }
            return View(model);
        }

        public ActionResult DeleteDriver(int id)
        {
            driver driver = null;
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "SELECT * FROM drivers WHERE driverid = @DriverId";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DriverId", id);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        driver = new driver
                        {
                            driverid = (int)reader["driverid"],
                            name = reader["name"].ToString(),
                            licensenumber = reader["licensenumber"].ToString(),
                            phone = reader["phone"].ToString()
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "<script>alert('Error loading driver details: " + ex.Message + "')</script>";
            }
            if (driver == null)
            {
                return HttpNotFound();
            }
            return View(driver);
        }

        [HttpPost, ActionName("DeleteDriver")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteDriverConfirmed(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM drivers WHERE driverid = @DriverId";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DriverId", id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = "<script>alert('Error deleting driver: " + ex.Message + "')</script>";
            }
            return RedirectToAction("ViewDriver");
        }

        public ActionResult ViewBookings()
        {
            var bookings = _context.bookings.ToList();
            return View(bookings);
        }




    }
}
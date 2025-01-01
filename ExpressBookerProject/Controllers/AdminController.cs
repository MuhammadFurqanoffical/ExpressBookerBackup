using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Web.Mvc;
using ExpressBookerProject.Utilities; // For AdminSessionAuthorize
using System.Data.Entity;
using System.Text.RegularExpressions;
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
            // Get the list of drivers that are not already assigned to a bus
            var assignedDriverIds = _context.buses
                                            .Where(b => b.driverid > 0)  // Only consider buses that have a driver assigned (assuming driverid > 0 means assigned)
                                             .Select(b => b.driverid)  // Get the list of driver IDs already assigned
                                             .ToList();

            // Get all drivers except the ones that are assigned to a bus
            var availableDrivers = _context.drivers
                                           .Where(d => !assignedDriverIds.Contains(d.driverid))  // Exclude assigned drivers
                                           .ToList();

            // Populate the dropdown list with available drivers
            ViewBag.DriverList = availableDrivers.Select(d => new SelectListItem
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
            // Check if the bus number already exists in the database
            var existingBus = _context.buses
                                      .FirstOrDefault(b => b.busnumber == model.busnumber);

            if (existingBus != null)
            {
                // Add a custom error message to the ModelState
                ModelState.AddModelError("busnumber", "A bus with this number already exists.");
            }

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

            // Get the list of drivers that are not already assigned to a bus
            var assignedDriverIds = _context.buses
                                            .Where(b => b.driverid > 0) // Only consider buses that have a driver assigned
                                            .Select(b => b.driverid)   // Get the list of driver IDs already assigned
                                            .ToList();

            // Get all drivers except the ones that are assigned to a bus
            var availableDrivers = _context.drivers
                                           .Where(d => !assignedDriverIds.Contains(d.driverid))  // Exclude assigned drivers
                                           .ToList();

            // Populate the dropdown list with available drivers
            ViewBag.DriverList = availableDrivers.Select(d => new SelectListItem
            {
                Value = d.driverid.ToString(),
                Text = d.name
            }).ToList();

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

            // Repopulate the dropdown list if model state is invalid
            var assignedDriverIds = _context.buses
                                            .Where(b => b.driverid > 0) // Only consider buses that have a driver assigned
                                            .Select(b => b.driverid)   // Get the list of driver IDs already assigned
                                            .ToList();

            var availableDrivers = _context.drivers
                                           .Where(d => !assignedDriverIds.Contains(d.driverid))  // Exclude assigned drivers
                                           .ToList();

            ViewBag.DriverList = availableDrivers.Select(d => new SelectListItem
            {
                Value = d.driverid.ToString(),
                Text = d.name
            }).ToList();

            return View(model);
        }


        // DeleteBus action (GET)
        public ActionResult DeleteBus(int id)
        {
            // Find the bus by id
            var bus = _context.buses.Find(id);

            if (bus != null)
            {
                // Check if the bus is being used in any bookings
                var isBusInUse = _context.bookings.Any(b => b.busid == bus.busid);

                if (isBusInUse)
                {
                    // If the bus is in use, show an error message in TempData
                    TempData["Message"] = "<script>alert('Cannot delete this bus because it is being used in bookings. Please remove the bookings first.')</script>";
                    return RedirectToAction("ViewBus");
                }

                // If the bus is not in use, proceed to delete
                _context.buses.Remove(bus);
                _context.SaveChanges();
                TempData["Message"] = "<script>alert('Bus deleted successfully.')</script>";
            }
            else
            {
                // If the bus is not found, show a message
                TempData["Message"] = "<script>alert('Bus not found.')</script>";
            }

            // After deletion or error, return to the list of buses
            return RedirectToAction("ViewBus");
        }

        [HttpPost, ActionName("DeleteBus")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBusConfirmed(int id)
        {
            try
            {
                // Find the bus by id
                var bus = _context.buses.Find(id);

                if (bus != null)
                {
                    // Check if the bus is being used in any bookings
                    var isBusInUse = _context.bookings.Any(b => b.busid == bus.busid);

                    if (isBusInUse)
                    {
                        // If the bus is in use, show an error message in TempData
                        TempData["Message"] = "<script>alert('Cannot delete this bus because it is being used in bookings. Please remove the bookings first.')</script>";
                        return RedirectToAction("ViewBus");
                    }

                    // If the bus is not in use, proceed to delete
                    _context.buses.Remove(bus);
                    _context.SaveChanges();
                    TempData["Message"] = "<script>alert('Bus deleted successfully.')</script>";
                }
                else
                {
                    // If the bus is not found, show a message
                    TempData["Message"] = "<script>alert('Bus not found.')</script>";
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, show the error message
                TempData["Message"] = "<script>alert('Error deleting bus: " + ex.Message + "')</script>";
            }

            // After deletion or error, return to the list of buses
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
                // Server-side validation to ensure distance is not negative or zero
                if (model.distance <= 0)
                {
                    ModelState.AddModelError("distance", "Distance must be greater than 0.");
                    return View(model); // Return the view with validation message
                }

                // Ensure that the distance does not exceed 9999.99
                if (model.distance > 9999)
                {
                    ModelState.AddModelError("distance", "Distance cannot exceed 9999.");
                    return View(model); // Return the view with validation message
                }

                // Validate source and destination to only allow words (no numbers or special characters)
                if (!Regex.IsMatch(model.source, @"^[a-zA-Z\s]+$"))
                {
                    ModelState.AddModelError("source", "Source must only contain letters and spaces.");
                    return View(model);
                }

                if (!Regex.IsMatch(model.destination, @"^[a-zA-Z\s]+$"))
                {
                    ModelState.AddModelError("destination", "Destination must only contain letters and spaces.");
                    return View(model);
                }

                // Check if the route with the same source and destination already exists
                var existingRoute = _context.routes
                    .FirstOrDefault(r => r.source == model.source && r.destination == model.destination);
                if (existingRoute != null)
                {
                    ModelState.AddModelError("source", "A route between the same source and destination already exists.");
                    return View(model); // Return the view with validation message
                }

                try
                {
                    // Define the SQL insert command for the new route
                    string sqlQuery = "INSERT INTO routes (source, destination, distance) VALUES (@Source, @Destination, @Distance)";

                    // Create a new SqlConnection by casting the DbConnection to SqlConnection
                    using (var command = new SqlCommand(sqlQuery, (SqlConnection)_context.Database.Connection))
                    {
                        // Add parameters to prevent SQL injection
                        command.Parameters.AddWithValue("@Source", model.source);
                        command.Parameters.AddWithValue("@Destination", model.destination);
                        command.Parameters.AddWithValue("@Distance", model.distance);

                        // Open the database connection
                        _context.Database.Connection.Open();

                        // Execute the insert command
                        command.ExecuteNonQuery();
                    }

                    // Close the database connection
                    _context.Database.Connection.Close();

                    // Redirect to ViewRoute after successful addition
                    return RedirectToAction("ViewRoute");
                }
                catch (Exception e)
                {
                    TempData["Message"] = "<script>alert('Failed to add route: " + e.Message + "')</script>";
                }
            }
            return View(model); // Return the view with validation errors if any
        }



        public ActionResult ViewRoute()
        {
            var routes = _context.routes.ToList();
            return View(routes);
        }

        public ActionResult EditRoute(int id)
        {
            // Find the route by ID
            var route = _context.routes.Find(id);
            if (route == null)
            {
                return HttpNotFound();
            }
            return View(route); // Return the route to the view for editing
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoute(route model)
        {
            if (ModelState.IsValid)
            {
                // Server-side validation to ensure distance is not negative
                if (model.distance <= 0)
                {
                    ModelState.AddModelError("distance", "Distance must be greater than 0.");
                    return View(model);  // Return the view with validation message
                }

                // Ensure that the distance does not exceed 9999.99
                if (model.distance > 9999)
                {
                    ModelState.AddModelError("distance", "Distance cannot exceed 9999.");
                    return View(model); // Return the view with validation message
                }

                // Validate source and destination to only allow words (no numbers or special characters)
                if (!Regex.IsMatch(model.source, @"^[a-zA-Z\s]+$"))
                {
                    ModelState.AddModelError("source", "Source must only contain letters and spaces.");
                    return View(model);
                }

                if (!Regex.IsMatch(model.destination, @"^[a-zA-Z\s]+$"))
                {
                    ModelState.AddModelError("destination", "Destination must only contain letters and spaces.");
                    return View(model);
                }

                // Check if the route with the same source and destination already exists (excluding the current route being edited)
                var existingRoute = _context.routes
                    .FirstOrDefault(r => r.source == model.source && r.destination == model.destination && r.routeid != model.routeid);
                if (existingRoute != null)
                {
                    ModelState.AddModelError("source", "A route between the same source and destination already exists.");
                    return View(model); // Return the view with validation message
                }

                try
                {
                    // Define the SQL update command for the route
                    string sqlQuery = "UPDATE routes SET source = @Source, destination = @Destination, distance = @Distance WHERE routeid = @RouteId";

                    // Create a new SqlConnection by casting the DbConnection to SqlConnection
                    using (var command = new SqlCommand(sqlQuery, (SqlConnection)_context.Database.Connection))
                    {
                        // Add parameters to prevent SQL injection
                        command.Parameters.AddWithValue("@Source", model.source);
                        command.Parameters.AddWithValue("@Destination", model.destination);
                        command.Parameters.AddWithValue("@Distance", model.distance);
                        command.Parameters.AddWithValue("@RouteId", model.routeid);

                        // Open the database connection
                        _context.Database.Connection.Open();

                        // Execute the update command
                        command.ExecuteNonQuery();
                    }

                    // Close the database connection
                    _context.Database.Connection.Close();

                    // Redirect to ViewRoute after successful update
                    return RedirectToAction("ViewRoute");
                }
                catch (Exception e)
                {
                    TempData["Message"] = "<script>alert('Failed to update route: " + e.Message + "')</script>";
                }
            }
            return View(model); // Return the view with validation errors if any
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
            // Get the list of bus IDs that are already assigned to a schedule
            var assignedBusIds = _context.busschedules.Select(bs => bs.busid).ToList();

            // Filter out buses that are already assigned to a schedule
            var availableBuses = _context.buses.Where(b => !assignedBusIds.Contains(b.busid)).ToList();

            // Pass the filtered list to the ViewBag
            ViewBag.BusList = new SelectList(availableBuses, "busid", "busnumber");
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

                // Check if the bus is already assigned to another schedule at the same time
                var existingSchedule = _context.busschedules
                    .FirstOrDefault(s => s.busid == model.busid &&
                                         ((model.departuretime >= s.departuretime && model.departuretime < s.arrivaltime) ||
                                          (model.arrivaltime > s.departuretime && model.arrivaltime <= s.arrivaltime)));

                if (existingSchedule != null)
                {
                    TempData["Message"] = "<script>alert('This bus is already assigned to another schedule during the selected time slot.')</script>";
                    ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
                    ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
                    {
                        routeid = r.routeid,
                        RouteDisplay = r.source + " - " + r.destination
                    }), "routeid", "RouteDisplay", model.routeid);
                    return View(model);
                }

                // If no conflict, add the bus schedule
                _context.busschedules.Add(model);
                _context.SaveChanges();
                return RedirectToAction("ViewBusSchedule");
            }

            // Reload dropdowns in case of validation failure
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

            // Get the list of bus IDs that are already assigned to a schedule
            var assignedBusIds = _context.busschedules
                                        .Where(bs => bs.scheduleid != id) // Exclude the current schedule being edited
                                        .Select(bs => bs.busid)
                                        .ToList();

            // Filter out buses that are already assigned to a schedule, excluding the current bus being edited
            var availableBuses = _context.buses.Where(b => !assignedBusIds.Contains(b.busid) || b.busid == busSchedule.busid).ToList();

            // Pass the filtered list to the ViewBag
            ViewBag.BusList = new SelectList(availableBuses, "busid", "busnumber", busSchedule.busid);
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

                // Check if the bus is already assigned to another schedule at the same time
                var existingSchedule = _context.busschedules
                    .FirstOrDefault(s => s.busid == model.busid &&
                                         s.scheduleid != model.scheduleid && // Exclude the current schedule being edited
                                         ((model.departuretime >= s.departuretime && model.departuretime < s.arrivaltime) ||
                                          (model.arrivaltime > s.departuretime && model.arrivaltime <= s.arrivaltime)));

                if (existingSchedule != null)
                {
                    TempData["Message"] = "<script>alert('This bus is already assigned to another schedule during the selected time slot.')</script>";
                    ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
                    ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
                    {
                        routeid = r.routeid,
                        RouteDisplay = r.source + " - " + r.destination
                    }), "routeid", "RouteDisplay", model.routeid);
                    return View(model);  // Return the view with error message
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

            // Reload dropdowns in case of validation failure
            ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
            ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
            {
                routeid = r.routeid,
                RouteDisplay = r.source + " - " + r.destination
            }), "routeid", "RouteDisplay", model.routeid);

            return View(model);
        }




        // DeleteBusSchedule actions
        // DeleteBusSchedule actions
        public ActionResult DeleteBusSchedule(int id)
        {
            var busSchedule = _context.busschedules.Find(id);
            if (busSchedule == null)
            {
                return HttpNotFound();
            }

            // Check if the bus schedule has any associated bookings
            var bookings = _context.bookings.Where(b => b.bookingid == id).ToList();
            if (bookings.Any())
            {
                // If there are bookings, don't allow deletion and show a message
                TempData["Message"] = "<script>alert('This bus schedule cannot be deleted because there are active bookings associated with it. Please cancel the bookings first.')</script>";
                return RedirectToAction("ViewBusSchedule");
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
                    // Check if there are any bookings before deleting
                    var bookings = _context.bookings.Where(b => b.bookingid == id).ToList();
                    if (bookings.Any())
                    {
                        // If bookings exist, prevent deletion and show message
                        TempData["Message"] = "<script>alert('This bus schedule cannot be deleted because there are active bookings associated with it. Please cancel the bookings first.')</script>";
                        return RedirectToAction("ViewBusSchedule");
                    }

                    // If no bookings exist, proceed with deletion
                    _context.busschedules.Remove(busSchedule);
                    _context.SaveChanges();
                    TempData["Message"] = "<script>alert('Bus schedule deleted successfully!')</script>";
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
                // Validate Phone Number Length (must be 11 digits)
                if (model.phone.Length != 11)
                {
                    TempData["Message"] = "<script>alert('Phone number must be 11 digits.')</script>";
                    return View(model); // Return if phone length is not valid
                }

                // Validate that Phone Number contains only digits
                if (!model.phone.All(char.IsDigit))
                {
                    TempData["Message"] = "<script>alert('Phone number must contain only digits.')</script>";
                    return View(model); // Return if phone contains non-digit characters
                }

                // Validate License Number Length (assume 15 characters for example)
                if (model.licensenumber.Length != 15)
                {
                    TempData["Message"] = "<script>alert('License number must be 15 characters long.')</script>";
                    return View(model); // Return if license number length is not valid
                }

                // Validate that License Number contains only letters and digits
                if (!model.licensenumber.All(c => char.IsLetterOrDigit(c)))
                {
                    TempData["Message"] = "<script>alert('License number must contain only letters and digits.')</script>";
                    return View(model); // Return if license number contains invalid characters
                }

                try
                {
                    // Check if there is an existing driver with the same license number or phone number
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        string checkQuery = "SELECT COUNT(*) FROM drivers WHERE licensenumber = @LicenseNumber OR phone = @Phone";
                        SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                        checkCmd.Parameters.AddWithValue("@LicenseNumber", model.licensenumber);
                        checkCmd.Parameters.AddWithValue("@Phone", model.phone);
                        conn.Open();
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            TempData["Message"] = "<script>alert('Driver with the same license number or phone number already exists.')</script>";
                            return View(model); // Return the view if duplicate found
                        }

                        // Insert the new driver if no duplicates
                        string query = "INSERT INTO drivers (name, licensenumber, phone) VALUES (@Name, @LicenseNumber, @Phone)";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Name", model.name);
                        cmd.Parameters.AddWithValue("@LicenseNumber", model.licensenumber);
                        cmd.Parameters.AddWithValue("@Phone", model.phone);
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
                // Validate Phone Number Length (must be 11 digits)
                if (model.phone.Length != 11)
                {
                    TempData["Message"] = "<script>alert('Phone number must be 11 digits.')</script>";
                    return View(model); // Return if phone length is not valid
                }

                // Validate that Phone Number contains only digits
                if (!model.phone.All(char.IsDigit))
                {
                    TempData["Message"] = "<script>alert('Phone number must contain only digits.')</script>";
                    return View(model); // Return if phone contains non-digit characters
                }

                // Validate License Number Length (assume 15 characters for example)
                if (model.licensenumber.Length != 15)
                {
                    TempData["Message"] = "<script>alert('License number must be 15 characters long.')</script>";
                    return View(model); // Return if license number length is not valid
                }

                // Validate that License Number contains only letters and digits
                if (!model.licensenumber.All(c => char.IsLetterOrDigit(c)))
                {
                    TempData["Message"] = "<script>alert('License number must contain only letters and digits.')</script>";
                    return View(model); // Return if license number contains invalid characters
                }

                try
                {
                    // Check for existing driver with the same license number or phone number (excluding current driver)
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        string checkQuery = "SELECT COUNT(*) FROM drivers WHERE (licensenumber = @LicenseNumber OR phone = @Phone) AND driverid != @DriverId";
                        SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                        checkCmd.Parameters.AddWithValue("@LicenseNumber", model.licensenumber);
                        checkCmd.Parameters.AddWithValue("@Phone", model.phone);
                        checkCmd.Parameters.AddWithValue("@DriverId", model.driverid);
                        conn.Open();
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            TempData["Message"] = "<script>alert('Driver with the same license number or phone number already exists.')</script>";
                            return View(model); // Return the view if duplicate found
                        }

                        // Update the driver if no duplicates
                        string query = "UPDATE drivers SET name = @Name, licensenumber = @LicenseNumber, phone = @Phone WHERE driverid = @DriverId";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Name", model.name);
                        cmd.Parameters.AddWithValue("@LicenseNumber", model.licensenumber);
                        cmd.Parameters.AddWithValue("@Phone", model.phone);
                        cmd.Parameters.AddWithValue("@DriverId", model.driverid);
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

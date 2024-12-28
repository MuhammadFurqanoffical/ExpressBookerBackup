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
    [AdminSessionAuthorize]
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

        public ActionResult AdminDashboard() => View();

        // Bus Management
        public ActionResult AddBus()
        {
            ViewBag.DriverList = GetDriverSelectList();
            ViewBag.CapacityList = GetCapacitySelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBus(bus model)
        {
            if (ModelState.IsValid)
            {
                _context.buses.Add(model);
                _context.SaveChanges();
                return RedirectToAction("ViewBus");
            }

            PopulateBusDropdowns();
            return View(model);
        }

        public ActionResult ViewBus() => View(_context.buses.ToList());

        public ActionResult EditBus(int id)
        {
            var bus = _context.buses.Find(id);
            if (bus == null) return HttpNotFound();

            PopulateBusDropdowns(bus.driverid);
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
                    TempData["Message"] = GenerateAlertMessage("Data not updated", e);
                }
            }

            PopulateBusDropdowns(model.driverid);
            return View(model);
        }

        public ActionResult DeleteBus(int id)
        {
            var bus = _context.buses.Find(id);
            if (bus == null) return HttpNotFound();

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
                    TempData["Message"] = GenerateAlertMessage("Data not found");
                }
            }
            catch (Exception e)
            {
                TempData["Message"] = GenerateAlertMessage("Data not deleted", e);
            }

            return RedirectToAction("ViewBus");
        }

        public ActionResult AddRoute() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRoute(route model)
        {
            if (ModelState.IsValid)
            {
                // Validate the route distance
                if (model.distance > 9999)
                {
                    TempData["Message"] = "The distance cannot exceed 9999 km.";
                    return View(model); // Return to the view if distance exceeds 1000
                }

                if (model.distance <= 0)
                {
                    TempData["Message"] = "The distance must be a positive value.";
                    return View(model); // Return to the view if distance is negative or zero
                }

                try
                {
                    // Using Ado.Net (SqlConnection) to manually insert the route
                    string query = "INSERT INTO routes (source, destination, distance) VALUES (@Source, @Destination, @Distance)";

                    using (var conn = new SqlConnection(_connectionString))
                    {
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Source", model.source);
                        cmd.Parameters.AddWithValue("@Destination", model.destination);
                        cmd.Parameters.AddWithValue("@Distance", model.distance);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    return RedirectToAction("ViewRoute");
                }
                catch (Exception e)
                {
                    TempData["Message"] = $"Failed to add route: {e.Message}";
                }
            }

            return View(model);
        }


        public ActionResult ViewRoute() => View(_context.routes.ToList());

        public ActionResult EditRoute(int id)
        {
            var route = _context.routes.Find(id);
            if (route == null) return HttpNotFound();

            return View(route);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoute(route model)
        {
            if (ModelState.IsValid)
            {
                // Validate the route distance
                if (model.distance > 9999)
                {
                    TempData["Message"] = "The distance cannot exceed 9999 km.";
                    return View(model); // Return to the view if distance exceeds 1000
                }

                if (model.distance <= 0)
                {
                    TempData["Message"] = "The distance must be a positive value.";
                    return View(model); // Return to the view if distance is negative or zero
                }

                try
                {
                    // Using Ado.Net (SqlConnection) to manually update the route
                    string query = "UPDATE routes SET source = @Source, destination = @Destination, distance = @Distance WHERE routeid = @RouteId";

                    using (var conn = new SqlConnection(_connectionString))
                    {
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@Source", model.source);
                        cmd.Parameters.AddWithValue("@Destination", model.destination);
                        cmd.Parameters.AddWithValue("@Distance", model.distance);
                        cmd.Parameters.AddWithValue("@RouteId", model.routeid);

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }

                    return RedirectToAction("ViewRoute");
                }
                catch (Exception e)
                {
                    TempData["Message"] = $"Failed to update route: {e.Message}";
                }
            }

            return View(model);
        }

        public ActionResult DeleteRoute(int id)
        {
            var route = _context.routes.Find(id);
            if (route == null) return HttpNotFound();

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
                    TempData["Message"] = GenerateAlertMessage("Route not found");
                }
                else if (_context.busschedules.Any(bs => bs.routeid == route.routeid))
                {
                    TempData["Message"] = GenerateAlertMessage("This route is currently used in a bus schedule. Please remove it from the schedule first.");
                }
                else
                {
                    _context.routes.Remove(route);
                    _context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                TempData["Message"] = GenerateAlertMessage("Failed to delete route", e);
            }

            return RedirectToAction("ViewRoute");
        }

        // ViewBusSchedule action
        public ActionResult ViewBusSchedule()
        {
            try
            {
                var busSchedules = _context.busschedules
                                            .Include(b => b.bus)
                                            .Include(r => r.route)
                                            .ToList();

                return View(busSchedules);
            }
            catch (Exception e)
            {
                TempData["Message"] = $"<script>alert('Error loading bus schedules: {e.Message}')</script>";
                return RedirectToAction("AddBusSchedule");  
            }
        }

        // AddBusSchedule actions
        public ActionResult AddBusSchedule()
        {
            try
            {
                ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber");
                ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
                {
                    routeid = r.routeid,
                    RouteDisplay = r.source + " - " + r.destination
                }), "routeid", "RouteDisplay");
            }
            catch (Exception e)
            {
                TempData["Message"] = $"<script>alert('Error loading data: {e.Message}')</script>";
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBusSchedule(busschedule model)
        {
            if (ModelState.IsValid)
            {
                try
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
                catch (Exception e)
                {
                    TempData["Message"] = $"<script>alert('Error saving bus schedule: {e.Message}')</script>";
                }
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
            try
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
            catch (Exception e)
            {
                TempData["Message"] = $"<script>alert('Error loading bus schedule: {e.Message}')</script>";
                return RedirectToAction("ViewBusSchedule");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditBusSchedule(busschedule model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Server-side validation to ensure arrival time is after departure time
                    if (model.arrivaltime <= model.departuretime)
                    {
                        ModelState.AddModelError("", "Arrival time must be after departure time.");

                        ViewBag.BusList = new SelectList(_context.buses, "busid", "busnumber", model.busid);
                        ViewBag.RouteList = new SelectList(_context.routes.Select(r => new
                        {
                            routeid = r.routeid,
                            RouteDisplay = r.source + " - " + r.destination
                        }), "routeid", "RouteDisplay", model.routeid);

                        return View(model);  // Return the view with the error message
                    }

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
                TempData["Message"] = "Bus schedule not found.";
            }
        }
        catch (Exception e)
        {
            TempData["Message"] = $"Bus schedule not deleted: {e.Message}";
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
                const string query = "SELECT * FROM drivers";
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
            TempData["Message"] = $"Error loading drivers: {ex.Message}";
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
                    const string query = "INSERT INTO drivers (name, licensenumber, phone) VALUES (@Name, @LicenseNumber, @Phone)";
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
                TempData["Message"] = $"Error adding driver: {ex.Message}";
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
                const string query = "SELECT * FROM drivers WHERE driverid = @DriverId";
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
            TempData["Message"] = $"Error loading driver details: {ex.Message}";
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
                    const string query = "UPDATE drivers SET name = @Name, licensenumber = @LicenseNumber, phone = @Phone WHERE driverid = @DriverId";
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
                TempData["Message"] = $"Error updating driver: {ex.Message}";
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
                const string query = "SELECT * FROM drivers WHERE driverid = @DriverId";
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
            TempData["Message"] = $"Error loading driver details: {ex.Message}";
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
                const string query = "DELETE FROM drivers WHERE driverid = @DriverId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DriverId", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            TempData["Message"] = $"Error deleting driver: {ex.Message}";
        }
        return RedirectToAction("ViewDriver");
    }

    public ActionResult ViewBookings()
    {
        var bookings = _context.bookings.ToList();
        return View(bookings);
    }
        // Utility Methods
        private IEnumerable<SelectListItem> GetDriverSelectList() =>
            _context.drivers.Select(d => new SelectListItem
            {
                Value = d.driverid.ToString(),
                Text = d.name
            }).ToList();

        private IEnumerable<SelectListItem> GetCapacitySelectList() =>
            Enumerable.Range(20, 11).Select(c => new SelectListItem
            {
                Value = c.ToString(),
                Text = c.ToString()
            });

        private void PopulateBusDropdowns(int? selectedDriverId = null)
        {
            ViewBag.DriverList = new SelectList(GetDriverSelectList(), "Value", "Text", selectedDriverId);
            ViewBag.CapacityList = GetCapacitySelectList();
        }

        private bool ValidateRouteDistance(route model)
        {
            if (model.distance < 0)
            {
                ModelState.AddModelError("distance", "Distance cannot be negative.");
                return false;
            }
            return true;
        }

        private string GenerateAlertMessage(string message, Exception exception = null) =>
            $"<script>alert('{message}{(exception != null ? ": " + exception.Message : string.Empty)}')</script>";
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ExpressBookerProject.Utilities
{
    public class AdminSessionManager
    {
        // Private constructor to prevent instantiation from outside
        private AdminSessionManager() { }

        // Public static method to set the current admin session
        public static bool SetCurrentAdminSession(AdminSession session)
        {
            // Store the session data in HttpContext.Current.Session
            if (HttpContext.Current.Session["AdminSession"] == null)
            {
                HttpContext.Current.Session["AdminSession"] = session;
                return true;
            }
            return false;
        }

        // Public static method to clear the current admin session
        public static void ClearCurrentAdminSession()
        {
            // Clear the session data in HttpContext.Current.Session
            HttpContext.Current.Session["AdminSession"] = null;
        }

        // Public static method to get the current admin session
        public static AdminSession GetCurrentAdminSession()
        {
            // Retrieve the session data from HttpContext.Current.Session
            return HttpContext.Current.Session["AdminSession"] as AdminSession;
        }
    }
}

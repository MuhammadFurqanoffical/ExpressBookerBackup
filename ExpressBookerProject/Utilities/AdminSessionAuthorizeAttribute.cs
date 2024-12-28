using System;
using System.Web;
using System.Web.Mvc;
using ExpressBookerProject.Utilities; // Reference to AdminSessionManager

public class AdminSessionAuthorizeAttribute : AuthorizeAttribute
{
    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        // Check if the admin session is active
        var adminSession = AdminSessionManager.GetCurrentAdminSession();
        return adminSession != null; // Allow only if an admin session exists
    }

    protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
    {
        // Redirect to the login page if unauthorized
        filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary
        {
            { "controller", "Account" },
            { "action", "Login" }
        });
    }
}

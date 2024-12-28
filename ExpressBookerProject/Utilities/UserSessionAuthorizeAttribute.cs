using System;
using System.Web;
using System.Collections.Generic;
using System.Web.Mvc;

namespace ExpressBookerProject.Utilities
{
    public class UserSessionAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Check if the UserID session is active
            return httpContext.Session["UserID"] != null;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Redirect to the User login page if unauthorized
            filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary
            {
                { "controller", "User" },
                { "action", "Login" }
            });
        }
    }
}

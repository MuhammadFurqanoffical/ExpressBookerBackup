using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ExpressBookerProject.Controllers;
using ExpressBookerProject.Services;
using ExpressBookerProject.Models;
using ExpressBookerProject.Utilities;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace UnitTesting
{
    [TestClass]
    public class UnitTest1
    {
        private Mock<BookingFacade> _mockFacade;
        private UserController _controller;

        public UnitTest1()
        {
            _mockFacade = new Mock<BookingFacade>();  // Your mock setup
            _controller = new UserController(_mockFacade.Object);  // Your controller initialization
        }

        [TestMethod]
        public void Login_Get_Returns_View()
        {
            // Arrange
            var result = _controller.Login();

            // Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        public void Login_Post_ValidUser_ReturnsExpectedResult()
        {
            // Arrange
            var model = new user { username = "ali", password = "alipass" };

            // Mock the facade to return a valid user when AuthenticateUser is called
            _mockFacade.Setup(f => f.AuthenticateUser(model.username, model.password))
                       .Returns(new user { userid = 14, roleid = 2 }); // Mock a valid user

            // Mock HttpContext and Session if required (just for completeness)
            var mockHttpContext = new Mock<HttpContextBase>();
            var mockSession = new Mock<HttpSessionStateBase>();
            mockHttpContext.Setup(c => c.Session).Returns(mockSession.Object);

            // Setting up the controller context
            _controller.ControllerContext = new ControllerContext(mockHttpContext.Object, new RouteData(), _controller);

            // Ensure that ModelState is valid for the test
            _controller.ModelState.Clear();  // Clear any potential ModelState errors for a valid model

            // Act
            var result = _controller.Login(model) as RedirectToRouteResult;

            // Assert
            Assert.IsNotNull(result);  // Ensure that the result is not null (should not be null if redirected)
            Assert.AreEqual("BusSchedule", result.RouteValues["action"]);  // Check that it redirects to BusSchedule
        }





        [TestMethod]
        public void Login_Post_InvalidUser_ReturnsViewWithErrorMessage()
        {
            // Arrange
            var model = new user { username = "invalidUser", password = "wrongPassword" };

            // Set up the mock for AuthenticateUser to return null for invalid credentials
            _mockFacade.Setup(f => f.AuthenticateUser(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns((user)null); // Invalid user should return null

            // Act
            var result = _controller.Login(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);  // Ensure a ViewResult is returned
            Assert.AreEqual(model, result.Model);  // Ensure the model is passed back to the view
            Assert.AreEqual("Invalid username or password.", result.ViewBag.ErrorMessage);  // Verify the error message
        }



        [TestMethod]
        public void BookSeats_Get_ReturnsViewWithAvailableSeats()
        {
            // Arrange
            var scheduleId = 18;
            var mockSchedule = new busschedule { scheduleid = scheduleId, bus = new bus { busnumber = "Daewoo Express 1" } };
            _mockFacade.Setup(f => f.GetBusSchedule(scheduleId)).Returns(mockSchedule);
            _mockFacade.Setup(f => f.GetAvailableSeats(mockSchedule)).Returns(2); // 10 seats available

            // Act
            var result = _controller.BookSeats(scheduleId) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.ViewData["AvailableSeats"]);
        }

        [TestMethod]
        public void BookSeats_Post_SeatsBookedSuccessfully_ReturnsConfirmBookingView()
        {
            // Arrange
            var scheduleId = 18;
            var seats = 3;

            // Mock the bus and route
            var mockBus = new bus { busnumber = "Daewoo Express 1", capacity = 20 }; // Set capacity to 20
            var mockRoute = new route { distance = 100 }; // Mock the route with a distance of 100 km
            var mockSchedule = new busschedule
            {
                scheduleid = scheduleId,
                bus = mockBus,
                route = mockRoute
            };

            // Setup the facade methods
            _mockFacade.Setup(f => f.GetBusSchedule(scheduleId)).Returns(mockSchedule);
            _mockFacade.Setup(f => f.GetAvailableSeats(mockSchedule)).Returns(10); // Available seats = 10
            _mockFacade.Setup(f => f.CalculatePrice(mockSchedule)).Returns(100m); // Price for 100 km

            // Act
            var result = _controller.BookSeats(scheduleId, seats) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("ConfirmBooking", result.ViewName);
        }


       
    }
}

using Lowballers.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LowBallersTest
{
    [TestClass]
    public class HomeControllerTest
    {
        [TestMethod]
        public void IndexViewLoads()
        {
            //arrange
            var homeController = new HomeController();

            //act - cast IActionResult return object as a ViewResult
            var result = (ViewResult) homeController.Index();

            //assert
            Assert.AreEqual("Index", result.ViewName);
        }

        [TestMethod]
        public void PrivacyViewLoads()
        {
            //arrange
            var homeController = new HomeController();
            //act
            var result = (ViewResult)homeController.Privacy();

            //assert
            Assert.AreEqual("Privacy", result.ViewName);
        }
    }
}

using Lowballers.Controllers;
using Lowballers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LowBallersTest
{
    [TestClass]

    public class ProductsControllerTest
    {

        //dbcontext in memory for dependency injection
        private LowballersContext _context;
        ProductsController productsController;
        List<Products> products = new List<Products>();

        //adding this annotation automatically runs this method before every test
        [TestInitialize]
        public void TestInitialize()
        {
            //create in-memory context and inject to controller instance
            var options = new DbContextOptionsBuilder<LowballersContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new LowballersContext(options);

            var category = new Categories
            {
                CategoryId = 100,
                Name = "Some Category"
            };

            //create mock data and add to in-memory database
            products.Add(new Products
            {
                ProductId = 200,
                Name = "Prod 1",
                Price = 12,
                Category = category
            });

            //create mock data and add to in-memory database
            products.Add(new Products
            {
                ProductId = 30,
                Name = "Prod 0",
                Price = 15,
                Category = category
            });

            //create mock data and add to in-memory database
            products.Add(new Products
            {
                ProductId = 29,
                Name = "Prod 3",
                Price = 55,
                Category = category
            });

            foreach(var p in products)
            {
                _context.Add(p);
            }

            _context.SaveChanges();

            //instantiate controller with mock data in the dependency
            productsController = new ProductsController(_context);
        }

        [TestMethod]
        public void IndexViewLoads()
        {
            //act
            var result = productsController.Index();
            //result.Wait();

            var viewResult = (ViewResult)result.Result;

            //assert
            Assert.AreEqual("Index", viewResult.ViewName);
        }

        [TestMethod]
        public void IndexLoadsProducts()
        {
            //act
            var result = productsController.Index();
            var viewResult = (ViewResult)result.Result;

            List<Products> model = (List<Products>)viewResult.Model;

            //assert
            CollectionAssert.AreEqual(products.OrderBy(p => p.Name).ToList(), model);
        }

        [TestMethod]
        public void DetailsNullId()
        {
            //act
            var result = productsController.Details(null).Result;

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void DetailsInvalidId()
        {
            //act
            var result = productsController.Details(23).Result;

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public void DetailsValidIdLoadsView()
        {
            //act
            var result = (ViewResult)productsController.Details(29).Result;


            Assert.AreEqual("Details", result.ViewName);
        }

        [TestMethod]
        public void DetailsIsValidIdLoadsProduct()
        {
            //act
            var result = (ViewResult)productsController.Details(30).Result;
            var model = (Products)result.Model;

            //assert
            Assert.AreEqual(products[1], model);
        }
    }
}

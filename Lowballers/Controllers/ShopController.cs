using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lowballers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lowballers.Controllers
{
    public class ShopController : Controller
    {
        //add db connection
        private readonly LowballersContext _context;

        //constructor that accepts an instance of our dbcontext
        public ShopController(LowballersContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // return the list of categories to the view
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            return View(categories);
        }

        public IActionResult Browse(string category)
        {
            //Store category name in the ViewBag to display on the page heading
            ViewBag.category = category;
            //get products a-z from selected category & pass to the view
            var products = _context.Products.Where(p => p.Category.Name == category).OrderBy(p => p.Name).ToList();
            return View(products);
        }

        public IActionResult ProductDetails(string product)
        {
            //get selected product and pass to the view
            var selectedProduct = _context.Products.SingleOrDefault(p => p.Name == product);
            return View(selectedProduct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int Quantity, int ProductId)
        {
            //get the current product price from the db
            var product = _context.Products.SingleOrDefault(p => p.ProductId == ProductId);
            var price = product.Price;

            //set (if not set) then get cart username
            var cartUsername = GetCartUsername();

            //create and save a new cart object
            var cart = new Carts
            {
                Quantity = Quantity,
                ProductId = ProductId,
                Price = price,
                Username = cartUsername
            };

            _context.Carts.Add(cart);
            _context.SaveChanges();

            //redirect to Cart page
            return RedirectToAction("Cart");
        }

        private string GetCartUsername()
        {
            //check if user already has a cart in the session object
            if(HttpContext.Session.GetString("CartUsername") == null)
            {
                var cartUsername = "";

                //is user logged in or anonymous?
                if(User.Identity.IsAuthenticated)
                {
                    cartUsername = User.Identity.Name;
                } else
                {
                    //generate a random string as the username for now
                    cartUsername = Guid.NewGuid().ToString();
                }

                //store the cartUsername in a session variable
                HttpContext.Session.SetString("CartUsername", cartUsername);
            }

            return HttpContext.Session.GetString("CartUsername");
        }

        public IActionResult Cart()
        {
            //get Current Cart Username from session object
            var cartUsername = HttpContext.Session.GetString("CartUsername");
            //get current user's shopping cart items and pass to the view
            var cartItems = _context.Carts.Include(p => p.Product).Where(c => c.Username == cartUsername).ToList();

            HttpContext.Session.SetString("CartCount", cartItems.Count.ToString());
            return View(cartItems);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lowballers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Lowballers.Controllers
{
    public class ShopController : Controller
    {
        //add db connection
        private readonly LowballersContext _context;

        //add configuration through dependency injection
        private IConfiguration _iconfiguration;

        //constructor that accepts an instance of our dbcontext
        public ShopController(LowballersContext context, IConfiguration configuration)
        {
            _context = context;
            _iconfiguration = configuration;
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
            MigrateCart();
            //get Current Cart Username from session object
            var cartUsername = HttpContext.Session.GetString("CartUsername");

            //get current user's shopping cart items and pass to the view
            var cartItems = _context.Carts.Include(p => p.Product).Where(c => c.Username == cartUsername).ToList();

            HttpContext.Session.SetString("CartCount", cartItems.Count.ToString());
            return View(cartItems);
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cartItem = _context.Carts.SingleOrDefault(c => c.CartId == id);

            //delete and update the db so this cart record is gone
            _context.Carts.Remove(cartItem);
            _context.SaveChanges();

            //refresh the cart
            return RedirectToAction("Cart");
        }

        [Authorize]
        public IActionResult Checkout()
        {
            //check if the user has shopped anonymously; if so attach their email to their cart now
            MigrateCart();
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout([Bind("FirstName,LastName,Address,City,Province,PostalCode,Phone")] Orders order)
        {
            //auto fill the other 3 order properties
            order.OrderDate = DateTime.Now;
            order.UserId = User.Identity.Name;

            //calc order total
            var cartItems = _context.Carts.Where(c => c.Username == order.UserId).ToList();
            var orderTotal = (from c in cartItems
                              select c.Quantity * c.Price).Sum();

            order.Total = orderTotal;

            //use the SessionsExtensions class we found online to store the order in a session variable
            HttpContext.Session.SetObject("Order", order);
            return RedirectToAction("Payment");

        }

        [Authorize]
        public IActionResult Payment()
        {
            //read the object back out of the session and cast it to its original type
            var Order = HttpContext.Session.GetObject<Orders>("Order");

            var orderTotal = Order.Total;
            var centsTotal = orderTotal * 100;

            ViewBag.Total = orderTotal;
            ViewBag.CentsTotal = centsTotal;
            ViewBag.PublishableKey = _iconfiguration.GetSection("Stripe")["PublishableKey"];

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult Payment(string stripeEmail, string stripeToken)
        {
            //get secret key from configuration and pass to stripe API
            StripeConfiguration.ApiKey = _iconfiguration.GetSection("Stripe")["SecretKey"];
            var cartUsername = User.Identity.Name;
            var cartItems = _context.Carts.Where(c => c.Username == cartUsername).ToList();
            var order = HttpContext.Session.GetObject<Orders>("Order");

            //invoke stripe payment attempt
            var customerService = new Stripe.CustomerService();
            var charges = new Stripe.ChargeService();

            Stripe.Customer customer = customerService.Create(new Stripe.CustomerCreateOptions
            {
                Source = stripeToken,
                Email = stripeEmail
            });

            var charge = charges.Create(new Stripe.ChargeCreateOptions
            {
                Amount = Convert.ToInt32(order.Total * 100),
                Description = "Sample Charge",
                Currency = "cad",
                Customer = customer.Id
            });

            //save the order
            _context.Orders.Add(order);
            _context.SaveChanges();

            //save the order details
            foreach(var item in cartItems)
            {
                var orderDetail = new OrderDetails
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };

                _context.OrderDetails.Add(orderDetail);
            }

            _context.SaveChanges();

            //empty the cart
            foreach(var item in cartItems)
            {
                _context.Carts.Remove(item);
            }

            _context.SaveChanges();


            return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        }


        private void MigrateCart()
        {
            if(!User.Identity.IsAuthenticated)
            {
                return;
            }


            //if user has shopped anonymously, now attach items to their username rather than a GUID
            if(HttpContext.Session.GetString("CartUsername") != User.Identity.Name)
            {
                var cartUsername = HttpContext.Session.GetString("CartUsername");
                //find all the cart items and update the username to user's email
                var cartItems = _context.Carts.Where(c => c.Username == cartUsername).ToList();

                foreach(var item in cartItems)
                {
                    item.Username = User.Identity.Name;
                }

                _context.SaveChanges();

                //update session variable too
                HttpContext.Session.SetString("CartUsername", User.Identity.Name);
            }
        }
    }
}
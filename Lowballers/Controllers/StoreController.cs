using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lowballers.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Lowballers.Controllers
{
    public class StoreController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            var categories = new List<Category>();

            //create 10 mock categories
            for(int i = 1; i <= 10; i++)
            {
                categories.Add(new Category { Name = "Category " + i.ToString() });
            }

            //when returning the view, also pass the list of categories to the UI
            return View(categories);
        }

        public IActionResult Browse(string category)
        {
            //add selected category to ViewBag to display on the browse page
            ViewBag.category = category;
            return View();
        }
    }
}

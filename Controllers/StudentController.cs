using laborator1.Models;
using Microsoft.AspNetCore.Mvc;

namespace laborator1.Controllers
{
    public class StudentController : Controller
    {
        public IActionResult Info()
        {
            var student = new Student { Name = "Ana", Age = 21 };
            return View(student);
        }
    }
}

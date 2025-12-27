using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreOcrDemo.Controllers
{
    public class DefaultController : Controller
    {

        public DefaultController()
        {
        }



        public IActionResult Index()
        {
            return View();
        }

    }
}
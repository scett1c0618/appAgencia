using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;

namespace app1.Controllers
{
    public class HotelController : Controller
    {
        // El servicio de hoteles ha sido removido temporalmente
        public IActionResult Index()
        {
            return View();
        }
    }
}

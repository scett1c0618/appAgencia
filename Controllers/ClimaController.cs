using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app1.Servicios;

namespace app1.Controllers
{
    public class ClimaController : Controller
    {
        private readonly ClimaService _climaService;

        public ClimaController(ClimaService climaService)
        {
            _climaService = climaService;
        }

        public async Task<IActionResult> Index(string ciudad = "Lima")
        {
            var clima = await _climaService.ObtenerClimaAsync(ciudad);
            var pronostico = await _climaService.ObtenerPronosticoAsync(ciudad);
            ViewBag.Ciudad = ciudad;
            ViewBag.Clima = clima;
            ViewBag.Pronostico = pronostico;
            return View();
        }
    }
}

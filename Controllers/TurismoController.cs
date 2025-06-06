using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app1.Servicios;

namespace app1.Controllers
{
    public class TurismoController : Controller
    {
        private readonly WikipediaService _wikipediaService;
        public TurismoController(WikipediaService wikipediaService)
        {
            _wikipediaService = wikipediaService;
        }

        public async Task<IActionResult> Index(string ciudad = "Lima")
        {
            var (resumen, imagen, url, sugerencias) = await _wikipediaService.ObtenerResumenAsync(ciudad);
            ViewBag.Ciudad = ciudad;
            ViewBag.Resumen = resumen;
            ViewBag.Imagen = imagen;
            ViewBag.UrlWiki = url;
            ViewBag.Sugerencias = sugerencias;
            return View();
        }
    }
}

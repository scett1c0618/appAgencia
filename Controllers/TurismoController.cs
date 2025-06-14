using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app1.Servicios;

namespace app1.Controllers
{
    public class TurismoController : Controller
    {
        private readonly GooglePlacesService _googlePlacesService;
        public TurismoController(GooglePlacesService googlePlacesService)
        {
            _googlePlacesService = googlePlacesService;
        }

        public async Task<IActionResult> Index(string ciudad = "Lima", string? placeId = null)
        {
            if (!string.IsNullOrEmpty(placeId))
            {
                // Mostrar detalles del lugar seleccionado
                var (resumen, imagen, url, sugerencias, sitioWeb, tipoLugar, rating, telefono, horario, reviews, nombreLugar) = await _googlePlacesService.ObtenerInfoLugarAsync(placeId, true);
                ViewBag.Ciudad = ciudad;
                ViewBag.NombreLugar = nombreLugar ?? ciudad;
                ViewBag.Resumen = resumen;
                ViewBag.Imagen = imagen;
                ViewBag.UrlInfo = url;
                ViewBag.Sugerencias = sugerencias?.ToArray();
                ViewBag.SitioWeb = sitioWeb;
                ViewBag.TipoLugar = tipoLugar;
                ViewBag.Rating = rating;
                ViewBag.Telefono = telefono;
                ViewBag.Horario = horario;
                ViewBag.Reviews = reviews;
                ViewBag.Lugares = null;
                return View();
            }
            else
            {
                // Buscar lugares cercanos y mostrar lista
                var lugares = await _googlePlacesService.BuscarLugaresCercanosAsync(ciudad);
                ViewBag.Ciudad = ciudad;
                ViewBag.Lugares = lugares;
                ViewBag.Resumen = null;
                ViewBag.Imagen = null;
                ViewBag.UrlInfo = null;
                ViewBag.Sugerencias = null;
                ViewBag.SitioWeb = null;
                ViewBag.TipoLugar = null;
                ViewBag.Rating = null;
                ViewBag.Telefono = null;
                ViewBag.Horario = null;
                ViewBag.Reviews = null;
                return View();
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using app1.Data; // Asegúrate de tener la referencia correcta a tu contexto de base de datos

namespace app1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Compras(int? viajeId = null)
        {
            // Obtener todos los viajes para el filtro
            var viajes = await _context.Viajes.OrderBy(v => v.Titulo).ToListAsync();
            ViewBag.Viajes = viajes;
            ViewBag.ViajeIdSeleccionado = viajeId;
            // Obtener compras, filtrando por viaje si corresponde
            var comprasQuery = _context.Compras.AsQueryable();
            if (viajeId.HasValue && viajeId.Value != 0)
            {
                comprasQuery = comprasQuery.Where(c => c.Detalles.Any(d => d.ViajeId == viajeId.Value));
            }
            var compras = await comprasQuery.Include(c => c.Detalles).ToListAsync();
            return View(compras);
        }
    }
}

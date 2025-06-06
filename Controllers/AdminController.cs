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
        public async Task<IActionResult> Compras()
        {
            var compras = await _context.Compras.Include(c => c.Detalles).ToListAsync();
            return View(compras);
        }
    }
}

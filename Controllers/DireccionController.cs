using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app1.Data;
using app1.Models;

namespace app1.Controllers
{
    [Authorize]
    public class DireccionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DireccionController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Direccion
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var direccion = await _context.Direcciones.FirstOrDefaultAsync(d => d.ClienteId == userId);
            return View(direccion ?? new Direccion());
        }

        // POST: Direccion/GuardarDireccion
        [HttpPost]
        public async Task<IActionResult> GuardarDireccion(Direccion model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);
            var userId = _userManager.GetUserId(User);
            model.ClienteId = userId;
            var existente = await _context.Direcciones.FirstOrDefaultAsync(d => d.ClienteId == userId);
            if (existente != null)
            {
                existente.Departamento = model.Departamento;
                existente.Provincia = model.Provincia;
                existente.Distrito = model.Distrito;
                existente.DireccionTexto = model.DireccionTexto;
            }
            else
            {
                _context.Direcciones.Add(model);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("Validar");
        }

        // GET: Direccion/Validar
        public IActionResult Validar()
        {
            return View();
        }
    }
}

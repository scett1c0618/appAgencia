using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app1.Data;
using app1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace app1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartamentoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartamentoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Departamento
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Departamentos.ToListAsync());
        }

        // POST: Departamento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string Nombre, IFormFile Imagen)
        {
            string nombreArchivo = null;
            if (Imagen != null && Imagen.Length > 0)
            {
                var carpetaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "departamentos");
                if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);
                nombreArchivo = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(Imagen.FileName);
                var rutaArchivo = Path.Combine(carpetaDestino, nombreArchivo);
                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await Imagen.CopyToAsync(stream);
                }
            }
            if (!string.IsNullOrWhiteSpace(Nombre))
            {
                _context.Departamentos.Add(new Models.Departamento { Nombre = Nombre, Imagen = nombreArchivo });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Departamento/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var departamento = await _context.Departamentos.FindAsync(id);
            if (departamento != null)
            {
                _context.Departamentos.Remove(departamento);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

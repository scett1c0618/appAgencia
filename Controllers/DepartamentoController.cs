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
            string nombreArchivo = string.Empty;
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

        /// <summary>
        /// Devuelve la lista de todos los departamentos.
        /// </summary>
        /// <remarks>
        /// Endpoint público para obtener departamentos en formato JSON.
        /// 
        /// <b>Ejemplo de respuesta:</b>
        /// <code>
        /// [
        ///   {
        ///     "id": 1,
        ///     "nombre": "Cusco",
        ///     "imagen": "cusco.jpg"
        ///   },
        ///   {
        ///     "id": 2,
        ///     "nombre": "Arequipa",
        ///     "imagen": "arequipa.jpg"
        ///   }
        /// ]
        /// </code>
        /// </remarks>
        /// <returns>Lista de departamentos.</returns>
        [HttpGet("api/departamentos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<DepartamentoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepartamentosApi()
        {
            var departamentos = await _context.Departamentos
                .Select(d => new DepartamentoDto
                {
                    Id = d.Id,
                    Nombre = d.Nombre,
                    Imagen = d.Imagen
                }).ToListAsync();
            return Ok(departamentos);
        }

        /// <summary>
        /// Crea un nuevo departamento (solo para administradores).
        /// </summary>
        /// <remarks>
        /// Requiere multipart/form-data. La imagen es opcional.
        /// <b>Ejemplo de request (form-data):</b>
        /// <ul>
        ///   <li>nombre: "Amazonas"</li>
        ///   <li>imagen: archivo.jpg</li>
        /// </ul>
        /// <b>Ejemplo de respuesta exitosa:</b>
        /// <code>
        /// {
        ///   "id": 3,
        ///   "nombre": "Amazonas",
        ///   "imagen": "archivo.jpg"
        /// }
        /// </code>
        /// <b>Errores posibles:</b>
        /// <ul>
        ///   <li>400: "El nombre es obligatorio."</li>
        ///   <li>401: "No autenticado o no autorizado."</li>
        /// </ul>
        /// </remarks>
        /// <param name="request">Datos del departamento.</param>
        /// <returns>Departamento creado.</returns>
        [HttpPost("api/departamentos")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DepartamentoDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearDepartamentoApi([FromForm] DepartamentoApiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
                return BadRequest(new { error = "El nombre es obligatorio." });
            string? nombreArchivo = null;
            if (request.Imagen != null && request.Imagen.Length > 0)
            {
                var carpetaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "departamentos");
                if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);
                nombreArchivo = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(request.Imagen.FileName);
                var rutaArchivo = Path.Combine(carpetaDestino, nombreArchivo);
                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await request.Imagen.CopyToAsync(stream);
                }
            }
            var departamento = new Departamento
            {
                Nombre = request.Nombre,
                Imagen = nombreArchivo ?? string.Empty
            };
            _context.Departamentos.Add(departamento);
            await _context.SaveChangesAsync();
            var dto = new DepartamentoDto
            {
                Id = departamento.Id,
                Nombre = departamento.Nombre,
                Imagen = departamento.Imagen
            };
            return Created($"/api/departamentos/{departamento.Id}", dto);
        }
    }
}

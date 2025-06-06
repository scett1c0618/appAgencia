using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app1.Data;
using app1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace app1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ViajesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ViajesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Viajes
        [AllowAnonymous]
        public async Task<IActionResult> Index(int? departamentoId)
        {
            // Eliminar fechas de salida pasadas de todos los viajes
            var ahora = DateTime.Now;
            var viajesConFechas = _context.Viajes.Include(v => v.FechasSalida);
            bool cambios = false;
            foreach (var viaje in viajesConFechas)
            {
                var fechasPasadas = viaje.FechasSalida.Where(f => f.FechaSalida.Add(f.HoraSalida) < ahora).ToList();
                if (fechasPasadas.Any())
                {
                    _context.RemoveRange(fechasPasadas);
                    cambios = true;
                }
            }
            if (cambios)
                await _context.SaveChangesAsync();
            var departamentos = await _context.Departamentos.ToListAsync();
            ViewBag.Departamentos = new SelectList(departamentos, "Id", "Nombre");
            var viajes = _context.Viajes
                .Include(v => v.Departamento)
                .Include(v => v.FechasSalida)
                .AsQueryable();
            if (departamentoId.HasValue)
            {
                viajes = viajes.Where(v => v.DepartamentoId == departamentoId);
            }
            return View(await viajes.ToListAsync());
        }

        // GET: Viajes/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id, int? cantidad, bool? modificar, int? fechaSalidaId)
        {
            if (id == null) return NotFound();
            var viaje = await _context.Viajes
                .Include(v => v.Departamento)
                .Include(v => v.FechasSalida)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (viaje == null) return NotFound();
            // Eliminar fechas de salida pasadas de la base de datos
            var ahora = DateTime.Now;
            var fechasPasadas = viaje.FechasSalida.Where(f => f.FechaSalida.Add(f.HoraSalida) < ahora).ToList();
            if (fechasPasadas.Any())
            {
                _context.RemoveRange(fechasPasadas);
                await _context.SaveChangesAsync();
                viaje.FechasSalida = viaje.FechasSalida.Where(f => f.FechaSalida.Add(f.HoraSalida) >= ahora).ToList();
            }
            // Comentarios asociados a este viaje
            var comentarios = await _context.Contacto
                .Where(c => c.ViajeId == id && !string.IsNullOrEmpty(c.Mensaje))
                .OrderByDescending(c => c.FechaEnvio)
                .ToListAsync();
            ViewBag.Comentarios = comentarios;
            ViewBag.TotalComentarios = comentarios.Count;
            // Pasar info de modificación a la vista
            ViewBag.Modificar = modificar ?? false;
            ViewBag.Cantidad = cantidad;
            ViewBag.FechaSalidaId = fechaSalidaId;
            return View(viaje);
        }

        // GET: Viajes/Create
        public IActionResult Create()
        {
            ViewBag.Departamentos = new SelectList(_context.Departamentos, "Id", "Nombre");
            return View(new Viaje());
        }

        // POST: Viajes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Viaje viaje, IFormFile Imagen, List<string> fechasSalida, List<string> horasSalida, List<string> asientosPorFecha)
        {
            // Validación de imagen solo en backend
            if ((Imagen == null || Imagen.Length == 0) && string.IsNullOrEmpty(viaje.Imagen))
            {
                ModelState.AddModelError("Imagen", "La imagen es obligatoria.");
            }
            // Procesar imagen si se subió
            if (Imagen != null && Imagen.Length > 0)
            {
                var carpetaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "viajes");
                if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);
                var nombreArchivo = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(Imagen.FileName);
                var rutaArchivo = Path.Combine(carpetaDestino, nombreArchivo);
                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await Imagen.CopyToAsync(stream);
                }
                viaje.Imagen = nombreArchivo;
            }
            // Inicializar la colección de fechas
            var fechasColeccion = new List<FechaSalidaViaje>();
            DateTime ahora = DateTime.Now;
            if (fechasSalida != null && horasSalida != null && asientosPorFecha != null && fechasSalida.Count == asientosPorFecha.Count && fechasSalida.Count == horasSalida.Count)
            {
                for (int i = 0; i < fechasSalida.Count; i++)
                {
                    if (DateTime.TryParse(fechasSalida[i], out DateTime fecha) && TimeSpan.TryParse(horasSalida[i], out TimeSpan horaLocal) && int.TryParse(asientosPorFecha[i], out int asientos))
                    {
                        var fechaHora = fecha.Date.Add(horaLocal);
                        if (fechaHora <= ahora) continue; // Solo agregar fechas futuras
                        var fechaUtc = DateTime.SpecifyKind(fecha.Date, DateTimeKind.Utc);
                        if (fechaUtc >= DateTime.UtcNow.Date && asientos > 0)
                        {
                            fechasColeccion.Add(new FechaSalidaViaje
                            {
                                FechaSalida = fechaUtc,
                                HoraSalida = horaLocal,
                                AsientosDisponibles = asientos
                            });
                        }
                    }
                }
            }
            // Asignar la colección al modelo antes de agregar al contexto
            viaje.FechasSalida = fechasColeccion;
            ViewBag.Departamentos = new SelectList(_context.Departamentos, "Id", "Nombre", viaje.DepartamentoId);
            if (ModelState.IsValid)
            {
                _context.Viajes.Add(viaje);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            // Reconstruir fechas para la vista si hay error
            var fechasReconstruidas = new List<FechaSalidaViaje>();
            if (fechasSalida != null && horasSalida != null && asientosPorFecha != null && fechasSalida.Count == asientosPorFecha.Count && fechasSalida.Count == horasSalida.Count)
            {
                for (int i = 0; i < fechasSalida.Count; i++)
                {
                    if (DateTime.TryParse(fechasSalida[i], out DateTime fecha) && TimeSpan.TryParse(horasSalida[i], out TimeSpan horaLocal) && int.TryParse(asientosPorFecha[i], out int asientos))
                    {
                        var fechaHora = fecha.Date.Add(horaLocal);
                        if (fechaHora <= ahora) continue; // Solo reconstruir fechas futuras
                        fechasReconstruidas.Add(new FechaSalidaViaje
                        {
                            FechaSalida = fecha,
                            HoraSalida = horaLocal,
                            AsientosDisponibles = asientos
                        });
                    }
                }
            }
            viaje.FechasSalida = fechasReconstruidas;
            return View(viaje);
        }

        // GET: Viajes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var viaje = await _context.Viajes
                .Include(v => v.FechasSalida)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (viaje == null) return NotFound();
            // Eliminar fechas de salida pasadas de la base de datos
            var hoy = DateTime.Today;
            var fechasPasadas = viaje.FechasSalida.Where(f => f.FechaSalida.Date < hoy).ToList();
            if (fechasPasadas.Any())
            {
                _context.RemoveRange(fechasPasadas);
                await _context.SaveChangesAsync();
                viaje.FechasSalida = viaje.FechasSalida.Where(f => f.FechaSalida.Date >= hoy).ToList();
            }
            ViewBag.Departamentos = new SelectList(_context.Departamentos, "Id", "Nombre", viaje.DepartamentoId);
            return View(viaje);
        }

        // POST: Viajes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Viaje viaje, IFormFile NuevaImagen, List<int> fechasSalidaIds, List<DateTime> fechasSalida, List<string> horasSalida, List<int> asientosPorFecha)
        {
            DateTime ahora = DateTime.Now;
            if (id != viaje.Id) return NotFound();
            var viajeDb = await _context.Viajes
                .Include(v => v.FechasSalida)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (viajeDb == null) return NotFound();

            // Validación de imagen solo en backend
            if ((NuevaImagen == null || NuevaImagen.Length == 0) && string.IsNullOrEmpty(viajeDb.Imagen))
            {
                ModelState.AddModelError("Imagen", "La imagen es obligatoria.");
            }

            if (!ModelState.IsValid)
            {
                // Reconstruir modelo para la vista en caso de error
                var fechasReconstruidas = new List<FechaSalidaViaje>();
                if (fechasSalida != null && horasSalida != null && asientosPorFecha != null && fechasSalida.Count == asientosPorFecha.Count && fechasSalida.Count == horasSalida.Count)
                {
                    for (int i = 0; i < fechasSalida.Count; i++)
                    {
                        TimeSpan horaLocal = TimeSpan.TryParse(horasSalida[i], out var h) ? h : TimeSpan.Zero;
                        var fechaHora = fechasSalida[i].Date.Add(horaLocal);
                        if (fechaHora <= ahora) continue; // Solo reconstruir fechas futuras
                        fechasReconstruidas.Add(new FechaSalidaViaje
                        {
                            Id = (fechasSalidaIds != null && i < fechasSalidaIds.Count) ? fechasSalidaIds[i] : 0,
                            FechaSalida = fechasSalida[i],
                            HoraSalida = horaLocal,
                            AsientosDisponibles = asientosPorFecha[i]
                        });
                    }
                }
                viaje.FechasSalida = fechasReconstruidas;
                ViewBag.Departamentos = new SelectList(_context.Departamentos, "Id", "Nombre", viaje.DepartamentoId);
                return View(viaje);
            }

            // Actualizar datos simples
            viajeDb.Titulo = viaje.Titulo;
            viajeDb.Descripcion = viaje.Descripcion;
            viajeDb.Precio = viaje.Precio;
            viajeDb.DepartamentoId = viaje.DepartamentoId;

            // Imagen
            if (NuevaImagen != null && NuevaImagen.Length > 0)
            {
                var carpetaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "viajes");
                if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);
                var nombreArchivo = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(NuevaImagen.FileName);
                var rutaArchivo = Path.Combine(carpetaDestino, nombreArchivo);
                using (var stream = new FileStream(rutaArchivo, FileMode.Create))
                {
                    await NuevaImagen.CopyToAsync(stream);
                }
                viajeDb.Imagen = nombreArchivo;
            }
            else if (!string.IsNullOrEmpty(viaje.Imagen))
            {
                // Si no se sube nueva imagen, conservar la existente
                viajeDb.Imagen = viaje.Imagen;
            }

            // --- Sincronizar FechasSalida (solución robusta) ---
            // Eliminar todas las fechas existentes y agregar las nuevas
            if (viajeDb.FechasSalida == null)
                viajeDb.FechasSalida = new List<FechaSalidaViaje>();
            // Eliminar todas las fechas existentes
            foreach (var f in viajeDb.FechasSalida.ToList())
                _context.FechasSalidaViaje.Remove(f);
            viajeDb.FechasSalida.Clear();
            // Agregar las nuevas fechas recibidas
            if (fechasSalida != null && horasSalida != null && asientosPorFecha != null && fechasSalida.Count == asientosPorFecha.Count && fechasSalida.Count == horasSalida.Count)
            {
                for (int i = 0; i < fechasSalida.Count; i++)
                {
                    TimeSpan horaLocal = TimeSpan.TryParse(horasSalida[i], out var h2) ? h2 : TimeSpan.Zero;
                    var fechaHora = fechasSalida[i].Date.Add(horaLocal);
                    if (fechaHora <= ahora) continue; // Solo agregar fechas futuras
                    var fechaUtc = DateTime.SpecifyKind(fechasSalida[i].Date, DateTimeKind.Utc);
                    var asientos = asientosPorFecha[i];
                    if (fechaUtc < DateTime.UtcNow.Date || asientos < 1) continue;
                    viajeDb.FechasSalida.Add(new FechaSalidaViaje
                    {
                        FechaSalida = fechaUtc,
                        HoraSalida = horaLocal,
                        AsientosDisponibles = asientos,
                        ViajeId = viajeDb.Id
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Viajes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var viaje = await _context.Viajes.FindAsync(id);
            if (viaje == null) return NotFound();
            return View(viaje);
        }

        // POST: Viajes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var viaje = await _context.Viajes.FindAsync(id);
            if (viaje != null)
            {
                _context.Viajes.Remove(viaje);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

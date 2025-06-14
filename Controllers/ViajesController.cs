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
            ViewBag.ComentariosPositivos = comentarios.Count(c => c.Etiqueta == "Positivo");
            ViewBag.ComentariosNegativos = comentarios.Count(c => c.Etiqueta == "Negativo");
            ViewBag.ComentariosNeutros = comentarios.Count(c => c.Etiqueta == "Neutro");
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

        /// <summary>
        /// Devuelve la lista de todos los viajes disponibles.
        /// </summary>
        /// <remarks>
        /// Endpoint público para obtener viajes en formato JSON.
        /// 
        /// <b>Ejemplo de respuesta:</b>
        /// <code>
        /// [
        ///   {
        ///     "id": 10,
        ///     "titulo": "Cusco Mágico",
        ///     "descripcion": "Explora Machu Picchu y el Valle Sagrado.",
        ///     "precio": 1200.00,
        ///     "departamento": "Cusco",
        ///     "imagen": "cusco.jpg"
        ///   }
        /// ]
        /// </code>
        /// </remarks>
        /// <returns>Lista de viajes.</returns>
        [HttpGet("api/viajes")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ViajeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetViajesApi()
        {
            var viajes = await _context.Viajes
                .Include(v => v.Departamento)
                .Select(v => new ViajeDto
                {
                    Id = v.Id,
                    Titulo = v.Titulo,
                    Descripcion = v.Descripcion,
                    Precio = v.Precio,
                    Departamento = v.Departamento != null ? v.Departamento.Nombre : null,
                    Imagen = v.Imagen
                }).ToListAsync();
            return Ok(viajes);
        }

        /// <summary>
        /// Crea un nuevo viaje (solo para administradores).
        /// </summary>
        /// <remarks>
        /// Requiere multipart/form-data. La imagen es obligatoria. No incluye fechas de salida (se agregan luego).
        /// <b>Ejemplo de request (form-data):</b>
        /// <ul>
        ///   <li>titulo: "Cusco Mágico"</li>
        ///   <li>descripcion: "Explora Machu Picchu y el Valle Sagrado."</li>
        ///   <li>precio: 1200.00</li>
        ///   <li>departamentoId: 1</li>
        ///   <li>imagen: archivo.jpg</li>
        /// </ul>
        /// <b>Ejemplo de respuesta exitosa:</b>
        /// <code>
        /// {
        ///   "id": 10,
        ///   "titulo": "Cusco Mágico",
        ///   "descripcion": "Explora Machu Picchu y el Valle Sagrado.",
        ///   "precio": 1200.00,
        ///   "departamento": "Cusco",
        ///   "imagen": "archivo.jpg"
        /// }
        /// </code>
        /// <b>Errores posibles:</b>
        /// <ul>
        ///   <li>400: "La imagen es obligatoria."</li>
        ///   <li>400: "Datos de viaje inválidos."</li>
        ///   <li>401: "No autenticado o no autorizado."</li>
        /// </ul>
        /// </remarks>
        /// <param name="request">Datos del viaje.</param>
        /// <returns>Viaje creado.</returns>
        [HttpPost("api/viajes")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ViajeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearViajeApi([FromForm] ViajeApiRequest request)
        {
            var Imagen = request.Imagen;
            if (Imagen == null || Imagen.Length == 0)
                return BadRequest(new { error = "La imagen es obligatoria." });
            if (string.IsNullOrWhiteSpace(request.Titulo) || string.IsNullOrWhiteSpace(request.Descripcion) || request.Precio <= 0 || request.DepartamentoId <= 0)
                return BadRequest(new { error = "Datos de viaje inválidos." });
            // Guardar imagen
            var carpetaDestino = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "viajes");
            if (!Directory.Exists(carpetaDestino)) Directory.CreateDirectory(carpetaDestino);
            var nombreArchivo = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(Imagen.FileName);
            var rutaArchivo = Path.Combine(carpetaDestino, nombreArchivo);
            using (var stream = new FileStream(rutaArchivo, FileMode.Create))
            {
                await Imagen.CopyToAsync(stream);
            }
            var viaje = new Viaje
            {
                Titulo = request.Titulo,
                Descripcion = request.Descripcion,
                Precio = request.Precio,
                DepartamentoId = request.DepartamentoId,
                Imagen = nombreArchivo
            };
            _context.Viajes.Add(viaje);
            await _context.SaveChangesAsync();
            // Proyección a DTO
            var departamento = await _context.Departamentos.FindAsync(viaje.DepartamentoId);
            var dto = new ViajeDto
            {
                Id = viaje.Id,
                Titulo = viaje.Titulo,
                Descripcion = viaje.Descripcion,
                Precio = viaje.Precio,
                Departamento = departamento?.Nombre,
                Imagen = viaje.Imagen
            };
            return Created($"/api/viajes/{viaje.Id}", dto);
        }

        /// <summary>
        /// Devuelve la lista de todas las fechas de salida de viajes.
        /// </summary>
        /// <remarks>
        /// Endpoint público para obtener fechas de salida en formato JSON.
        /// 
        /// <b>Ejemplo de respuesta:</b>
        /// <code>
        /// [
        ///   {
        ///     "id": 100,
        ///     "fechaSalida": "2025-07-01T00:00:00Z",
        ///     "horaSalida": "08:00:00",
        ///     "asientosDisponibles": 20
        ///   }
        /// ]
        /// </code>
        /// </remarks>
        /// <returns>Lista de fechas de salida.</returns>
        [HttpGet("api/fechas-salida")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<FechaSalidaViajeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFechasSalidaApi()
        {
            var fechas = await _context.FechasSalidaViaje
                .Select(f => new FechaSalidaViajeDto
                {
                    Id = f.Id,
                    FechaSalida = f.FechaSalida,
                    HoraSalida = f.HoraSalida,
                    AsientosDisponibles = f.AsientosDisponibles
                }).ToListAsync();
            return Ok(fechas);
        }

        /// <summary>
        /// Crea una nueva fecha de salida para un viaje (solo para administradores).
        /// </summary>
        /// <remarks>
        /// Requiere autenticación admin. La fecha y hora deben ser futuras y los asientos mayores a 0.
        /// <b>Ejemplo de request:</b>
        /// <code>
        /// {
        ///   "viajeId": 10,
        ///   "fechaSalida": "2025-07-01T00:00:00Z",
        ///   "horaSalida": "08:00:00",
        ///   "asientosDisponibles": 20
        /// }
        /// </code>
        /// <b>Ejemplo de respuesta exitosa:</b>
        /// <code>
        /// {
        ///   "id": 100,
        ///   "fechaSalida": "2025-07-01T00:00:00Z",
        ///   "horaSalida": "08:00:00",
        ///   "asientosDisponibles": 20
        /// }
        /// </code>
        /// <b>Errores posibles:</b>
        /// <ul>
        ///   <li>400: "Datos inválidos."</li>
        ///   <li>400: "La fecha debe ser futura."</li>
        ///   <li>400: "Viaje no encontrado."</li>
        ///   <li>401: "No autenticado o no autorizado."</li>
        /// </ul>
        /// </remarks>
        /// <param name="request">Datos de la fecha de salida.</param>
        /// <returns>Fecha de salida creada.</returns>
        [HttpPost("api/fechas-salida")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FechaSalidaViajeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearFechaSalidaApi([FromBody] FechaSalidaViajeApiRequest request)
        {
            if (request == null || request.ViajeId <= 0 || request.AsientosDisponibles < 1)
                return BadRequest(new { error = "Datos inválidos." });
            if (request.FechaSalida.Date < DateTime.UtcNow.Date)
                return BadRequest(new { error = "La fecha debe ser futura." });
            var viaje = await _context.Viajes.FindAsync(request.ViajeId);
            if (viaje == null)
                return BadRequest(new { error = "Viaje no encontrado." });
            var fecha = new FechaSalidaViaje
            {
                ViajeId = request.ViajeId,
                FechaSalida = request.FechaSalida.Date,
                HoraSalida = request.HoraSalida,
                AsientosDisponibles = request.AsientosDisponibles
            };
            _context.FechasSalidaViaje.Add(fecha);
            await _context.SaveChangesAsync();
            var dto = new FechaSalidaViajeDto
            {
                Id = fecha.Id,
                FechaSalida = fecha.FechaSalida,
                HoraSalida = fecha.HoraSalida,
                AsientosDisponibles = fecha.AsientosDisponibles
            };
            return Created($"/api/fechas-salida/{fecha.Id}", dto);
        }
    }
}

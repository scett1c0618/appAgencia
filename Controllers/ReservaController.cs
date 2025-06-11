using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using app1.Data;
using app1.Models;

namespace app1.Controllers
{
    [AllowAnonymous]
    public class ReservaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const string SessionReservasKey = "ReservasSesion";

        public ReservaController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reserva/Index
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                var reservas = await _context.Reservas
                    .Include(r => r.Viaje)
                    .Include(r => r.Viaje.FechasSalida)
                    .Where(r => r.ClienteId == userId)
                    .ToListAsync();
                // Eliminar reservas con fecha/hora pasada
                var ahora = DateTime.Now;
                var reservasVigentes = reservas.Where(r =>
                    r.Viaje != null && r.Viaje.FechasSalida != null &&
                    r.Viaje.FechasSalida.Any(f => f.FechaSalida.Date == r.FechaSalida.Date && f.FechaSalida.Add(f.HoraSalida) >= ahora)
                ).ToList();
                if (reservasVigentes.Count != reservas.Count)
                {
                    var expiradas = reservas.Except(reservasVigentes).ToList();
                    _context.Reservas.RemoveRange(expiradas);
                    await _context.SaveChangesAsync();
                }
                return View(reservasVigentes);
            }
            else
            {
                // Mostrar reservas de sesión para usuario no autenticado
                var reservasSesion = ObtenerReservasSesion() ?? new List<Reserva>();
                var viajes = await _context.Viajes.Include(v => v.FechasSalida).ToListAsync();
                var ahora = DateTime.Now;
                // Eliminar reservas expiradas
                var reservasVigentes = reservasSesion.Where(r =>
                    viajes.Any(v => v.Id == r.ViajeId && v.FechasSalida != null && v.FechasSalida.Any(f => f.FechaSalida.Date == r.FechaSalida.Date && f.FechaSalida.Add(f.HoraSalida) >= ahora))
                ).ToList();
                if (reservasVigentes.Count != reservasSesion.Count)
                {
                    GuardarReservasSesion(reservasVigentes);
                }
                // Cargar datos de viaje para mostrar
                foreach (var r in reservasVigentes)
                {
                    var v = viajes.FirstOrDefault(vj => vj.Id == r.ViajeId);
                    if (v != null) r.Viaje = v;
                }
                return View(reservasVigentes);
            }
        }

        // POST: Reserva/Agregar
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Agregar(int viajeId, int cantidad, int fechaSalidaId, bool? modificar)
        {
            var fechaSalida = await _context.FechasSalidaViaje.FirstOrDefaultAsync(f => f.Id == fechaSalidaId && f.ViajeId == viajeId);
            if (fechaSalida == null || fechaSalida.FechaSalida < DateTime.Today || fechaSalida.AsientosDisponibles < cantidad)
            {
                TempData["Error"] = "No hay suficientes asientos disponibles o la fecha no es válida.";
                return RedirectToAction("Details", "Viajes", new { id = viajeId });
            }
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User) ?? string.Empty;
                var reservaExistente = await _context.Reservas.FirstOrDefaultAsync(r => r.ClienteId == userId && r.ViajeId == viajeId && r.FechaSalida == fechaSalida.FechaSalida);
                if (reservaExistente != null)
                {
                    if (modificar == true)
                    {
                        // Reemplazar cantidad
                        reservaExistente.Cantidad = cantidad;
                    }
                    else
                    {
                        // Sumar cantidad (comportamiento original)
                        if (fechaSalida.AsientosDisponibles < cantidad + reservaExistente.Cantidad)
                        {
                            TempData["Error"] = "No hay suficientes asientos disponibles para aumentar la reserva.";
                            return RedirectToAction("Details", "Viajes", new { id = viajeId });
                        }
                        reservaExistente.Cantidad += cantidad;
                    }
                    reservaExistente.FechaReserva = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                }
                else
                {
                    var reserva = new Reserva
                    {
                        ClienteId = userId,
                        ViajeId = viajeId,
                        Cantidad = cantidad,
                        FechaReserva = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        FechaSalida = fechaSalida.FechaSalida
                    };
                    _context.Reservas.Add(reserva);
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                var reservas = ObtenerReservasSesion();
                var reservaExistente = reservas.FirstOrDefault(r => r.ViajeId == viajeId && r.FechaSalida == fechaSalida.FechaSalida);
                if (reservaExistente != null)
                {
                    if (modificar == true)
                    {
                        reservaExistente.Cantidad = cantidad;
                    }
                    else
                    {
                        if (fechaSalida.AsientosDisponibles < cantidad + reservaExistente.Cantidad)
                        {
                            TempData["Error"] = "No hay suficientes asientos disponibles para aumentar la reserva.";
                            return RedirectToAction("Details", "Viajes", new { id = viajeId });
                        }
                        reservaExistente.Cantidad += cantidad;
                    }
                    reservaExistente.FechaReserva = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                }
                else
                {
                    reservas.Add(new Reserva
                    {
                        ClienteId = string.Empty, // No null
                        ViajeId = viajeId,
                        Cantidad = cantidad,
                        FechaReserva = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        FechaSalida = fechaSalida.FechaSalida
                    });
                }
                GuardarReservasSesion(reservas);
            }
            return RedirectToAction("Index");
        }

        // POST: Reserva/Checkout
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Checkout()
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Embarque", "Checkout");
            }
            else
            {
                // Redirigir a login y volver a embarque tras autenticación
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = Url.Action("Embarque", "Checkout") });
            }
        }

        // GET: Reserva/MigrarReservasSesion
        [Authorize]
        public async Task<IActionResult> MigrarReservasSesion()
        {
            var reservasSesion = ObtenerReservasSesion();
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var errores = new List<string>();
            foreach (var r in reservasSesion)
            {
                // Buscar la fecha de salida correspondiente
                var fechaSalida = await _context.FechasSalidaViaje.FirstOrDefaultAsync(f => f.ViajeId == r.ViajeId && f.FechaSalida == r.FechaSalida);
                if (fechaSalida == null || fechaSalida.FechaSalida < DateTime.Today || fechaSalida.AsientosDisponibles < r.Cantidad)
                {
                    errores.Add($"No se pudo migrar la reserva del viaje ID {r.ViajeId} para la fecha {r.FechaSalida:dd/MM/yyyy}: asientos insuficientes o fecha inválida.");
                    continue;
                }
                // Buscar si ya existe una reserva para ese viaje, usuario y fecha
                var reservaExistente = await _context.Reservas.FirstOrDefaultAsync(x => x.ClienteId == userId && x.ViajeId == r.ViajeId && x.FechaSalida == r.FechaSalida);
                if (reservaExistente != null)
                {
                    if (fechaSalida.AsientosDisponibles < r.Cantidad + reservaExistente.Cantidad)
                    {
                        errores.Add($"No se pudo migrar la reserva del viaje ID {r.ViajeId} para la fecha {r.FechaSalida:dd/MM/yyyy}: asientos insuficientes para aumentar la reserva.");
                        continue;
                    }
                    reservaExistente.Cantidad += r.Cantidad;
                    reservaExistente.FechaReserva = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                }
                else
                {
                    _context.Reservas.Add(new Reserva
                    {
                        ClienteId = userId,
                        ViajeId = r.ViajeId,
                        Cantidad = r.Cantidad,
                        FechaReserva = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                        FechaSalida = r.FechaSalida
                    });
                }
                // NO descontar asientos aquí
            }
            await _context.SaveChangesAsync();
            GuardarReservasSesion(new List<Reserva>()); // Limpiar sesión
            if (errores.Count > 0)
            {
                TempData["Error"] = string.Join("<br>", errores);
            }
            return RedirectToAction("Index");
        }

        // POST: Reserva/Eliminar
        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var reserva = await _context.Reservas.FindAsync(id);
                if (reserva != null)
                {
                    _context.Reservas.Remove(reserva);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var reservas = ObtenerReservasSesion();
                var reserva = reservas.FirstOrDefault(r => r.Id == id);
                if (reserva != null)
                {
                    reservas.Remove(reserva);
                    GuardarReservasSesion(reservas);
                }
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Obtiene las reservas activas del usuario autenticado.
        /// </summary>
        /// <remarks>
        /// Devuelve la lista de reservas activas del usuario autenticado en formato JSON.
        /// 
        /// <b>Requiere autenticación.</b>
        /// 
        /// <b>Ejemplo de respuesta:</b>
        /// <code>
        /// [
        ///   {
        ///     "id": 1,
        ///     "viajeId": 5,
        ///     "viajeTitulo": "Cusco Mágico",
        ///     "cantidad": 2,
        ///     "fechaReserva": "2025-06-10T15:00:00Z",
        ///     "fechaSalida": "2025-07-01T00:00:00Z"
        ///   }
        /// ]
        /// </code>
        /// </remarks>
        /// <returns>Lista de reservas en formato JSON.</returns>
        [HttpGet("api/reservas")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<ReservaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetReservasUsuario()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var reservas = await _context.Reservas
                .Include(r => r.Viaje)
                .Where(r => r.ClienteId == userId)
                .ToListAsync();
            var reservasDto = reservas.Select(r => new ReservaDto
            {
                Id = r.Id,
                ViajeId = r.ViajeId,
                ViajeTitulo = r.Viaje?.Titulo ?? string.Empty,
                Cantidad = r.Cantidad,
                FechaReserva = r.FechaReserva,
                FechaSalida = r.FechaSalida
            }).ToList();
            return Ok(reservasDto);
        }

        /// <summary>
        /// Crea una nueva reserva para el usuario autenticado.
        /// </summary>
        /// <remarks>
        /// Permite crear una reserva si hay asientos disponibles y la fecha es válida.
        /// <b>Requiere autenticación.</b>
        /// 
        /// <b>Ejemplo de request:</b>
        /// <code>
        /// {
        ///   "viajeId": 5,
        ///   "cantidad": 2,
        ///   "fechaSalidaId": 10
        /// }
        /// </code>
        /// <b>Ejemplo de respuesta exitosa:</b>
        /// <code>
        /// {
        ///   "id": 12,
        ///   "viajeId": 5,
        ///   "viajeTitulo": "Cusco Mágico",
        ///   "cantidad": 2,
        ///   "fechaReserva": "2025-06-10T15:00:00Z",
        ///   "fechaSalida": "2025-07-01T00:00:00Z"
        /// }
        /// </code>
        /// <b>Errores posibles:</b>
        /// <ul>
        ///   <li>400: "No hay suficientes asientos disponibles o la fecha no es válida."</li>
        ///   <li>400: "Ya existe una reserva para este viaje y fecha."</li>
        ///   <li>401: "No autenticado."</li>
        /// </ul>
        /// </remarks>
        /// <param name="request">Datos de la reserva: viajeId, cantidad, fechaSalidaId.</param>
        /// <returns>Reserva creada o error.</returns>
        [HttpPost("api/reservas")]
        [Authorize]
        [ProducesResponseType(typeof(ReservaDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CrearReservaApi([FromBody] ReservaApiRequest request)
        {
            if (request == null || request.Cantidad < 1)
                return BadRequest(new { error = "Datos de reserva inválidos." });
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();
            var fechaSalida = await _context.FechasSalidaViaje.FirstOrDefaultAsync(f => f.Id == request.FechaSalidaId && f.ViajeId == request.ViajeId);
            if (fechaSalida == null || fechaSalida.FechaSalida < DateTime.Today || fechaSalida.AsientosDisponibles < request.Cantidad)
                return BadRequest(new { error = "No hay suficientes asientos disponibles o la fecha no es válida." });
            var reservaExistente = await _context.Reservas.FirstOrDefaultAsync(r => r.ClienteId == userId && r.ViajeId == request.ViajeId && r.FechaSalida == fechaSalida.FechaSalida);
            if (reservaExistente != null)
                return BadRequest(new { error = "Ya existe una reserva para este viaje y fecha." });
            var reserva = new Reserva
            {
                ClienteId = userId,
                ViajeId = request.ViajeId,
                Cantidad = request.Cantidad,
                FechaReserva = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                FechaSalida = fechaSalida.FechaSalida
            };
            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();
            // Proyección a DTO
            var dto = new ReservaDto
            {
                Id = reserva.Id,
                ViajeId = reserva.ViajeId,
                ViajeTitulo = (await _context.Viajes.FindAsync(reserva.ViajeId))?.Titulo ?? string.Empty,
                Cantidad = reserva.Cantidad,
                FechaReserva = reserva.FechaReserva,
                FechaSalida = reserva.FechaSalida
            };
            return Created($"/api/reservas/{reserva.Id}", dto);
        }

        /// <summary>
        /// Modelo para crear reservas vía API.
        /// </summary>
        public class ReservaApiRequest
        {
            public int ViajeId { get; set; }
            public int Cantidad { get; set; }
            public int FechaSalidaId { get; set; }
        }

        /// <summary>
        /// DTO para exponer reservas en la API sin ciclos.
        /// </summary>
        public class ReservaDto
        {
            public int Id { get; set; }
            public int ViajeId { get; set; }
            public string? ViajeTitulo { get; set; }
            public int Cantidad { get; set; }
            public DateTime FechaReserva { get; set; }
            public DateTime FechaSalida { get; set; }
        }

        // Métodos auxiliares para sesión
        private List<Reserva> ObtenerReservasSesion()
        {
            var data = HttpContext.Session.GetString(SessionReservasKey);
            if (string.IsNullOrEmpty(data)) return new List<Reserva>();
            return JsonConvert.DeserializeObject<List<Reserva>>(data) ?? new List<Reserva>();
        }
        private void GuardarReservasSesion(List<Reserva> reservas)
        {
            HttpContext.Session.SetString(SessionReservasKey, JsonConvert.SerializeObject(reservas));
        }
    }
}

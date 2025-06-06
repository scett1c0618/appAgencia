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
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
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
                        ClienteId = null,
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
            var userId = _userManager.GetUserId(User);
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
            if (User.Identity.IsAuthenticated)
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

        // Métodos auxiliares para sesión
        private List<Reserva> ObtenerReservasSesion()
        {
            var data = HttpContext.Session.GetString(SessionReservasKey);
            if (string.IsNullOrEmpty(data)) return new List<Reserva>();
            return JsonConvert.DeserializeObject<List<Reserva>>(data);
        }
        private void GuardarReservasSesion(List<Reserva> reservas)
        {
            HttpContext.Session.SetString(SessionReservasKey, JsonConvert.SerializeObject(reservas));
        }
    }
}

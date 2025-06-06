using System;
using System.Collections.Generic;
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
    [AllowAnonymous]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Checkout/Embarque
        public async Task<IActionResult> Embarque()
        {
            // Obtener reservas del usuario o sesión
            List<Reserva> reservas;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                reservas = await _context.Reservas.Include(r => r.Viaje).Where(r => r.ClienteId == userId).ToListAsync();
            }
            else
            {
                reservas = ObtenerReservasSesion();
                var viajes = await _context.Viajes.ToListAsync();
                foreach (var r in reservas) r.Viaje = viajes.FirstOrDefault(v => v.Id == r.ViajeId);
            }
            ViewBag.Reservas = reservas;
            return View();
        }

        // GET: Checkout/Pago
        [Authorize]
        public async Task<IActionResult> Pago()
        {
            var userId = _userManager.GetUserId(User);
            var reservas = await _context.Reservas.Include(r => r.Viaje).Where(r => r.ClienteId == userId).ToListAsync();
            ViewBag.Reservas = reservas;
            return View();
        }

        // POST: Checkout/Pagar
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Pagar(string NombreApellido, string NumeroTarjeta, string FechaVencimiento, string CVV, string DNI)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);
            var reservas = await _context.Reservas.Include(r => r.Viaje).Where(r => r.ClienteId == userId).ToListAsync();
            if (!reservas.Any()) return RedirectToAction("Embarque");
            var compra = new Compra
            {
                UsuarioId = userId,
                UsuarioEmail = user?.Email,
                FechaCompra = DateTime.UtcNow,
                MontoTotal = reservas.Sum(r => r.Cantidad * (r.Viaje?.Precio ?? 0)),
                Detalles = reservas.Select(r => new DetalleCompra
                {
                    ViajeId = r.ViajeId,
                    ViajeTitulo = r.Viaje?.Titulo ?? "",
                    FechaSalida = r.FechaSalida,
                    Cantidad = r.Cantidad,
                    PrecioUnitario = r.Viaje?.Precio ?? 0
                }).ToList()
            };
            _context.Compras.Add(compra);

            // Descontar asientos solo al concretar la compra
            foreach (var reserva in reservas)
            {
                var fechaSalida = await _context.FechasSalidaViaje.FirstOrDefaultAsync(f => f.ViajeId == reserva.ViajeId && f.FechaSalida == reserva.FechaSalida);
                if (fechaSalida != null)
                {
                    fechaSalida.AsientosDisponibles -= reserva.Cantidad;
                    if (fechaSalida.AsientosDisponibles < 0) fechaSalida.AsientosDisponibles = 0;
                }
            }

            _context.Reservas.RemoveRange(reservas);
            await _context.SaveChangesAsync();
            return RedirectToAction("Confirmacion");
        }

        // GET: Checkout/Confirmacion
        [Authorize]
        public IActionResult Confirmacion()
        {
            return View();
        }

        // Métodos para reservas de sesión (anónimos)
        private List<Reserva> ObtenerReservasSesion()
        {
            var data = HttpContext.Session.GetString("ReservasSesion");
            if (string.IsNullOrEmpty(data)) return new List<Reserva>();
            return System.Text.Json.JsonSerializer.Deserialize<List<Reserva>>(data) ?? new List<Reserva>();
        }
    }
}

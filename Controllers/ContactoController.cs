using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using app1.Data;
using app1.Models;
using app1.Servicios;
using Microsoft.EntityFrameworkCore;

namespace app1.Controllers
{
    public class ContactoController : Controller
    {
        private readonly ILogger<ContactoController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SentimentService _sentimentService;
        private readonly ClasificacionMensajeService _clasificacionService;

        public ContactoController(ILogger<ContactoController> logger, UserManager<IdentityUser> userManager, ApplicationDbContext context, SentimentService sentimentService, ClasificacionMensajeService clasificacionService)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _sentimentService = sentimentService;
            _clasificacionService = clasificacionService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var contacto = new Contacto();
            if (user != null)
            {
                contacto.Nombre = user.UserName;
                contacto.Correo = user.Email;
                contacto.Telefono = user.PhoneNumber ?? string.Empty;
            }
            if (TempData["Resultado"] != null)
            {
                ViewData["Message"] = TempData["Resultado"]?.ToString();
            }
            return View("Index", contacto);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RegistrarContacto(Contacto contacto)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    if (string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        user.PhoneNumber = contacto.Telefono;
                        await _userManager.UpdateAsync(user);
                    }
                }
                // Clasificar mensaje automáticamente
                if (!string.IsNullOrWhiteSpace(contacto.Mensaje))
                {
                    contacto.Etiqueta = _clasificacionService.Clasificar(contacto.Mensaje);
                }
                _context.Contacto.Add(contacto);
                await _context.SaveChangesAsync();
                TempData["Resultado"] = "Mensaje enviado correctamente." + (contacto.Etiqueta != null ? $" El mensaje fue clasificado como: {contacto.Etiqueta}" : "");
                return RedirectToAction("Index");
            }
            return View("Index", contacto);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminMensajes()
        {
            // Solo mensajes de contacto (sin ViajeId asociado)
            var mensajes = _context.Contacto
                .Where(c => c.ViajeId == null)
                .OrderByDescending(c => c.FechaEnvio)
                .ToList();
            // Mostrar la etiqueta de clasificación automática
            return View("AdminMensajes", mensajes);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminComentarios(int? viajeId = null)
        {
            // Obtener todos los viajes para el filtro
            var viajes = _context.Viajes.OrderBy(v => v.Titulo).ToList();
            ViewBag.Viajes = viajes;
            ViewBag.ViajeIdSeleccionado = viajeId;
            // Solo comentarios (con ViajeId asociado), incluir datos del viaje
            var comentariosQuery = _context.Contacto
                .Include(c => c.Viaje)
                .Where(c => c.ViajeId != null);
            if (viajeId.HasValue && viajeId.Value != 0)
            {
                comentariosQuery = comentariosQuery.Where(c => c.ViajeId == viajeId.Value);
            }
            var comentarios = comentariosQuery
                .OrderByDescending(c => c.FechaEnvio)
                .ToList();
            return View("AdminComentarios", comentarios);
        }

        [Authorize]
        public async Task<IActionResult> Comentario(int? viajeId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            // Obtener viajes comprados por el usuario (basado en compras finalizadas)
            var viajesComprados = (from compra in _context.Compras
                                   where compra.UsuarioId == user.Id
                                   from detalle in compra.Detalles
                                   join viaje in _context.Viajes on detalle.ViajeId equals viaje.Id
                                   select viaje)
                                   .Distinct()
                                   .ToList();
            ViewBag.ViajesComprados = viajesComprados;
            var contacto = new Contacto { Nombre = user.UserName, Correo = user.Email, Telefono = user.PhoneNumber ?? string.Empty };
            if (viajeId != null)
                contacto.ViajeId = viajeId.Value;
            return View("Comentario", contacto);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RegistrarComentario(Contacto contacto)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            // Validar que el usuario haya comprado el viaje seleccionado (basado en compras finalizadas, no en el carrito)
            var haComprado = (from compra in _context.Compras
                              where compra.UsuarioId == user.Id
                              from detalle in compra.Detalles
                              where detalle.ViajeId == contacto.ViajeId
                              select detalle).Any();
            if (!haComprado)
            {
                ModelState.AddModelError("ViajeId", "Solo puedes comentar sobre viajes que hayas comprado.");
            }
            if (ModelState.IsValid)
            {
                contacto.UserId = user.Id;
                if (!string.IsNullOrWhiteSpace(contacto.Mensaje))
                {
                    var (etiqueta, puntuacion) = await _sentimentService.AnalizarSentimientoAsync(contacto.Mensaje);
                    contacto.Etiqueta = etiqueta;
                    contacto.Puntuacion = (float)puntuacion;
                }
                _context.Contacto.Add(contacto);
                await _context.SaveChangesAsync();
                TempData["Resultado"] = "Comentario enviado correctamente." + (contacto.Etiqueta != null ? $" El comentario es {contacto.Etiqueta} ({contacto.Puntuacion:N2})" : "");
                if (contacto.ViajeId != 0)
                    return RedirectToAction("Details", "Viajes", new { id = contacto.ViajeId });
                return RedirectToAction("Comentario");
            }
            // Recargar viajes comprados si hay error
            var viajesComprados = (from compra in _context.Compras
                                   where compra.UsuarioId == user.Id
                                   from detalle in compra.Detalles
                                   join viaje in _context.Viajes on detalle.ViajeId equals viaje.Id
                                   select viaje)
                                   .Distinct()
                                   .ToList();
            ViewBag.ViajesComprados = viajesComprados;
            return View("Comentario", contacto);
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using app1.Models;
using Microsoft.EntityFrameworkCore;

namespace app1.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly app1.Data.ApplicationDbContext _context;
        public LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, app1.Data.ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            var email = Request.Form["Input.Email"].ToString();
            var password = Request.Form["Input.Password"].ToString();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Debe ingresar email y contraseña.");
                return Page();
            }
            // Lógica de login original (delegar a Identity)
            var result = await _signInManager.PasswordSignInAsync(email, password, false, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                // Migrar reservas de sesión a usuario logueado
                var data = HttpContext.Session.GetString("ReservasSesion");
                if (!string.IsNullOrEmpty(data))
                {
                    var reservasSesion = JsonConvert.DeserializeObject<List<Reserva>>(data) ?? new List<Reserva>();
                    var user = await _userManager.GetUserAsync(User);
                    var userId = user?.Id;
                    if (userId != null)
                    {
                        foreach (var r in reservasSesion)
                        {
                            var reservaExistente = await _context.Reservas.FirstOrDefaultAsync(x => x.ClienteId == userId && x.ViajeId == r.ViajeId && x.FechaSalida == r.FechaSalida);
                            if (reservaExistente != null)
                            {
                                reservaExistente.Cantidad += r.Cantidad;
                                reservaExistente.FechaReserva = System.DateTime.UtcNow;
                            }
                            else
                            {
                                _context.Reservas.Add(new Reserva
                                {
                                    ClienteId = userId,
                                    ViajeId = r.ViajeId,
                                    Cantidad = r.Cantidad,
                                    FechaReserva = System.DateTime.UtcNow,
                                    FechaSalida = r.FechaSalida
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                        HttpContext.Session.SetString("ReservasSesion", JsonConvert.SerializeObject(new List<Reserva>()));
                    }
                }
                if (returnUrl != null)
                {
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    return RedirectToPage("/Index");
                }
            }
            // Si falla, mostrar error (opcional)
            return Page();
        }
    }
}

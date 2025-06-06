using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace app1.Models
{
    public class Reserva
    {
        public int Id { get; set; }
        [Required]
        public string ClienteId { get; set; }
        public IdentityUser Cliente { get; set; }
        [Required]
        public int ViajeId { get; set; }
        public Viaje Viaje { get; set; }
        [Required]
        public int Cantidad { get; set; }
        public DateTime FechaReserva { get; set; }
        [Required]
        public DateTime FechaSalida { get; set; }
        public int? FechaSalidaViajeId { get; set; }
        public FechaSalidaViaje? FechaSalidaViaje { get; set; }
    }
}

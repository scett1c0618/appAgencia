using System;
using System.ComponentModel.DataAnnotations;

namespace app1.Models
{
    public class Pago
    {
        public int Id { get; set; }
        public string NombreApellido { get; set; } = string.Empty;
        public string NumeroTarjeta { get; set; } = string.Empty;
        public string FechaVencimiento { get; set; } = string.Empty; // MM/AA
        public string CVV { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public DateTime FechaPago { get; set; }
        public decimal Monto { get; set; }
        public string? UsuarioId { get; set; }
        public string? UsuarioEmail { get; set; }
    }
}

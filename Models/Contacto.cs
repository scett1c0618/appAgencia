using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace app1.Models
{
    [Table("Contacto")]
    public class Contacto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string? Nombre { get; set; }
        public string? Correo { get; set; }
        [Required]
        public string? Mensaje { get; set; }
        public string? Telefono { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
        public string? Etiqueta { get; set; }
        public float Puntuacion { get; set; }
        public int? ViajeId { get; set; }
        public Viaje? Viaje { get; set; }
        public string? UserId { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace app1.Models
{
    public class FechaSalidaViaje
    {
        public int Id { get; set; }
        [Required]
        public int ViajeId { get; set; }
        public Viaje Viaje { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime FechaSalida { get; set; }
        [Required]
        [DataType(DataType.Time)]
        public TimeSpan HoraSalida { get; set; }
        [Required]
        public int AsientosDisponibles { get; set; }
    }
}

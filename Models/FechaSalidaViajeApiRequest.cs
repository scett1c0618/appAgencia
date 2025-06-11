using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace app1.Models
{
    /// <summary>
    /// Modelo para crear fechas de salida vía API.
    /// </summary>
    public class FechaSalidaViajeApiRequest
    {
        [Required]
        public int ViajeId { get; set; }
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

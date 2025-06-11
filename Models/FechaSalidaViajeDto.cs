using System;

namespace app1.Models
{
    /// <summary>
    /// DTO para exponer fechas de salida de un viaje en la API.
    /// </summary>
    public class FechaSalidaViajeDto
    {
        public int Id { get; set; }
        public DateTime FechaSalida { get; set; }
        public TimeSpan HoraSalida { get; set; }
        public int AsientosDisponibles { get; set; }
    }
}

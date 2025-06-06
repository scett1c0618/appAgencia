using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace app1.Models
{
    public class Viaje
    {
        public int Id { get; set; }
        [Required]
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        [DataType(DataType.Currency)]
        public decimal Precio { get; set; }
        public int DepartamentoId { get; set; }
        public Departamento? Departamento { get; set; }
        public string? Imagen { get; set; } // Permitir null para que la validación no lo exija
        public List<FechaSalidaViaje> FechasSalida { get; set; } = new();
    }
}

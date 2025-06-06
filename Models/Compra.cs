using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace app1.Models
{
    public class Compra
    {
        public int Id { get; set; }
        public string? UsuarioId { get; set; } // null si es anónimo
        public string? UsuarioEmail { get; set; } // null si es anónimo
        public DateTime FechaCompra { get; set; }
        public decimal MontoTotal { get; set; }
        public List<DetalleCompra> Detalles { get; set; } = new();
    }

    public class DetalleCompra
    {
        public int Id { get; set; }
        public int CompraId { get; set; }
        public Compra Compra { get; set; }
        public int ViajeId { get; set; }
        public string ViajeTitulo { get; set; }
        public DateTime FechaSalida { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}

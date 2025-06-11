using Microsoft.AspNetCore.Http;

namespace app1.Models
{
    /// <summary>
    /// Modelo para crear viajes vía API.
    /// </summary>
    public class ViajeApiRequest
    {
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int DepartamentoId { get; set; }
        public IFormFile? Imagen { get; set; } // Soporte para archivos en Swagger
    }
}

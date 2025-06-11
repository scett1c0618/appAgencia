using Microsoft.AspNetCore.Http;

namespace app1.Models
{
    /// <summary>
    /// Modelo para crear departamentos vía API.
    /// </summary>
    public class DepartamentoApiRequest
    {
        public string? Nombre { get; set; }
        public IFormFile? Imagen { get; set; }
    }
}

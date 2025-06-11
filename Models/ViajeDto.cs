namespace app1.Models
{
    /// <summary>
    /// DTO para exponer viajes en la API.
    /// </summary>
    public class ViajeDto
    {
        public int Id { get; set; }
        public string? Titulo { get; set; }
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string? Departamento { get; set; }
        public string? Imagen { get; set; }
    }
}

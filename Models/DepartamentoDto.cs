namespace app1.Models
{
    /// <summary>
    /// DTO para exponer departamentos en la API.
    /// </summary>
    public class DepartamentoDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Imagen { get; set; }
    }
}

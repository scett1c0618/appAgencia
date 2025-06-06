using System.ComponentModel.DataAnnotations;

namespace app1.Models
{
    public class Direccion
    {
        public int Id { get; set; }
        [Required]
        public string ClienteId { get; set; }
        [Required]
        public string Departamento { get; set; }
        [Required]
        public string Provincia { get; set; }
        [Required]
        public string Distrito { get; set; }
        [Required]
        public string DireccionTexto { get; set; }
    }
}

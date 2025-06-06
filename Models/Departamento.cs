using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace app1.Models
{
    public class Departamento
    {
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }
        public string Imagen { get; set; } // Ruta o nombre de archivo de la imagen
        public ICollection<Viaje> Viajes { get; set; }
    }
}

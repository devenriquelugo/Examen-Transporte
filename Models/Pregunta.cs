using Microsoft.Extensions.Options;

namespace ExamenTransporte.Models
{
    public class Pregunta
    {
        public int Id { get; set; }
        public int ExamenId { get; set; }
        public string TextoPregunta { get; set; }
        public int OrdenPregunta { get; set; }
        public decimal Puntos { get; set; }

        // Navegación
        public Examen Examen { get; set; }
        public List<Opcion> Opciones { get; set; }
    }
}

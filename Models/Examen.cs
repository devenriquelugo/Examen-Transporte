namespace ExamenTransporte.Models
{
    public class Examen
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }

        // Navegación
        public List<Pregunta> Preguntas { get; set; }
    }
}

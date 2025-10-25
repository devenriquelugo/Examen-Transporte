namespace ExamenTransporte.Models
{
    public class ExamenRealizado
    {
        public int Id { get; set; }
        public int ExamenId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? TiempoTranscurridoMinutos { get; set; }
        public decimal? Puntuacion { get; set; }
        public decimal? PuntuacionMaxima { get; set; }
        public bool Completado { get; set; }

        // Navegación
        public Examen Examen { get; set; }
        public List<Respuesta> Respuestas { get; set; }
    }
}

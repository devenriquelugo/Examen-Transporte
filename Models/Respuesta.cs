namespace ExamenTransporte.Models
{
    public class Respuesta
    {
        public int Id { get; set; }
        public int ExamenRealizadoId { get; set; }
        public int PreguntaId { get; set; }
        public int OpcionSeleccionadaId { get; set; }
        public bool EsCorrecta { get; set; }
        public DateTime FechaRespuesta { get; set; }

        // Navegación
        public ExamenRealizado ExamenRealizado { get; set; }
        public Pregunta Pregunta { get; set; }
        public Opcion OpcionSeleccionada { get; set; }
    }
}

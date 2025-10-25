namespace ExamenTransporte.Models
{
    public class ExamenViewModel
    {
        public int ExamenRealizadoId { get; set; }
        public int ExamenId { get; set; }
        public string TituloExamen { get; set; }
        public int PreguntaActual { get; set; }
        public int TotalPreguntas { get; set; }
        public PreguntaExamenViewModel Pregunta { get; set; }
        public bool PuedeRetroceder { get; set; }
        public bool MostroRespuesta { get; set; }
        public int? OpcionSeleccionada { get; set; }
        public bool? EsCorrecta { get; set; }
    }

    public class PreguntaExamenViewModel
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Texto { get; set; }
        public List<OpcionViewModel> Opciones { get; set; }
        public int? OpcionCorrectaId { get; set; }
    }

    public class OpcionViewModel
    {
        public int Id { get; set; }
        public string Letra { get; set; }
        public string Texto { get; set; }
        public bool EsCorrecta { get; set; }
    }
}
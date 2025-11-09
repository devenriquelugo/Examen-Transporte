namespace ExamenTransporte.Models
{
    public class ResultadoExamenViewModel
    {
        public int ExamenRealizadoId { get; set; }
        public string TituloExamen { get; set; }
        public int TotalPreguntas { get; set; }
        public int Correctas { get; set; }
        public int Incorrectas { get; set; }
        public double Porcentaje { get; set; }
        public bool Aprobado { get; set; }
    }
}

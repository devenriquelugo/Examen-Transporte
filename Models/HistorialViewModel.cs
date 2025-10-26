namespace ExamenTransporte.Models
{
    public class HistorialExamenViewModel
    {
        public int ExamenId { get; set; }
        public string TituloExamen { get; set; }
        public int TotalIntentos { get; set; }
        public DateTime UltimoIntento { get; set; }
    }

    public class IntentoExamenViewModel
    {
        public int ExamenRealizadoId { get; set; }
        public DateTime FechaRealizacion { get; set; }
        public int TotalPreguntas { get; set; }
        public int Correctas { get; set; }
        public int Incorrectas { get; set; }
        public double Porcentaje { get; set; }
    }

    public class RespuestaDetalleViewModel
    {
        public int NumeroPregunta { get; set; }
        public string TextoPregunta { get; set; }
        public string RespuestaUsuario { get; set; }
        public bool EsCorrecta { get; set; }
        public string RespuestaCorrecta { get; set; }
        public DateTime FechaRespuesta { get; set; }
    }

    public class DetalleIntentoViewModel
    {
        public int ExamenRealizadoId { get; set; }
        public int ExamenId { get; set; } // NUEVO
        public string TituloExamen { get; set; }
        public DateTime FechaRealizacion { get; set; }
        public int TotalPreguntas { get; set; }
        public int Correctas { get; set; }
        public int Incorrectas { get; set; }
        public double Porcentaje { get; set; }
        public string Filtro { get; set; }
        public List<RespuestaDetalleViewModel> Respuestas { get; set; }
    }
}
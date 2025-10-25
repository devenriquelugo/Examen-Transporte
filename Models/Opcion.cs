namespace ExamenTransporte.Models
{
    public class Opcion
    {
        public int Id { get; set; }
        public int PreguntaId { get; set; }
        public string TextoOpcion { get; set; }
        public bool EsCorrecta { get; set; }
        public int OrdenOpcion { get; set; }

        // Navegación
        public Pregunta Pregunta { get; set; }
    }
}

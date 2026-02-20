namespace ExamenTransporte.Models
{
    public class CargaExamenViewModel
    {
        // Modelo 1: 2 archivos (preguntas + respuestas)
        public IFormFile ArchivoExamen { get; set; }
        public IFormFile ArchivoRespuestas { get; set; }

        // Modelo 2: 1 archivo con todo incluido
        public IFormFile ArchivoCompleto { get; set; }

        public string Mensaje { get; set; }
        public bool Exito { get; set; }

        // Para identificar qué modelo se está usando
        public int ModeloCarga { get; set; } // 1 o 2
    }
}

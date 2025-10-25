namespace ExamenTransporte.Models
{
    public class CargaExamenViewModel
    {
        public IFormFile ArchivoExamen { get; set; }
        public IFormFile ArchivoRespuestas { get; set; }
        public string Mensaje { get; set; }
        public bool Exito { get; set; }
    }
}

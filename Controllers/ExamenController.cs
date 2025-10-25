using Microsoft.AspNetCore.Mvc;
using ExamenTransporte.Data;
using ExamenTransporte.Models;

namespace ExamenTransporte.Controllers
{
    public class ExamenController : Controller
    {
        private readonly ExamenRepository _repository;
        private const string SESSION_KEY = "ExamenActual";

        public ExamenController(ExamenRepository repository)
        {
            _repository = repository;
        }

        // Listado de exámenes disponibles
        public IActionResult Index()
        {
            var examenes = _repository.ObtenerListaExamenes();
            return View(examenes);
        }

        // Iniciar un examen
        public IActionResult Iniciar(int id)
        {
            // Crear registro de examen realizado
            int examenRealizadoId = _repository.IniciarExamen(id);

            // Guardar en sesión
            var sesion = new SesionExamen
            {
                ExamenId = id,
                ExamenRealizadoId = examenRealizadoId,
                PreguntaActual = 1,
                RespuestasComprobadas = new Dictionary<int, bool>()
            };

            HttpContext.Session.SetString(SESSION_KEY, System.Text.Json.JsonSerializer.Serialize(sesion));

            return RedirectToAction("Pregunta");
        }

        // Mostrar pregunta actual
        public IActionResult Pregunta()
        {
            var sesionJson = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrEmpty(sesionJson))
            {
                return RedirectToAction("Index");
            }

            var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionExamen>(sesionJson);

            var pregunta = _repository.ObtenerPregunta(sesion.ExamenId, sesion.PreguntaActual);
            if (pregunta == null)
            {
                return RedirectToAction("Finalizar");
            }

            var modelo = new ExamenViewModel
            {
                ExamenRealizadoId = sesion.ExamenRealizadoId,
                ExamenId = sesion.ExamenId,
                TituloExamen = _repository.ObtenerTituloExamen(sesion.ExamenId),
                PreguntaActual = sesion.PreguntaActual,
                TotalPreguntas = _repository.ObtenerTotalPreguntas(sesion.ExamenId),
                Pregunta = pregunta,
                PuedeRetroceder = sesion.PreguntaActual > 1,
                MostroRespuesta = sesion.RespuestasComprobadas.ContainsKey(sesion.PreguntaActual)
            };

            return View(modelo);
        }

        // Comprobar respuesta
        [HttpPost]
        public IActionResult Comprobar(int opcionId)
        {
            var sesionJson = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrEmpty(sesionJson))
            {
                return RedirectToAction("Index");
            }

            var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionExamen>(sesionJson);
            var pregunta = _repository.ObtenerPregunta(sesion.ExamenId, sesion.PreguntaActual);

            bool esCorrecta = pregunta.OpcionCorrectaId == opcionId;

            // Guardar respuesta en BD
            _repository.GuardarRespuesta(sesion.ExamenRealizadoId, pregunta.Id, opcionId, esCorrecta);

            // Marcar como comprobada
            sesion.RespuestasComprobadas[sesion.PreguntaActual] = esCorrecta;
            HttpContext.Session.SetString(SESSION_KEY, System.Text.Json.JsonSerializer.Serialize(sesion));

            var modelo = new ExamenViewModel
            {
                ExamenRealizadoId = sesion.ExamenRealizadoId,
                ExamenId = sesion.ExamenId,
                TituloExamen = _repository.ObtenerTituloExamen(sesion.ExamenId),
                PreguntaActual = sesion.PreguntaActual,
                TotalPreguntas = _repository.ObtenerTotalPreguntas(sesion.ExamenId),
                Pregunta = pregunta,
                PuedeRetroceder = sesion.PreguntaActual > 1,
                MostroRespuesta = true,
                OpcionSeleccionada = opcionId,
                EsCorrecta = esCorrecta
            };

            return View("Pregunta", modelo);
        }

        // Ir a pregunta anterior
        [HttpPost]
        public IActionResult Anterior()
        {
            var sesionJson = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrEmpty(sesionJson))
            {
                return RedirectToAction("Index");
            }

            var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionExamen>(sesionJson);

            if (sesion.PreguntaActual > 1)
            {
                sesion.PreguntaActual--;
                HttpContext.Session.SetString(SESSION_KEY, System.Text.Json.JsonSerializer.Serialize(sesion));
            }

            return RedirectToAction("Pregunta");
        }

        // Ir a pregunta siguiente
        [HttpPost]
        public IActionResult Siguiente()
        {
            var sesionJson = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrEmpty(sesionJson))
            {
                return RedirectToAction("Index");
            }

            var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionExamen>(sesionJson);

            int totalPreguntas = _repository.ObtenerTotalPreguntas(sesion.ExamenId);

            if (sesion.PreguntaActual < totalPreguntas)
            {
                sesion.PreguntaActual++;
                HttpContext.Session.SetString(SESSION_KEY, System.Text.Json.JsonSerializer.Serialize(sesion));
                return RedirectToAction("Pregunta");
            }
            else
            {
                return RedirectToAction("Finalizar");
            }
        }

        // Finalizar examen
        public IActionResult Finalizar()
        {
            HttpContext.Session.Remove(SESSION_KEY);
            return View();
        }
    }

    // Clase para guardar estado en sesión
    public class SesionExamen
    {
        public int ExamenId { get; set; }
        public int ExamenRealizadoId { get; set; }
        public int PreguntaActual { get; set; }
        public Dictionary<int, bool> RespuestasComprobadas { get; set; }
    }
}
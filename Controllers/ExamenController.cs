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
                RespuestasComprobadas = new Dictionary<int, bool>(),
                OpcionesSeleccionadas = new Dictionary<int, int>() // NUEVO
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

            // NUEVO: Recuperar la opción seleccionada si existe
            int? opcionSeleccionada = null;
            if (sesion.OpcionesSeleccionadas.ContainsKey(sesion.PreguntaActual))
            {
                opcionSeleccionada = sesion.OpcionesSeleccionadas[sesion.PreguntaActual];
            }

            // NUEVO: Verificar si es correcta (si ya fue comprobada)
            bool? esCorrecta = null;
            if (sesion.RespuestasComprobadas.ContainsKey(sesion.PreguntaActual))
            {
                esCorrecta = sesion.RespuestasComprobadas[sesion.PreguntaActual];
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
                MostroRespuesta = sesion.RespuestasComprobadas.ContainsKey(sesion.PreguntaActual),
                OpcionSeleccionada = opcionSeleccionada, // NUEVO
                EsCorrecta = esCorrecta // NUEVO
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
            // NUEVO: Guardar la opción seleccionada
            sesion.OpcionesSeleccionadas[sesion.PreguntaActual] = opcionId;

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
        public IActionResult Siguiente(int? opcionId)
        {
            var sesionJson = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrEmpty(sesionJson))
            {
                return RedirectToAction("Index");
            }

            var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionExamen>(sesionJson);

            // NUEVO: Guardar la opción seleccionada en sesión
            if (opcionId.HasValue && opcionId.Value > 0)
            {
                sesion.OpcionesSeleccionadas[sesion.PreguntaActual] = opcionId.Value;

                // NUEVO: Si NO se ha comprobado esta respuesta, guardarla en BD ahora
                if (!sesion.RespuestasComprobadas.ContainsKey(sesion.PreguntaActual))
                {
                    var pregunta = _repository.ObtenerPregunta(sesion.ExamenId, sesion.PreguntaActual);

                    if (pregunta != null)
                    {
                        bool esCorrecta = pregunta.OpcionCorrectaId == opcionId.Value;

                        // Guardar en base de datos
                        _repository.GuardarRespuesta(sesion.ExamenRealizadoId, pregunta.Id, opcionId.Value, esCorrecta);

                        // Marcar como guardada (aunque no se haya "comprobado" visualmente)
                        sesion.RespuestasComprobadas[sesion.PreguntaActual] = esCorrecta;
                    }
                }
            }

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

        // Ver historial general
        public IActionResult Historial()
        {
            var historial = _repository.ObtenerHistorialExamenes();
            return View(historial);
        }

        // Ver intentos de un examen específico
        public IActionResult IntentosExamen(int id)
        {
            var intentos = _repository.ObtenerIntentosExamen(id);
            ViewBag.TituloExamen = _repository.ObtenerTituloExamen(id);
            ViewBag.ExamenId = id;
            return View(intentos);
        }

        // Ver detalle de un intento con filtro
        public IActionResult DetalleIntento(int id, string filtro = "todas")
        {
            // PRIMERO: Obtener TODAS las respuestas para estadísticas correctas
            var todasLasRespuestas = _repository.ObtenerRespuestasIntento(id, "todas");

            // SEGUNDO: Obtener las respuestas filtradas para mostrar
            var respuestasFiltradas = filtro == "todas"
                ? todasLasRespuestas
                : _repository.ObtenerRespuestasIntento(id, filtro);

            var primerRespuesta = todasLasRespuestas.FirstOrDefault();

            // Obtener ExamenId y total de preguntas
            int examenId = _repository.ObtenerExamenIdPorIntento(id);
            int totalPreguntasExamen = _repository.ObtenerTotalPreguntas(examenId);

            // Calcular estadísticas basadas en TODAS las respuestas (sin filtro)
            int respondidas = todasLasRespuestas.Count;
            int correctas = todasLasRespuestas.Count(r => r.EsCorrecta);
            int incorrectas = todasLasRespuestas.Count(r => !r.EsCorrecta);
            double porcentaje = totalPreguntasExamen > 0 ? Math.Round((double)correctas / totalPreguntasExamen * 100, 2) : 0;

            var modelo = new DetalleIntentoViewModel
            {
                ExamenRealizadoId = id,
                ExamenId = examenId,
                TituloExamen = _repository.ObtenerTituloExamenPorIntento(id),
                FechaRealizacion = primerRespuesta?.FechaRespuesta ?? DateTime.Now,
                TotalPreguntasExamen = totalPreguntasExamen,
                Respondidas = respondidas,
                Correctas = correctas,
                Incorrectas = incorrectas,
                Porcentaje = porcentaje,
                Filtro = filtro,
                Respuestas = respuestasFiltradas
            };

            return View(modelo);
        }

        // Retomar un examen incompleto
        public IActionResult Retomar(int examenRealizadoId)
        {
            // Obtener el ExamenId del intento
            int examenId = _repository.ObtenerExamenIdPorIntento(examenRealizadoId);

            if (examenId == 0)
            {
                TempData["Error"] = "No se encontró el examen";
                return RedirectToAction("Historial");
            }

            // Verificar si ya está completo
            bool estaCompleto = _repository.ExamenEstaCompleto(examenRealizadoId, examenId);

            if (estaCompleto)
            {
                TempData["Error"] = "Este examen ya ha sido completado";
                return RedirectToAction("IntentosExamen", new { id = examenId });
            }

            // Obtener las respuestas ya guardadas
            var opcionesSeleccionadas = _repository.ObtenerRespuestasGuardadas(examenRealizadoId, examenId);
            var respuestasComprobadas = _repository.ObtenerRespuestasComprobadas(examenRealizadoId, examenId);

            // Obtener la primera pregunta sin responder
            int preguntaActual = _repository.ObtenerPrimeraPreguntaSinResponder(examenRealizadoId, examenId);

            // Crear sesión con el estado guardado
            var sesion = new SesionExamen
            {
                ExamenId = examenId,
                ExamenRealizadoId = examenRealizadoId,
                PreguntaActual = preguntaActual,
                RespuestasComprobadas = respuestasComprobadas,
                OpcionesSeleccionadas = opcionesSeleccionadas
            };

            HttpContext.Session.SetString(SESSION_KEY, System.Text.Json.JsonSerializer.Serialize(sesion));

            return RedirectToAction("Pregunta");
        }

        public IActionResult Finalizar()
        {
            var sesionJson = HttpContext.Session.GetString(SESSION_KEY);
            if (string.IsNullOrEmpty(sesionJson))
            {
                return RedirectToAction("Index");
            }

            var sesion = System.Text.Json.JsonSerializer.Deserialize<SesionExamen>(sesionJson);

            // Calcular estadísticas
            int totalPreguntas = _repository.ObtenerTotalPreguntas(sesion.ExamenId);
            int correctas = sesion.RespuestasComprobadas.Count(r => r.Value == true);
            int incorrectas = sesion.RespuestasComprobadas.Count(r => r.Value == false);
            double porcentaje = totalPreguntas > 0 ? Math.Round((double)correctas / totalPreguntas * 100, 2) : 0;

            var modelo = new ResultadoExamenViewModel
            {
                ExamenRealizadoId = sesion.ExamenRealizadoId,
                TituloExamen = _repository.ObtenerTituloExamen(sesion.ExamenId),
                TotalPreguntas = totalPreguntas,
                Correctas = correctas,
                Incorrectas = incorrectas,
                Porcentaje = porcentaje,
                Aprobado = porcentaje >= 70
            };

            // Limpiar sesión
            HttpContext.Session.Remove(SESSION_KEY);

            return View(modelo);
        }
    }

    // Clase para guardar estado en sesión
    public class SesionExamen
    {
        public int ExamenId { get; set; }
        public int ExamenRealizadoId { get; set; }
        public int PreguntaActual { get; set; }
        public Dictionary<int, bool> RespuestasComprobadas { get; set; }

        public Dictionary<int, int> OpcionesSeleccionadas { get; set; }
    }
}
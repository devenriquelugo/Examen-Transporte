using Microsoft.AspNetCore.Mvc;
using ExamenTransporte.Models;
using ExamenTransporte.Data;
using System.Text.RegularExpressions;
using Xceed.Words.NET;

namespace ExamenTransporte.Controllers
{
    public class CargaController : Controller
    {
        private readonly ExamenRepository _repository;

        public CargaController(ExamenRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View(new CargaExamenViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CargarArchivos(CargaExamenViewModel model)
        {
            if (model.ArchivoExamen == null || model.ArchivoRespuestas == null)
            {
                model.Mensaje = "Debe seleccionar ambos archivos .docx";
                model.Exito = false;
                return View("Index", model);
            }

            try
            {
                var datosExamen = await ProcesarArchivoExamen(model.ArchivoExamen);
                var respuestas = await ProcesarArchivoRespuestas(model.ArchivoRespuestas);

                int examenId = _repository.GuardarExamen(datosExamen.Titulo);

                foreach (var pregunta in datosExamen.Preguntas)
                {
                    string respuestaCorrecta = respuestas.ContainsKey(pregunta.Numero)
                        ? respuestas[pregunta.Numero]
                        : "";

                    _repository.GuardarPregunta(examenId, pregunta, respuestaCorrecta);
                }

                model.Mensaje = $"Examen '{datosExamen.Titulo}' cargado correctamente con {datosExamen.Preguntas.Count} preguntas";
                model.Exito = true;
            }
            catch (Exception ex)
            {
                model.Mensaje = $"Error: {ex.Message}";
                model.Exito = false;
            }

            return View("Index", model);
        }

        private async Task<DatosExamen> ProcesarArchivoExamen(IFormFile archivo)
        {
            var resultado = new DatosExamen();

            using (var stream = archivo.OpenReadStream())
            {
                using (var doc = DocX.Load(stream))
                {
                    string textoCompleto = doc.Text;

                    Console.WriteLine($"Longitud del texto: {textoCompleto.Length} caracteres");

                    // EXTRAER TÍTULO
                    var matchTitulo = Regex.Match(textoCompleto, @"^(.*?)(\d+[\s\.\-]+Pregunta:)", RegexOptions.Singleline);

                    if (matchTitulo.Success)
                    {
                        resultado.Titulo = matchTitulo.Groups[1].Value.Trim();
                        if (resultado.Titulo.Length > 200)
                        {
                            resultado.Titulo = resultado.Titulo.Substring(0, 200);
                        }
                    }
                    else
                    {
                        resultado.Titulo = "Examen sin título";
                    }

                    Console.WriteLine($"Título: {resultado.Titulo}");

                    // EXTRAER PREGUNTAS
                    var patronPreguntas = @"(\d+)[\s\.\-]+Pregunta:\s*(.+?)(?=\d+[\s\.\-]+Pregunta:|$)";
                    var matchesPreguntas = Regex.Matches(textoCompleto, patronPreguntas, RegexOptions.Singleline);

                    Console.WriteLine($"Bloques de preguntas encontrados: {matchesPreguntas.Count}");

                    foreach (Match matchPregunta in matchesPreguntas)
                    {
                        int numeroPregunta = int.Parse(matchPregunta.Groups[1].Value);
                        string bloqueCompleto = matchPregunta.Groups[2].Value;

                        var preguntaData = new PreguntaData
                        {
                            Numero = numeroPregunta,
                            Texto = "",
                            Opciones = new Dictionary<string, string>()
                        };

                        // PASO 1: Extraer texto de la pregunta
                        // CLAVE: Buscar texto que termine con signo de puntuación [?.:!] ANTES de la opción A
                        var matchInicioOpciones = Regex.Match(bloqueCompleto, @"^(.+?[?.:!])\s*(?<![A-Za-z])A\s+", RegexOptions.Singleline);

                        if (matchInicioOpciones.Success)
                        {
                            preguntaData.Texto = matchInicioOpciones.Groups[1].Value.Trim();
                        }
                        else
                        {
                            // Plan B: Si no encuentra, buscar sin requerir signo de puntuación
                            var matchPlanB = Regex.Match(bloqueCompleto, @"^(.+?)(?<![A-Za-z])A\s+", RegexOptions.Singleline);

                            if (matchPlanB.Success)
                            {
                                preguntaData.Texto = matchPlanB.Groups[1].Value.Trim();
                            }
                            else
                            {
                                preguntaData.Texto = bloqueCompleto.Length > 200
                                    ? bloqueCompleto.Substring(0, 200).Trim()
                                    : bloqueCompleto.Trim();
                            }
                        }

                        // VALIDACIÓN: Si el texto es muy corto (< 15 chars), probablemente falló
                        if (preguntaData.Texto.Length < 15)
                        {
                            Console.WriteLine($"⚠ Pregunta {numeroPregunta} - Texto sospechosamente corto: '{preguntaData.Texto}'");

                            // Buscar la SEGUNDA ocurrencia de "A " (la primera es parte de la pregunta)
                            var matchesA = Regex.Matches(bloqueCompleto, @"(?<![A-Za-z])A\s+");

                            if (matchesA.Count >= 2)
                            {
                                // Tomar texto hasta la SEGUNDA "A "
                                int posSegundaA = matchesA[1].Index;
                                preguntaData.Texto = bloqueCompleto.Substring(0, posSegundaA).Trim();
                                Console.WriteLine($"  ✓ Corregido usando segunda A: '{preguntaData.Texto.Substring(0, Math.Min(80, preguntaData.Texto.Length))}...'");
                            }
                        }

                        // PASO 2: Extraer opciones
                        // CLAVE: Buscar opciones DESPUÉS de donde termina el texto de la pregunta
                        string textoOpciones = bloqueCompleto;

                        // Si encontramos el texto de la pregunta, buscar opciones después de él
                        if (!string.IsNullOrWhiteSpace(preguntaData.Texto) && bloqueCompleto.Contains(preguntaData.Texto))
                        {
                            int finPregunta = bloqueCompleto.IndexOf(preguntaData.Texto) + preguntaData.Texto.Length;
                            if (finPregunta < bloqueCompleto.Length)
                            {
                                textoOpciones = bloqueCompleto.Substring(finPregunta);
                            }
                        }

                        // Ahora buscar opciones en textoOpciones (que ya no contiene el texto de la pregunta)
                        var matchA = Regex.Match(textoOpciones, @"(?<![A-Za-z])A\s+(.+?)(?=(?<![A-Za-z])B\s+|$)", RegexOptions.Singleline);
                        if (matchA.Success)
                        {
                            preguntaData.Opciones["A"] = matchA.Groups[1].Value.Trim();
                        }

                        var matchB = Regex.Match(textoOpciones, @"(?<![A-Za-z])B\s+(.+?)(?=(?<![A-Za-z])C\s+|$)", RegexOptions.Singleline);
                        if (matchB.Success)
                        {
                            preguntaData.Opciones["B"] = matchB.Groups[1].Value.Trim();
                        }

                        var matchC = Regex.Match(textoOpciones, @"(?<![A-Za-z])C\s+(.+?)(?=(?<![A-Za-z])D\s+|$)", RegexOptions.Singleline);
                        if (matchC.Success)
                        {
                            preguntaData.Opciones["C"] = matchC.Groups[1].Value.Trim();
                        }

                        var matchD = Regex.Match(textoOpciones, @"(?<![A-Za-z])D\s+(.+?)$", RegexOptions.Singleline);
                        if (matchD.Success)
                        {
                            preguntaData.Opciones["D"] = matchD.Groups[1].Value.Trim();
                        }

                        // Validar que tenga las 4 opciones
                        if (!string.IsNullOrWhiteSpace(preguntaData.Texto) &&
                            preguntaData.Opciones.Count >= 4 &&
                            preguntaData.Texto.Length >= 15)  // Al menos 15 caracteres para ser válido
                        {
                            resultado.Preguntas.Add(preguntaData);
                        }
                        else
                        {
                            Console.WriteLine($"Pregunta {numeroPregunta} descartada - Texto length: {preguntaData.Texto.Length}, Opciones: {preguntaData.Opciones.Count}");
                        }
                    }

                    Console.WriteLine($"Total preguntas procesadas: {resultado.Preguntas.Count}");

                    if (resultado.Preguntas.Count == 0)
                    {
                        throw new Exception("No se encontraron preguntas válidas en el archivo");
                    }
                }
            }

            return resultado;
        }

        private async Task<Dictionary<int, string>> ProcesarArchivoRespuestas(IFormFile archivo)
        {
            var respuestas = new Dictionary<int, string>();

            using (var stream = archivo.OpenReadStream())
            {
                using (var doc = DocX.Load(stream))
                {
                    string textoCompleto = doc.Text;

                    Console.WriteLine($"Procesando respuestas, longitud: {textoCompleto.Length}");

                    // Buscar patrones como "1 B", "2 C", "3 A", etc.
                    var patronRespuestas = @"(\d+)[\.\s]+([A-D])";
                    var matches = Regex.Matches(textoCompleto, patronRespuestas);

                    Console.WriteLine($"Respuestas encontradas: {matches.Count}");

                    foreach (Match match in matches)
                    {
                        int numeroPregunta = int.Parse(match.Groups[1].Value);
                        string respuesta = match.Groups[2].Value;

                        respuestas[numeroPregunta] = respuesta;
                    }
                }
            }

            return respuestas;
        }
    }

    // Clases auxiliares (DTOs)
    public class DatosExamen
    {
        public string Titulo { get; set; }
        public List<PreguntaData> Preguntas { get; set; } = new List<PreguntaData>();
    }

    public class PreguntaData
    {
        public int Numero { get; set; }
        public string Texto { get; set; }
        public Dictionary<string, string> Opciones { get; set; }
    }
}
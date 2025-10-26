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
                Console.WriteLine("\n========== INICIANDO CARGA DE EXAMEN ==========");
                Console.WriteLine($"Archivo examen: {model.ArchivoExamen.FileName}");
                Console.WriteLine($"Archivo respuestas: {model.ArchivoRespuestas.FileName}");

                Console.WriteLine("\n--- PASO 1: Procesando archivo de preguntas ---");
                var datosExamen = await ProcesarArchivoExamen(model.ArchivoExamen);
                Console.WriteLine($"✓ Examen procesado: '{datosExamen.Titulo}' con {datosExamen.Preguntas.Count} preguntas");

                Console.WriteLine("\n--- PASO 2: Procesando archivo de respuestas ---");
                var respuestas = await ProcesarArchivoRespuestas(model.ArchivoRespuestas);
                Console.WriteLine($"✓ Respuestas procesadas: {respuestas.Count}");

                Console.WriteLine("\n--- PASO 3: Guardando en base de datos ---");
                int examenId = _repository.GuardarExamen(datosExamen.Titulo);
                Console.WriteLine($"✓ Examen guardado con ID: {examenId}");

                int preguntasGuardadas = 0;
                foreach (var pregunta in datosExamen.Preguntas)
                {
                    try
                    {
                        string respuestaCorrecta = respuestas.ContainsKey(pregunta.Numero)
                            ? respuestas[pregunta.Numero]
                            : "";

                        Console.WriteLine($"\nGuardando pregunta {pregunta.Numero} (respuesta correcta: '{respuestaCorrecta}')...");
                        _repository.GuardarPregunta(examenId, pregunta, respuestaCorrecta);
                        preguntasGuardadas++;
                        Console.WriteLine($"  ✓ Guardada correctamente");
                    }
                    catch (Exception exPregunta)
                    {
                        Console.WriteLine($"✗ Error guardando pregunta {pregunta.Numero}:");
                        Console.WriteLine($"  Mensaje: {exPregunta.Message}");
                        Console.WriteLine($"  Tipo: {exPregunta.GetType().FullName}");
                        Console.WriteLine($"  Stack: {exPregunta.StackTrace}");
                        throw; // Re-lanzar para detener el proceso
                    }
                }

                Console.WriteLine($"\n✓ Total preguntas guardadas: {preguntasGuardadas}");
                Console.WriteLine("========== CARGA COMPLETADA ==========\n");

                model.Mensaje = $"Examen '{datosExamen.Titulo}' cargado correctamente con {datosExamen.Preguntas.Count} preguntas";
                model.Exito = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗✗✗ ERROR CRÍTICO ✗✗✗");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Tipo completo: {ex.GetType().FullName}");
                Console.WriteLine($"Stack trace completo:");
                Console.WriteLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"\n--- Inner Exception ---");
                    Console.WriteLine($"Mensaje: {ex.InnerException.Message}");
                    Console.WriteLine($"Tipo: {ex.InnerException.GetType().FullName}");
                    Console.WriteLine($"Stack: {ex.InnerException.StackTrace}");
                }

                model.Mensaje = $"Error: {ex.Message} (Ver consola para detalles)";
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
                    Console.WriteLine($"Primeras 500 caracteres:\n{textoCompleto.Substring(0, Math.Min(500, textoCompleto.Length))}");

                    // USAR NOMBRE DEL ARCHIVO COMO TÍTULO (sin extensión)
                    resultado.Titulo = Path.GetFileNameWithoutExtension(archivo.FileName);

                    // Limpiar el título: remover extensiones comunes y espacios extras
                    resultado.Titulo = resultado.Titulo.Trim();

                    if (resultado.Titulo.Length > 200)
                    {
                        resultado.Titulo = resultado.Titulo.Substring(0, 200);
                    }

                    Console.WriteLine($"Título extraído del nombre del archivo: '{resultado.Titulo}'");

                    // EXTRAER PREGUNTAS - PATRÓN UNIVERSAL
                    var patronPreguntas = @"(?:\d+\.?\s*-?\s*)?Pregunta:\s*(.+?)(?=(?:\d+\.?\s*-?\s*)?Pregunta:|$)";
                    var matchesPreguntas = Regex.Matches(textoCompleto, patronPreguntas, RegexOptions.Singleline);

                    Console.WriteLine($"Bloques de preguntas encontrados: {matchesPreguntas.Count}");

                    int numeroPreguntaReal = 1;

                    foreach (Match matchPregunta in matchesPreguntas)
                    {
                        try
                        {
                            Console.WriteLine($"\n=== Procesando Pregunta {numeroPreguntaReal} ===");

                            string bloqueCompleto = matchPregunta.Groups[1].Value;

                            // Solo mostrar log detallado para las primeras 5 preguntas
                            if (numeroPreguntaReal <= 5)
                            {
                                Console.WriteLine($"Bloque completo (primeros 200 chars): {bloqueCompleto.Substring(0, Math.Min(200, bloqueCompleto.Length))}...");
                            }

                            var preguntaData = new PreguntaData
                            {
                                Numero = numeroPreguntaReal,
                                Texto = "",
                                Opciones = new Dictionary<string, string>()
                            };

                            // PASO 1: Extraer texto de la pregunta
                            var matchInicioOpciones = Regex.Match(bloqueCompleto, @"^(.+?[?.:!])\s*(?<![A-Za-z])A\s+", RegexOptions.Singleline);

                            if (matchInicioOpciones.Success)
                            {
                                preguntaData.Texto = matchInicioOpciones.Groups[1].Value.Trim();
                                if (numeroPreguntaReal <= 5)
                                {
                                    Console.WriteLine($"✓ Texto extraído: {preguntaData.Texto.Substring(0, Math.Min(80, preguntaData.Texto.Length))}...");
                                }
                            }
                            else
                            {
                                var matchPlanB = Regex.Match(bloqueCompleto, @"^(.+?)(?<![A-Za-z])A\s+", RegexOptions.Singleline);

                                if (matchPlanB.Success)
                                {
                                    preguntaData.Texto = matchPlanB.Groups[1].Value.Trim();
                                    if (numeroPreguntaReal <= 5)
                                    {
                                        Console.WriteLine($"✓ Texto extraído (Plan B): {preguntaData.Texto.Substring(0, Math.Min(80, preguntaData.Texto.Length))}...");
                                    }
                                }
                                else
                                {
                                    int posA = bloqueCompleto.IndexOf(" A ");
                                    if (posA > 0)
                                    {
                                        preguntaData.Texto = bloqueCompleto.Substring(0, posA).Trim();
                                        if (numeroPreguntaReal <= 5)
                                        {
                                            Console.WriteLine($"✓ Texto extraído (Plan C): {preguntaData.Texto.Substring(0, Math.Min(80, preguntaData.Texto.Length))}...");
                                        }
                                    }
                                    else
                                    {
                                        preguntaData.Texto = bloqueCompleto.Length > 200
                                            ? bloqueCompleto.Substring(0, 200).Trim()
                                            : bloqueCompleto.Trim();
                                        Console.WriteLine($"⚠ Texto extraído (fallback): {preguntaData.Texto.Substring(0, Math.Min(80, preguntaData.Texto.Length))}...");
                                    }
                                }
                            }

                            // Validación de texto muy corto
                            if (preguntaData.Texto.Length < 15)
                            {
                                Console.WriteLine($"⚠ Pregunta {numeroPreguntaReal} - Texto muy corto: '{preguntaData.Texto}'");

                                var matchesA = Regex.Matches(bloqueCompleto, @"(?<![A-Za-z])A\s+");

                                if (matchesA.Count >= 2)
                                {
                                    int posSegundaA = matchesA[1].Index;
                                    preguntaData.Texto = bloqueCompleto.Substring(0, posSegundaA).Trim();
                                    Console.WriteLine($"  ✓ Corregido usando segunda A: '{preguntaData.Texto.Substring(0, Math.Min(80, preguntaData.Texto.Length))}...'");
                                }
                            }

                            // PASO 2: Extraer opciones
                            string textoOpciones = bloqueCompleto;

                            if (!string.IsNullOrWhiteSpace(preguntaData.Texto) && bloqueCompleto.Contains(preguntaData.Texto))
                            {
                                int finPregunta = bloqueCompleto.IndexOf(preguntaData.Texto) + preguntaData.Texto.Length;
                                if (finPregunta < bloqueCompleto.Length)
                                {
                                    textoOpciones = bloqueCompleto.Substring(finPregunta);
                                }
                            }

                            // Extraer opciones
                            var matchA = Regex.Match(textoOpciones, @"(?<![A-Za-z])\s*A\s+(.+?)(?=(?<![A-Za-z])\s*B\s+|$)", RegexOptions.Singleline);
                            if (matchA.Success)
                            {
                                preguntaData.Opciones["A"] = matchA.Groups[1].Value.Trim();
                            }

                            var matchB = Regex.Match(textoOpciones, @"(?<![A-Za-z])\s*B\s+(.+?)(?=(?<![A-Za-z])\s*C\s+|$)", RegexOptions.Singleline);
                            if (matchB.Success)
                            {
                                preguntaData.Opciones["B"] = matchB.Groups[1].Value.Trim();
                            }

                            var matchC = Regex.Match(textoOpciones, @"(?<![A-Za-z])\s*C\s+(.+?)(?=(?<![A-Za-z])\s*D\s+|$)", RegexOptions.Singleline);
                            if (matchC.Success)
                            {
                                preguntaData.Opciones["C"] = matchC.Groups[1].Value.Trim();
                            }

                            var matchD = Regex.Match(textoOpciones, @"(?<![A-Za-z])\s*D\s+(.+?)(?=\d+\.?\s*-?\s*Pregunta:|$)", RegexOptions.Singleline);
                            if (matchD.Success)
                            {
                                preguntaData.Opciones["D"] = matchD.Groups[1].Value.Trim();
                            }

                            // Validar que tenga las 4 opciones
                            if (!string.IsNullOrWhiteSpace(preguntaData.Texto) &&
                                preguntaData.Opciones.Count >= 4 &&
                                preguntaData.Texto.Length >= 15)
                            {
                                resultado.Preguntas.Add(preguntaData);

                                if (numeroPreguntaReal <= 5 || numeroPreguntaReal % 50 == 0)
                                {
                                    Console.WriteLine($"✓ Pregunta {numeroPreguntaReal} agregada correctamente");
                                }

                                numeroPreguntaReal++;
                            }
                            else
                            {
                                Console.WriteLine($"✗ Pregunta {numeroPreguntaReal} descartada - Texto length: {preguntaData.Texto.Length}, Opciones: {preguntaData.Opciones.Count}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Error procesando pregunta: {ex.Message}");
                        }
                    }

                    Console.WriteLine($"\nTotal preguntas procesadas: {resultado.Preguntas.Count}");

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

            try
            {
                using (var stream = archivo.OpenReadStream())
                {
                    using (var doc = DocX.Load(stream))
                    {
                        // Extraer solo el texto, ignorando tablas y formato complejo
                        string textoCompleto = "";

                        try
                        {
                            textoCompleto = doc.Text;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠ Error extrayendo texto del documento: {ex.Message}");
                            Console.WriteLine("Intentando método alternativo...");

                            // Método alternativo: leer párrafos directamente
                            foreach (var paragraph in doc.Paragraphs)
                            {
                                textoCompleto += paragraph.Text + "\n";
                            }
                        }

                        Console.WriteLine($"Procesando respuestas, longitud: {textoCompleto.Length}");
                        Console.WriteLine($"Primeras 300 caracteres:\n{textoCompleto.Substring(0, Math.Min(300, textoCompleto.Length))}");

                        // Buscar patrones como "1 B", "2 C", "3 A", etc.
                        var patronRespuestas = @"(\d+)[\.\s\-]+([A-D])";
                        var matches = Regex.Matches(textoCompleto, patronRespuestas);

                        Console.WriteLine($"Coincidencias de respuestas encontradas: {matches.Count}");

                        foreach (Match match in matches)
                        {
                            try
                            {
                                int numeroPregunta = int.Parse(match.Groups[1].Value);
                                string respuesta = match.Groups[2].Value.ToUpper().Trim();

                                respuestas[numeroPregunta] = respuesta;

                                // Mostrar solo las primeras 10 y las últimas 10
                                if (respuestas.Count <= 10 || respuestas.Count > matches.Count - 10)
                                {
                                    Console.WriteLine($"  {numeroPregunta} → {respuesta}");
                                }
                                else if (respuestas.Count == 11)
                                {
                                    Console.WriteLine($"  ... (mostrando solo primeras y últimas 10) ...");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"✗ Error procesando respuesta '{match.Value}': {ex.Message}");
                            }
                        }

                        Console.WriteLine($"Total respuestas procesadas: {respuestas.Count}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error al abrir archivo de respuestas con Xceed: {ex.Message}");
                Console.WriteLine("Intentando método alternativo sin Xceed...");

                // PLAN B: Intentar leer como archivo ZIP (los .docx son ZIP internamente)
                using (var stream = archivo.OpenReadStream())
                {
                    using (var package = System.IO.Packaging.Package.Open(stream, FileMode.Open, FileAccess.Read))
                    {
                        var documentUri = new Uri("/word/document.xml", UriKind.Relative);
                        if (package.PartExists(documentUri))
                        {
                            var documentPart = package.GetPart(documentUri);
                            using (var documentStream = documentPart.GetStream())
                            using (var reader = new StreamReader(documentStream))
                            {
                                string xmlContent = reader.ReadToEnd();

                                // Extraer texto del XML (muy básico pero funcional)
                                var textMatches = Regex.Matches(xmlContent, @"<w:t[^>]*>([^<]+)</w:t>");
                                string textoCompleto = "";
                                foreach (Match textMatch in textMatches)
                                {
                                    textoCompleto += textMatch.Groups[1].Value + " ";
                                }

                                Console.WriteLine($"Texto extraído del XML: {textoCompleto.Length} caracteres");

                                // Buscar respuestas
                                var patronRespuestas = @"(\d+)[\.\s\-]+([A-D])";
                                var matches = Regex.Matches(textoCompleto, patronRespuestas);

                                Console.WriteLine($"Respuestas encontradas: {matches.Count}");

                                foreach (Match match in matches)
                                {
                                    int numeroPregunta = int.Parse(match.Groups[1].Value);
                                    string respuesta = match.Groups[2].Value.ToUpper().Trim();
                                    respuestas[numeroPregunta] = respuesta;
                                }
                            }
                        }
                    }
                }

                if (respuestas.Count == 0)
                {
                    throw new Exception($"No se pudieron procesar las respuestas. Error original: {ex.Message}");
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
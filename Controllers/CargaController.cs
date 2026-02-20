using ExamenTransporte.Data;
using ExamenTransporte.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
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

        // ============================================
        // MODELO 1: 2 archivos (CÓDIGO EXISTENTE)
        // ============================================
        [HttpPost]
        public async Task<IActionResult> CargarArchivosModelo1(CargaExamenViewModel model)
        {
            if (model.ArchivoExamen == null || model.ArchivoRespuestas == null)
            {
                model.Mensaje = "Debe seleccionar ambos archivos .docx";
                model.Exito = false;
                model.ModeloCarga = 1;
                return View("Index", model);
            }

            try
            {
                Console.WriteLine("\n========== INICIANDO CARGA MODELO 1 ==========");
                Console.WriteLine($"Archivo examen: {model.ArchivoExamen.FileName}");
                Console.WriteLine($"Archivo respuestas: {model.ArchivoRespuestas.FileName}");

                Console.WriteLine("\n--- PASO 1: Procesando archivo de preguntas ---");
                var datosExamen = await ProcesarArchivoExamenModelo1(model.ArchivoExamen);
                Console.WriteLine($"✓ Examen procesado: '{datosExamen.Titulo}' con {datosExamen.Preguntas.Count} preguntas");

                Console.WriteLine("\n--- PASO 2: Procesando archivo de respuestas ---");
                var respuestas = await ProcesarArchivoRespuestasModelo1(model.ArchivoRespuestas);
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
                        throw;
                    }
                }

                Console.WriteLine($"\n✓ Total preguntas guardadas: {preguntasGuardadas}");
                Console.WriteLine("========== CARGA COMPLETADA ==========\n");

                model.Mensaje = $"Examen '{datosExamen.Titulo}' cargado correctamente con {datosExamen.Preguntas.Count} preguntas";
                model.Exito = true;
                model.ModeloCarga = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗✗✗ ERROR CRÍTICO ✗✗✗");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                model.Mensaje = $"Error: {ex.Message}";
                model.Exito = false;
                model.ModeloCarga = 1;
            }

            return View("Index", model);
        }

        // ============================================
        // MODELO 2: 1 archivo completo (NUEVO)
        // ============================================
        [HttpPost]
        public async Task<IActionResult> CargarArchivoModelo2(CargaExamenViewModel model)
        {
            if (model.ArchivoCompleto == null)
            {
                model.Mensaje = "Debe seleccionar un archivo .docx";
                model.Exito = false;
                model.ModeloCarga = 2;
                return View("Index", model);
            }

            try
            {
                Console.WriteLine("\n========== INICIANDO CARGA MODELO 2 ==========");
                Console.WriteLine($"Archivo: {model.ArchivoCompleto.FileName}");

                Console.WriteLine("\n--- PASO 1: Procesando archivo completo ---");
                var datosExamen = await ProcesarArchivoCompletoModelo2(model.ArchivoCompleto);
                Console.WriteLine($"✓ Examen procesado: '{datosExamen.Titulo}' con {datosExamen.Preguntas.Count} preguntas");

                Console.WriteLine("\n--- PASO 2: Guardando en base de datos ---");
                int examenId = _repository.GuardarExamen(datosExamen.Titulo);
                Console.WriteLine($"✓ Examen guardado con ID: {examenId}");

                int preguntasGuardadas = 0;
                foreach (var pregunta in datosExamen.Preguntas)
                {
                    try
                    {
                        Console.WriteLine($"\nGuardando pregunta {pregunta.Numero} (respuesta correcta: '{pregunta.RespuestaCorrecta}')...");
                        _repository.GuardarPregunta(examenId, pregunta, pregunta.RespuestaCorrecta);
                        preguntasGuardadas++;
                        Console.WriteLine($"  ✓ Guardada correctamente");
                    }
                    catch (Exception exPregunta)
                    {
                        Console.WriteLine($"✗ Error guardando pregunta {pregunta.Numero}:");
                        Console.WriteLine($"  Mensaje: {exPregunta.Message}");
                        throw;
                    }
                }

                Console.WriteLine($"\n✓ Total preguntas guardadas: {preguntasGuardadas}");
                Console.WriteLine("========== CARGA COMPLETADA ==========\n");

                model.Mensaje = $"Examen '{datosExamen.Titulo}' cargado correctamente con {datosExamen.Preguntas.Count} preguntas";
                model.Exito = true;
                model.ModeloCarga = 2;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗✗✗ ERROR CRÍTICO ✗✗✗");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                model.Mensaje = $"Error: {ex.Message}";
                model.Exito = false;
                model.ModeloCarga = 2;
            }

            return View("Index", model);
        }

        // ============================================
        // MÉTODOS DE PROCESAMIENTO MODELO 1
        // ============================================

        private async Task<DatosExamen> ProcesarArchivoExamenModelo1(IFormFile archivo)
        {
            var resultado = new DatosExamen();

            // Leer contenido como texto plano
            string textoCompleto;
            using (var stream = archivo.OpenReadStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                textoCompleto = await reader.ReadToEndAsync();
            }

            textoCompleto = textoCompleto.Replace("\0", "");
            Console.WriteLine($"Longitud del texto: {textoCompleto.Length} caracteres");

            // Título del archivo
            resultado.Titulo = Path.GetFileNameWithoutExtension(archivo.FileName).Trim();
            if (resultado.Titulo.Length > 200)
            {
                resultado.Titulo = resultado.Titulo.Substring(0, 200);
            }

            Console.WriteLine($"Título extraído: '{resultado.Titulo}'");

            // Patrón de preguntas del modelo 1
            var patronPreguntas = @"(?:\d+\.?\s*-?\s*)?Pregunta:\s*(.+?)(?=(?:\d+\.?\s*-?\s*)?Pregunta:|$)";
            var matchesPreguntas = Regex.Matches(textoCompleto, patronPreguntas, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            Console.WriteLine($"Bloques encontrados: {matchesPreguntas.Count}");

            int numeroPreguntaReal = 1;

            foreach (Match matchPregunta in matchesPreguntas)
            {
                try
                {
                    string bloqueCompleto = matchPregunta.Groups[1].Value;

                    var preguntaData = new PreguntaData
                    {
                        Numero = numeroPreguntaReal,
                        Texto = "",
                        Opciones = new Dictionary<string, string>()
                    };

                    // Extraer texto de la pregunta
                    var matchInicioOpciones = Regex.Match(bloqueCompleto, @"^(.+?[?.:!])\s*(?<![A-Za-z])A[\s:]", RegexOptions.Singleline);

                    if (matchInicioOpciones.Success)
                    {
                        preguntaData.Texto = matchInicioOpciones.Groups[1].Value.Trim();
                    }
                    else
                    {
                        var matchPlanB = Regex.Match(bloqueCompleto, @"^(.+?)(?<![A-Za-z])A[\s:]", RegexOptions.Singleline);
                        if (matchPlanB.Success)
                        {
                            preguntaData.Texto = matchPlanB.Groups[1].Value.Trim();
                        }
                    }

                    // Extraer opciones A, B, C, D
                    var patronOpciones = @"(?<![A-Za-z])([A-D])[\s:]+([^\n]+?)(?=\s*(?:[A-D][\s:]|\Z))";
                    var matchesOpciones = Regex.Matches(bloqueCompleto, patronOpciones, RegexOptions.Singleline);

                    foreach (Match matchOpcion in matchesOpciones)
                    {
                        string letra = matchOpcion.Groups[1].Value.Trim().ToUpper();
                        string textoOpcion = matchOpcion.Groups[2].Value.Trim();

                        if (!preguntaData.Opciones.ContainsKey(letra))
                        {
                            preguntaData.Opciones[letra] = textoOpcion;
                        }
                    }

                    // Validar y agregar
                    if (!string.IsNullOrWhiteSpace(preguntaData.Texto) &&
                        preguntaData.Opciones.Count >= 4 &&
                        preguntaData.Texto.Length >= 10)
                    {
                        resultado.Preguntas.Add(preguntaData);
                        numeroPreguntaReal++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error procesando pregunta: {ex.Message}");
                }
            }

            if (resultado.Preguntas.Count == 0)
            {
                throw new Exception("No se encontraron preguntas válidas en el archivo");
            }

            return resultado;
        }

        private async Task<Dictionary<int, string>> ProcesarArchivoRespuestasModelo1(IFormFile archivo)
        {
            var respuestas = new Dictionary<int, string>();

            string textoCompleto;
            using (var stream = archivo.OpenReadStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                textoCompleto = await reader.ReadToEndAsync();
            }

            Console.WriteLine($"Procesando respuestas, longitud: {textoCompleto.Length}");

            var patronRespuestas = @"(\d+)[\.\s\-]+([A-D])";
            var matches = Regex.Matches(textoCompleto, patronRespuestas);

            Console.WriteLine($"Respuestas encontradas: {matches.Count}");

            foreach (Match match in matches)
            {
                int numeroPregunta = int.Parse(match.Groups[1].Value);
                string respuesta = match.Groups[2].Value.ToUpper().Trim();
                respuestas[numeroPregunta] = respuesta;
            }

            return respuestas;
        }

        // ============================================
        // MÉTODOS DE PROCESAMIENTO MODELO 2 (NUEVO)
        // ============================================

        private async Task<DatosExamen> ProcesarArchivoCompletoModelo2(IFormFile archivo)
        {
            var resultado = new DatosExamen();
            string textoCompleto = "";

            // Título del archivo
            resultado.Titulo = Path.GetFileNameWithoutExtension(archivo.FileName).Trim();
            if (resultado.Titulo.Length > 200)
            {
                resultado.Titulo = resultado.Titulo.Substring(0, 200);
            }

            Console.WriteLine($"Título extraído: '{resultado.Titulo}'");

            try
            {
                // INTENTAR LEER COMO .DOCX REAL (con Xceed)
                using (var stream = archivo.OpenReadStream())
                {
                    using (var doc = DocX.Load(stream))
                    {
                        textoCompleto = doc.Text;
                        Console.WriteLine("✓ Archivo leído con Xceed.Words.NET");
                    }
                }
            }
            catch (Exception exXceed)
            {
                Console.WriteLine($"⚠ No se pudo leer con Xceed: {exXceed.Message}");
                Console.WriteLine("Intentando leer como texto plano...");

                // PLAN B: LEER COMO TEXTO PLANO
                using (var stream = archivo.OpenReadStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    textoCompleto = await reader.ReadToEndAsync();
                }
            }

            Console.WriteLine($"Texto extraído: {textoCompleto.Length} caracteres");
            Console.WriteLine($"Primeras 500 caracteres:\n{textoCompleto.Substring(0, Math.Min(500, textoCompleto.Length))}");

            // DETECTAR FORMATO AUTOMÁTICAMENTE
            bool esFormatoA = textoCompleto.Contains("SOLUCION", StringComparison.OrdinalIgnoreCase) ||
                  textoCompleto.Contains("SOLUCIÓ", StringComparison.OrdinalIgnoreCase);

            bool esFormatoB = textoCompleto.Contains("Respuesta correcta", StringComparison.OrdinalIgnoreCase) ||
                              textoCompleto.Contains("Resposta correcta", StringComparison.OrdinalIgnoreCase);

            Console.WriteLine($"\nDetección de formato:");
            Console.WriteLine($"  - Formato A (SOLUCION): {esFormatoA}");
            Console.WriteLine($"  - Formato B (Respuesta correcta): {esFormatoB}");

            // 🔍 DEBUG: Ver más del contenido
            Console.WriteLine("\n========== MOSTRANDO MÁS CONTENIDO ==========");
            Console.WriteLine($"Primeros 2000 caracteres del texto:\n{textoCompleto.Substring(0, Math.Min(2000, textoCompleto.Length))}");
            Console.WriteLine("\n========== FIN CONTENIDO ==========\n");

            if (esFormatoB)
            {
                Console.WriteLine("\n>>> Procesando con FORMATO B <<<");
                resultado.Preguntas = ProcesarFormatoB(textoCompleto);
            }
            else if (esFormatoA)
            {
                Console.WriteLine("\n>>> Procesando con FORMATO A <<<");
                resultado.Preguntas = ProcesarFormatoA(textoCompleto);
            }
            else
            {
                throw new Exception("No se pudo detectar el formato del archivo. Asegúrate de que contenga 'SOLUCION' o 'Respuesta correcta'");
            }

            Console.WriteLine($"\nTotal preguntas procesadas: {resultado.Preguntas.Count}");

            if (resultado.Preguntas.Count == 0)
            {
                throw new Exception("No se encontraron preguntas válidas en el archivo. Verifica el formato.");
            }

            return resultado;
        }

        // FORMATO A: PREGUNTA: ... A: ... B: ... SOLUCION: X
        private List<PreguntaData> ProcesarFormatoA(string texto)
        {
            var preguntas = new List<PreguntaData>();

            // Patrón: buscar bloques que empiezan con "PREGUNTA:" hasta el siguiente "PREGUNTA:" o fin
            var patronBloques = @"PREGUNTA:\s*(.+?)(?=PREGUNTA:|$)";
            var bloques = Regex.Matches(texto, patronBloques, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            Console.WriteLine($"Bloques FORMATO A encontrados: {bloques.Count}");

            int numeroPregunta = 1;

            foreach (Match bloque in bloques)
            {
                try
                {
                    string contenido = bloque.Groups[1].Value;

                    Console.WriteLine($"\n=== Procesando Pregunta {numeroPregunta} (Formato A) ===");

                    var preguntaData = new PreguntaData
                    {
                        Numero = numeroPregunta,
                        Texto = "",
                        Opciones = new Dictionary<string, string>(),
                        RespuestaCorrecta = ""
                    };

                    // 1. Extraer SOLUCION primero
                    var matchSolucion = Regex.Match(contenido, @"SOLUCION\s*:\s*([A-D])", RegexOptions.IgnoreCase);
                    if (!matchSolucion.Success)
                    {
                        Console.WriteLine($"✗ No se encontró SOLUCION");
                        continue;
                    }
                    preguntaData.RespuestaCorrecta = matchSolucion.Groups[1].Value.ToUpper().Trim();

                    // 2. Extraer TODO desde el inicio hasta "SOLUCION" (esto incluye pregunta + opciones)
                    var matchHastaSolucion = Regex.Match(contenido, @"^(.+?)SOLUCION", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (!matchHastaSolucion.Success)
                    {
                        Console.WriteLine($"✗ No se pudo extraer bloque pregunta+opciones");
                        continue;
                    }

                    string bloquePreguntaOpciones = matchHastaSolucion.Groups[1].Value.Trim();

                    // 3. Extraer texto de la pregunta (desde inicio hasta "A:")
                    var matchTexto = Regex.Match(bloquePreguntaOpciones, @"^(.+?)\s*A\s*:", RegexOptions.Singleline);
                    if (!matchTexto.Success)
                    {
                        Console.WriteLine($"✗ No se pudo extraer texto de pregunta");
                        continue;
                    }

                    preguntaData.Texto = matchTexto.Groups[1].Value.Trim();
                    preguntaData.Texto = Regex.Replace(preguntaData.Texto, @"\s+", " ");

                    if (numeroPregunta <= 10)
                    {
                        Console.WriteLine($"Texto: {preguntaData.Texto.Substring(0, Math.Min(100, preguntaData.Texto.Length))}...");
                    }

                    // 4. Extraer cada opción A, B, C, D
                    // Este patrón funciona tanto si están juntas como separadas por saltos de línea
                    var patronOpciones = @"([A-D])\s*:\s*(.+?)(?=\s*[ABCD]\s*:|$)";
                    var matchesOpciones = Regex.Matches(bloquePreguntaOpciones, patronOpciones, RegexOptions.Singleline);

                    foreach (Match matchOpcion in matchesOpciones)
                    {
                        string letra = matchOpcion.Groups[1].Value.ToUpper().Trim();
                        string textoOpcion = matchOpcion.Groups[2].Value.Trim();

                        // Limpiar múltiples espacios
                        textoOpcion = Regex.Replace(textoOpcion, @"\s+", " ");

                        if (!preguntaData.Opciones.ContainsKey(letra) && !string.IsNullOrWhiteSpace(textoOpcion))
                        {
                            preguntaData.Opciones[letra] = textoOpcion;

                            if (numeroPregunta <= 10)
                            {
                                Console.WriteLine($"  Opción {letra}: {textoOpcion.Substring(0, Math.Min(50, textoOpcion.Length))}...");
                            }
                        }
                    }

                    // 5. Validar y agregar
                    if (!string.IsNullOrWhiteSpace(preguntaData.Texto) &&
                        preguntaData.Opciones.Count == 4 &&
                        preguntaData.Opciones.ContainsKey("A") &&
                        preguntaData.Opciones.ContainsKey("B") &&
                        preguntaData.Opciones.ContainsKey("C") &&
                        preguntaData.Opciones.ContainsKey("D") &&
                        !string.IsNullOrWhiteSpace(preguntaData.RespuestaCorrecta))
                    {
                        preguntas.Add(preguntaData);

                        if (numeroPregunta <= 10)
                        {
                            Console.WriteLine($"✓ Pregunta {numeroPregunta} agregada - Respuesta: {preguntaData.RespuestaCorrecta}");
                        }

                        numeroPregunta++;
                    }
                    else
                    {
                        if (numeroPregunta <= 10)
                        {
                            Console.WriteLine($"✗ Pregunta descartada:");
                            Console.WriteLine($"   - Texto length: {preguntaData.Texto.Length}");
                            Console.WriteLine($"   - Opciones encontradas: {string.Join(", ", preguntaData.Opciones.Keys)}");
                            Console.WriteLine($"   - Total opciones: {preguntaData.Opciones.Count}");
                            Console.WriteLine($"   - Respuesta: '{preguntaData.RespuestaCorrecta}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }

            return preguntas;
        }

        // FORMATO B: PREGUNTA X - ... A. ... B. ... Respuesta correcta: X

        private List<PreguntaData> ProcesarFormatoB(string texto)
        {
            var preguntas = new List<PreguntaData>();

            var patronBloques = @"PREGUNTA\s+(\d+)\s*-\s*(.+?)(?=PREGUNTA\s+\d+\s*-|$)";
            var bloques = Regex.Matches(texto, patronBloques, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            Console.WriteLine($"Bloques FORMATO B encontrados: {bloques.Count}");

            foreach (Match bloque in bloques)
            {
                try
                {
                    int numeroPregunta = int.Parse(bloque.Groups[1].Value);
                    string contenido = bloque.Groups[2].Value;

                    // DEBUG pregunta 45 - contenido COMPLETO antes de procesar
                    if (numeroPregunta == 45)
                    {
                        Console.WriteLine($"\n========== DEBUG PREGUNTA 45 - CONTENIDO COMPLETO ==========");
                        Console.WriteLine(contenido);
                        Console.WriteLine("========== FIN DEBUG ==========\n");
                    }

                    Console.WriteLine($"Procesando pregunta {numeroPregunta}...");

                    if (numeroPregunta <= 10 || numeroPregunta > 190)
                    {
                        Console.WriteLine($"\n=== Procesando Pregunta {numeroPregunta} (Formato B) ===");
                    }

                    var preguntaData = new PreguntaData
                    {
                        Numero = numeroPregunta,
                        Texto = "",
                        Opciones = new Dictionary<string, string>(),
                        RespuestaCorrecta = ""
                    };

                    // 1. Extraer Respuesta correcta
                    var matchRespuesta = Regex.Match(contenido, @"Resp(uesta|osta)\s+correcta\s*:\s*([A-D])", RegexOptions.IgnoreCase);
                    if (!matchRespuesta.Success)
                    {
                        Console.WriteLine($"✗ Pregunta {numeroPregunta} - No se encontró respuesta correcta");
                        continue;
                    }

                    preguntaData.RespuestaCorrecta = matchRespuesta.Groups[2].Value.ToUpper().Trim();

                    // 2. Extraer bloque pregunta+opciones (hasta "Respuesta alumno")
                    var matchHastaRespuesta = Regex.Match(contenido, @"^(.+?)\s*Resp(uesta|osta)\s+alumn(o|e)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    if (!matchHastaRespuesta.Success)
                    {
                        Console.WriteLine($"✗ Pregunta {numeroPregunta} - No se encontró 'Respuesta alumno'");
                        continue;
                    }

                    string bloquePreguntaOpciones = matchHastaRespuesta.Groups[1].Value.Trim();

                    // DEBUG para preguntas problemáticas
                    if (numeroPregunta == 35 || numeroPregunta == 39 || numeroPregunta == 55 ||
                        numeroPregunta == 113 || numeroPregunta == 125 || numeroPregunta == 174)
                    {
                        Console.WriteLine($"\n>>> DEBUG PREGUNTA {numeroPregunta} <<<");
                        Console.WriteLine($"Primeros 500 caracteres:");
                        Console.WriteLine(bloquePreguntaOpciones.Substring(0, Math.Min(500, bloquePreguntaOpciones.Length)));
                        Console.WriteLine(">>> FIN DEBUG <<<\n");
                    }

                    // 3. Buscar dónde termina la pregunta
                    int indicePregunta = bloquePreguntaOpciones.IndexOf('?');

                    // Si no hay "?", buscar donde empieza la primera opción
                    if (indicePregunta == -1)
                    {
                        // PATRÓN 1: letra minúscula/número/paréntesis + espacio + MAYÚSCULA
                        var matchFinPregunta = Regex.Match(bloquePreguntaOpciones, @"[a-z\d\)]\s+([A-Z])");

                        // PATRÓN 2: punto + espacio + minúscula (para preguntas que terminan con punto)
                        if (!matchFinPregunta.Success)
                        {
                            matchFinPregunta = Regex.Match(bloquePreguntaOpciones, @"\.\s+([A-Za-z])");
                        }

                        // PATRÓN 3: dos puntos + cualquier carácter (mayúscula, minúscula o número)
                        if (!matchFinPregunta.Success)
                        {
                            matchFinPregunta = Regex.Match(bloquePreguntaOpciones, @":([A-Za-z0-9])");
                        }

                        // PATRÓN 4: tres puntos suspensivos + cualquier carácter
                        if (!matchFinPregunta.Success)
                        {
                            matchFinPregunta = Regex.Match(bloquePreguntaOpciones, @"\.\.\.\s*([A-Za-z])");
                        }

                        if (matchFinPregunta.Success)
                        {
                            // Ajustar índice según el patrón detectado
                            if (matchFinPregunta.Value.Contains("..."))
                            {
                                // Para tres puntos, incluir los 3 puntos completos
                                indicePregunta = matchFinPregunta.Index + 2; // Posición del último punto
                            }
                            else
                            {
                                indicePregunta = matchFinPregunta.Index; // Para otros patrones
                            }
                        }
                        else
                        {
                            Console.WriteLine($"✗ Pregunta {numeroPregunta} - No se pudo detectar fin de pregunta (sin '?' ni patrón reconocible)");
                            continue;
                        }
                    }

                    preguntaData.Texto = bloquePreguntaOpciones.Substring(0, indicePregunta + 1).Trim();
                    preguntaData.Texto = Regex.Replace(preguntaData.Texto, @"\s+", " ");

                    if (numeroPregunta <= 10 || numeroPregunta > 190)
                    {
                        Console.WriteLine($"Texto: {preguntaData.Texto.Substring(0, Math.Min(100, preguntaData.Texto.Length))}...");
                    }

                    // 4. Extraer las 4 opciones (todo lo que queda después de la pregunta)
                    int inicioOpciones = preguntaData.Texto.EndsWith("?") ? indicePregunta + 1 : indicePregunta + 1;

                    // Si terminaba en punto, ajustar para no incluir el punto en las opciones
                    if (preguntaData.Texto.EndsWith(".") && indicePregunta < bloquePreguntaOpciones.Length)
                    {
                        inicioOpciones = indicePregunta + 1;
                    }

                    string bloqueOpciones = bloquePreguntaOpciones.Substring(inicioOpciones).Trim();

                    // ESTRATEGIA: Dividir palabra por palabra y detectar inicio de nueva opción
                    var opcionesEncontradas = new List<string>();
                    var palabras = bloqueOpciones.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    string opcionActual = "";

                    foreach (var palabra in palabras)
                    {
                        // Si encontramos "Respuesta", terminamos
                        if (palabra.StartsWith("Respuesta", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }

                        // Si encontramos inicio de nueva opción (mayúscula y ya tenemos contenido)
                        if (!string.IsNullOrEmpty(opcionActual) &&
                           char.IsUpper(palabra[0]) &&
                           opcionActual.Length >= 2 &&  // ← Cambiar solo esta línea
                           opcionesEncontradas.Count < 4)
                        {
                            // Guardar la opción anterior
                            string opcionLimpia = opcionActual.Trim().TrimEnd('.');
                            if (opcionLimpia.Length >= 2)
                            {
                                opcionesEncontradas.Add(opcionLimpia);
                            }
                            opcionActual = palabra;
                        }
                        else
                        {
                            opcionActual += (string.IsNullOrEmpty(opcionActual) ? "" : " ") + palabra;
                        }
                    }

                    // Agregar la última opción si no llegamos a 4
                    if (!string.IsNullOrEmpty(opcionActual) && opcionesEncontradas.Count < 4)
                    {
                        // Limpiar "Respuesta" si quedó pegado
                        opcionActual = Regex.Replace(opcionActual, @"\s*Respuesta\s+.*$", "", RegexOptions.IgnoreCase);
                        string opcionLimpia = opcionActual.Trim().TrimEnd('.');
                        if (opcionLimpia.Length >= 2)
                        {
                            opcionesEncontradas.Add(opcionLimpia);
                        }
                    }

                    // Si no tenemos exactamente 4 opciones, intentar método alternativo (split por punto)
                    if (opcionesEncontradas.Count != 4)
                    {
                        if (numeroPregunta <= 10 || numeroPregunta > 190)
                        {
                            Console.WriteLine($"  ⚠ Método 1 encontró {opcionesEncontradas.Count} opciones, intentando método alternativo...");
                        }

                        opcionesEncontradas.Clear();

                        // Método alternativo: dividir por punto y tomar frases >= 2 caracteres
                        var frases = bloqueOpciones.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var frase in frases)
                        {
                            string fraseClean = frase.Trim();

                            if (fraseClean.StartsWith("Respuesta", StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }

                            if (fraseClean.Length >= 2 &&
                                !fraseClean.StartsWith("Ley/", StringComparison.OrdinalIgnoreCase) &&
                                opcionesEncontradas.Count < 4)
                            {
                                opcionesEncontradas.Add(fraseClean);
                            }
                        }
                    }

                    // Asignar las opciones a A, B, C, D
                    string[] letras = { "A", "B", "C", "D" };

                    for (int i = 0; i < Math.Min(4, opcionesEncontradas.Count); i++)
                    {
                        string textoOpcion = opcionesEncontradas[i].Trim();
                        textoOpcion = Regex.Replace(textoOpcion, @"\s+", " ");

                        preguntaData.Opciones[letras[i]] = textoOpcion;

                        if (numeroPregunta <= 10 || numeroPregunta > 190)
                        {
                            Console.WriteLine($"  Opción {letras[i]}: {textoOpcion.Substring(0, Math.Min(60, textoOpcion.Length))}...");
                        }
                    }

                    if (numeroPregunta == 45 || numeroPregunta == 105)
                    {
                        Console.WriteLine($"\n>>> DEBUG OPCIONES PREGUNTA {numeroPregunta} <<<");
                        Console.WriteLine($"Bloque opciones completo:");
                        Console.WriteLine(bloqueOpciones.Substring(0, Math.Min(800, bloqueOpciones.Length)));
                        Console.WriteLine($"\nOpciones encontradas: {opcionesEncontradas.Count}");
                        for (int i = 0; i < opcionesEncontradas.Count; i++)
                        {
                            Console.WriteLine($"  Opción {i + 1}: {opcionesEncontradas[i]}");
                        }
                        Console.WriteLine(">>> FIN DEBUG OPCIONES <<<\n");
                    }

                    // 5. Validar
                    if (!string.IsNullOrWhiteSpace(preguntaData.Texto) &&
                        preguntaData.Opciones.Count == 4 &&
                        !string.IsNullOrWhiteSpace(preguntaData.RespuestaCorrecta))
                    {
                        preguntas.Add(preguntaData);

                        if (numeroPregunta <= 10 || numeroPregunta > 190)
                        {
                            Console.WriteLine($"✓ Pregunta {numeroPregunta} agregada - Respuesta: {preguntaData.RespuestaCorrecta}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"✗ Pregunta {numeroPregunta} descartada:");
                        Console.WriteLine($"   - Texto: {preguntaData.Texto.Length} chars");
                        Console.WriteLine($"   - Opciones: {preguntaData.Opciones.Count}");
                        Console.WriteLine($"   - Respuesta: '{preguntaData.RespuestaCorrecta}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error en pregunta: {ex.Message}");
                }
            }

            Console.WriteLine($"\n========================================");
            Console.WriteLine($"RESUMEN: {preguntas.Count} preguntas procesadas de {bloques.Count} bloques encontrados");
            Console.WriteLine($"========================================");

            return preguntas;
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
        public string RespuestaCorrecta { get; set; } // Para modelo 2
    }
}
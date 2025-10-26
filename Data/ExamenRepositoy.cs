using ExamenTransporte.Controllers;
using ExamenTransporte.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ExamenTransporte.Data
{
    public class ExamenRepository
    {
        private readonly string _connectionString;

        public ExamenRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public int GuardarExamen(string titulo)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"INSERT INTO Examenes (Titulo, FechaCreacion, Activo) 
                                VALUES (@Titulo, GETDATE(), 1);
                                SELECT CAST(SCOPE_IDENTITY() as int);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Titulo", titulo);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public void GuardarPregunta(int examenId, PreguntaData pregunta, string respuestaCorrecta)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Insertar pregunta
                string queryPregunta = @"INSERT INTO Preguntas (ExamenId, TextoPregunta, OrdenPregunta, Puntos) 
                                        VALUES (@ExamenId, @TextoPregunta, @OrdenPregunta, 1.0);
                                        SELECT CAST(SCOPE_IDENTITY() as int);";

                int preguntaId;
                using (SqlCommand cmd = new SqlCommand(queryPregunta, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenId", examenId);
                    cmd.Parameters.AddWithValue("@TextoPregunta", pregunta.Texto);
                    cmd.Parameters.AddWithValue("@OrdenPregunta", pregunta.Numero);
                    preguntaId = (int)cmd.ExecuteScalar();
                }

                // Insertar opciones
                string queryOpcion = @"INSERT INTO Opciones (PreguntaId, TextoOpcion, EsCorrecta, OrdenOpcion) 
                                      VALUES (@PreguntaId, @TextoOpcion, @EsCorrecta, @OrdenOpcion);";

                // Orden de las letras A=1, B=2, C=3, D=4
                var ordenLetras = new Dictionary<string, int>
                {
                    { "A", 1 }, { "B", 2 }, { "C", 3 }, { "D", 4 }
                };

                foreach (var opcion in pregunta.Opciones)
                {
                    using (SqlCommand cmd = new SqlCommand(queryOpcion, conn))
                    {
                        cmd.Parameters.AddWithValue("@PreguntaId", preguntaId);
                        cmd.Parameters.AddWithValue("@TextoOpcion", opcion.Value);

                        // Marcar si es la opción correcta
                        bool esCorrecta = opcion.Key == respuestaCorrecta;
                        cmd.Parameters.AddWithValue("@EsCorrecta", esCorrecta);

                        cmd.Parameters.AddWithValue("@OrdenOpcion", ordenLetras[opcion.Key]);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<(int Id, string Titulo)> ObtenerListaExamenes()
        {
            var examenes = new List<(int, string)>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT Id, Titulo FROM Examenes WHERE Activo = 1 ORDER BY FechaCreacion DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        examenes.Add((reader.GetInt32(0), reader.GetString(1)));
                    }
                }
            }

            return examenes;
        }

        public int IniciarExamen(int examenId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"INSERT INTO ExamenesRealizados (ExamenId, FechaInicio) 
                        VALUES (@ExamenId, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenId", examenId);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public PreguntaExamenViewModel ObtenerPregunta(int examenId, int numeroPregunta)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // CORREGIDO: usar OrdenPregunta en lugar de NumeroPregunta
                string query = @"SELECT p.Id, p.OrdenPregunta, p.TextoPregunta
                        FROM Preguntas p
                        WHERE p.ExamenId = @ExamenId AND p.OrdenPregunta = @Numero";

                PreguntaExamenViewModel pregunta = null;

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenId", examenId);
                    cmd.Parameters.AddWithValue("@Numero", numeroPregunta);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            pregunta = new PreguntaExamenViewModel
                            {
                                Id = reader.GetInt32(0),
                                Numero = reader.GetInt32(1),  // OrdenPregunta
                                Texto = reader.GetString(2),
                                Opciones = new List<OpcionViewModel>()
                            };
                        }
                    }
                }

                if (pregunta != null)
                {
                    // Obtener opciones
                    string queryOpciones = @"SELECT Id, TextoOpcion, EsCorrecta, OrdenOpcion
                                    FROM Opciones
                                    WHERE PreguntaId = @PreguntaId
                                    ORDER BY OrdenOpcion";

                    using (SqlCommand cmd = new SqlCommand(queryOpciones, conn))
                    {
                        cmd.Parameters.AddWithValue("@PreguntaId", pregunta.Id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var letras = new[] { "A", "B", "C", "D" };
                            int index = 0;

                            while (reader.Read())
                            {
                                bool esCorrecta = reader.GetBoolean(2);

                                pregunta.Opciones.Add(new OpcionViewModel
                                {
                                    Id = reader.GetInt32(0),
                                    Letra = letras[index],
                                    Texto = reader.GetString(1),
                                    EsCorrecta = esCorrecta
                                });

                                if (esCorrecta)
                                {
                                    pregunta.OpcionCorrectaId = reader.GetInt32(0);
                                }

                                index++;
                            }
                        }
                    }
                }

                return pregunta;
            }
        }

        public int ObtenerTotalPreguntas(int examenId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Preguntas WHERE ExamenId = @ExamenId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenId", examenId);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        public void GuardarRespuesta(int examenRealizadoId, int preguntaId, int opcionId, bool esCorrecta)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Verificar si ya existe una respuesta para esta pregunta en este intento
                string checkQuery = @"SELECT COUNT(*) FROM Respuestas 
                             WHERE ExamenRealizadoId = @ExamenRealizadoId 
                             AND PreguntaId = @PreguntaId";

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@ExamenRealizadoId", examenRealizadoId);
                    checkCmd.Parameters.AddWithValue("@PreguntaId", preguntaId);

                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        // Ya existe, actualizar en lugar de insertar
                        string updateQuery = @"UPDATE Respuestas 
                                      SET OpcionSeleccionadaId = @OpcionId, 
                                          EsCorrecta = @EsCorrecta,
                                          FechaRespuesta = GETDATE()
                                      WHERE ExamenRealizadoId = @ExamenRealizadoId 
                                      AND PreguntaId = @PreguntaId";

                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@ExamenRealizadoId", examenRealizadoId);
                            updateCmd.Parameters.AddWithValue("@PreguntaId", preguntaId);
                            updateCmd.Parameters.AddWithValue("@OpcionId", opcionId);
                            updateCmd.Parameters.AddWithValue("@EsCorrecta", esCorrecta);
                            updateCmd.ExecuteNonQuery();
                        }
                        return;
                    }
                }

                // No existe, insertar nueva
                string insertQuery = @"INSERT INTO Respuestas (ExamenRealizadoId, PreguntaId, OpcionSeleccionadaId, EsCorrecta)
                              VALUES (@ExamenRealizadoId, @PreguntaId, @OpcionId, @EsCorrecta)";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@ExamenRealizadoId", examenRealizadoId);
                    insertCmd.Parameters.AddWithValue("@PreguntaId", preguntaId);
                    insertCmd.Parameters.AddWithValue("@OpcionId", opcionId);
                    insertCmd.Parameters.AddWithValue("@EsCorrecta", esCorrecta);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        public string ObtenerTituloExamen(int examenId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string query = "SELECT Titulo FROM Examenes WHERE Id = @ExamenId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenId", examenId);
                    return cmd.ExecuteScalar()?.ToString() ?? "Examen";
                }
            }
        }

        // ========== MÉTODOS PARA HISTORIAL ==========

        public List<HistorialExamenViewModel> ObtenerHistorialExamenes()
        {
            var historial = new List<HistorialExamenViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"
            SELECT 
                e.Id AS ExamenId,
                e.Titulo,
                COUNT(DISTINCT er.Id) AS TotalIntentos,
                MAX(er.FechaInicio) AS UltimoIntento
            FROM Examenes e
            INNER JOIN ExamenesRealizados er ON e.Id = er.ExamenId
            GROUP BY e.Id, e.Titulo
            ORDER BY MAX(er.FechaInicio) DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        historial.Add(new HistorialExamenViewModel
                        {
                            ExamenId = reader.GetInt32(0),
                            TituloExamen = reader.GetString(1),
                            TotalIntentos = reader.GetInt32(2),
                            UltimoIntento = reader.GetDateTime(3)
                        });
                    }
                }
            }

            return historial;
        }

        public List<IntentoExamenViewModel> ObtenerIntentosExamen(int examenId)
        {
            var intentos = new List<IntentoExamenViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Primero obtener el total de preguntas del examen
                string queryTotalPreguntas = "SELECT COUNT(*) FROM Preguntas WHERE ExamenId = @ExamenId";
                int totalPreguntasExamen = 0;

                using (SqlCommand cmdTotal = new SqlCommand(queryTotalPreguntas, conn))
                {
                    cmdTotal.Parameters.AddWithValue("@ExamenId", examenId);
                    totalPreguntasExamen = (int)cmdTotal.ExecuteScalar();
                }

                string query = @"
            SELECT 
                er.Id,
                er.FechaInicio,
                COUNT(r.Id) AS Respondidas,
                SUM(CASE WHEN r.EsCorrecta = 1 THEN 1 ELSE 0 END) AS Correctas,
                SUM(CASE WHEN r.EsCorrecta = 0 THEN 1 ELSE 0 END) AS Incorrectas
            FROM ExamenesRealizados er
            LEFT JOIN Respuestas r ON er.Id = r.ExamenRealizadoId
            WHERE er.ExamenId = @ExamenId
            GROUP BY er.Id, er.FechaInicio
            ORDER BY er.FechaInicio DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenId", examenId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int respondidas = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            int correctas = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                            int incorrectas = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

                            // Porcentaje sobre el total de preguntas del examen
                            double porcentaje = totalPreguntasExamen > 0 ? (double)correctas / totalPreguntasExamen * 100 : 0;

                            intentos.Add(new IntentoExamenViewModel
                            {
                                ExamenRealizadoId = reader.GetInt32(0),
                                FechaRealizacion = reader.GetDateTime(1),
                                TotalPreguntasExamen = totalPreguntasExamen,
                                Respondidas = respondidas,
                                Correctas = correctas,
                                Incorrectas = incorrectas,
                                Porcentaje = Math.Round(porcentaje, 2)
                            });
                        }
                    }
                }
            }

            return intentos;
        }

        public List<RespuestaDetalleViewModel> ObtenerRespuestasIntento(int examenRealizadoId, string filtro = "todas")
        {
            var respuestas = new List<RespuestaDetalleViewModel>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"
            SELECT 
                p.OrdenPregunta,
                p.TextoPregunta,
                o.OrdenOpcion AS OrdenUsuario,
                o.TextoOpcion AS RespuestaUsuario,
                r.EsCorrecta,
                oc.OrdenOpcion AS OrdenCorrecta,
                oc.TextoOpcion AS RespuestaCorrecta,
                r.FechaRespuesta
            FROM Respuestas r
            INNER JOIN Preguntas p ON r.PreguntaId = p.Id
            INNER JOIN Opciones o ON r.OpcionSeleccionadaId = o.Id
            INNER JOIN Opciones oc ON p.Id = oc.PreguntaId AND oc.EsCorrecta = 1
            WHERE r.ExamenRealizadoId = @ExamenRealizadoId";

                // Aplicar filtro
                if (filtro == "correctas")
                {
                    query += " AND r.EsCorrecta = 1";
                }
                else if (filtro == "incorrectas")
                {
                    query += " AND r.EsCorrecta = 0";
                }

                query += " ORDER BY p.OrdenPregunta";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenRealizadoId", examenRealizadoId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var letras = new[] { "A", "B", "C", "D" };

                        while (reader.Read())
                        {
                            int ordenUsuario = reader.GetInt32(2) - 1; // OrdenOpcion es 1-based, array es 0-based
                            int ordenCorrecta = reader.GetInt32(5) - 1;

                            respuestas.Add(new RespuestaDetalleViewModel
                            {
                                NumeroPregunta = reader.GetInt32(0),
                                TextoPregunta = reader.GetString(1),
                                LetraUsuario = letras[ordenUsuario], // NUEVO
                                RespuestaUsuario = reader.GetString(3),
                                EsCorrecta = reader.GetBoolean(4),
                                LetraCorrecta = letras[ordenCorrecta], // NUEVO
                                RespuestaCorrecta = reader.GetString(6),
                                FechaRespuesta = reader.GetDateTime(7)
                            });
                        }
                    }
                }
            }

            return respuestas;
        }

        public string ObtenerTituloExamenPorIntento(int examenRealizadoId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = @"
            SELECT e.Titulo 
            FROM ExamenesRealizados er
            INNER JOIN Examenes e ON er.ExamenId = e.Id
            WHERE er.Id = @ExamenRealizadoId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenRealizadoId", examenRealizadoId);
                    return cmd.ExecuteScalar()?.ToString() ?? "Examen";
                }
            }
        }

        public int ObtenerExamenIdPorIntento(int examenRealizadoId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string query = "SELECT ExamenId FROM ExamenesRealizados WHERE Id = @ExamenRealizadoId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenRealizadoId", examenRealizadoId);
                    var result = cmd.ExecuteScalar();
                    return result != null ? (int)result : 0;
                }
            }
        }
    }
}
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

                string query = @"INSERT INTO Respuestas (ExamenRealizadoId, PreguntaId, OpcionSeleccionadaId, EsCorrecta)
                        VALUES (@ExamenRealizadoId, @PreguntaId, @OpcionId, @EsCorrecta)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ExamenRealizadoId", examenRealizadoId);
                    cmd.Parameters.AddWithValue("@PreguntaId", preguntaId);
                    cmd.Parameters.AddWithValue("@OpcionId", opcionId);
                    cmd.Parameters.AddWithValue("@EsCorrecta", esCorrecta);
                    cmd.ExecuteNonQuery();
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
    }
}
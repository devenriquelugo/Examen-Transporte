

-- Crear la base de datos
CREATE DATABASE ExamenTransporte;
GO

USE ExamenTransporte;
GO

-- 1. EXAMENES (Catálogo de exámenes disponibles)
CREATE TABLE Examenes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Titulo NVARCHAR(200) NOT NULL,
    FechaCreacion DATETIME DEFAULT GETDATE(),
    Activo BIT DEFAULT 1
);

-- 2. PREGUNTAS (Preguntas de cada examen)
CREATE TABLE Preguntas (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ExamenId INT NOT NULL,
    TextoPregunta NVARCHAR(1000) NOT NULL,
    OrdenPregunta INT NOT NULL,
    Puntos DECIMAL(5,2) DEFAULT 1.0,
    FOREIGN KEY (ExamenId) REFERENCES Examenes(Id)
);

-- 3. OPCIONES (Opciones de respuesta para cada pregunta)
CREATE TABLE Opciones (
    Id INT PRIMARY KEY IDENTITY(1,1),
    PreguntaId INT NOT NULL,
    TextoOpcion NVARCHAR(500) NOT NULL,
    EsCorrecta BIT NOT NULL DEFAULT 0,
    OrdenOpcion INT NOT NULL,
    FOREIGN KEY (PreguntaId) REFERENCES Preguntas(Id)
);

-- 4. EXAMENES_REALIZADOS (Historial de intentos de examen)
CREATE TABLE ExamenesRealizados (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ExamenId INT NOT NULL,
    FechaInicio DATETIME NOT NULL DEFAULT GETDATE(),
    FechaFin DATETIME NULL,
    TiempoTranscurridoMinutos INT NULL,
    Puntuacion DECIMAL(5,2) NULL,
    PuntuacionMaxima DECIMAL(5,2) NULL,
    Completado BIT DEFAULT 0,
    FOREIGN KEY (ExamenId) REFERENCES Examenes(Id)
);

-- 5. RESPUESTAS (Respuestas dadas en cada examen)
CREATE TABLE Respuestas (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ExamenRealizadoId INT NOT NULL,
    PreguntaId INT NOT NULL,
    OpcionSeleccionadaId INT NOT NULL,
    EsCorrecta BIT NOT NULL,
    FechaRespuesta DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ExamenRealizadoId) REFERENCES ExamenesRealizados(Id),
    FOREIGN KEY (PreguntaId) REFERENCES Preguntas(Id),
    FOREIGN KEY (OpcionSeleccionadaId) REFERENCES Opciones(Id)
);

-- Índices para mejorar el rendimiento
CREATE INDEX IX_Preguntas_ExamenId ON Preguntas(ExamenId);
CREATE INDEX IX_Opciones_PreguntaId ON Opciones(PreguntaId);
CREATE INDEX IX_ExamenesRealizados_Examen ON ExamenesRealizados(ExamenId);
CREATE INDEX IX_Respuestas_ExamenRealizado ON Respuestas(ExamenRealizadoId);
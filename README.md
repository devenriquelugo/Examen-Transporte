\# 🚛 Sistema de Exámenes de Transporte



Sistema web completo para la gestión y realización de exámenes de competencia profesional de transporte de mercancías por carretera.



!\[.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)

!\[C#](https://img.shields.io/badge/C%23-Language-239120?logo=csharp)

!\[SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?logo=microsoftsqlserver)

!\[Tailwind CSS](https://img.shields.io/badge/Tailwind-CSS-06B6D4?logo=tailwindcss)



---



\## 📋 Características



\- ✅ \*\*Carga de exámenes\*\* desde archivos Word (.docx)

\- ✅ \*\*Procesamiento inteligente\*\* de preguntas y respuestas

\- ✅ \*\*Realización de exámenes\*\* pregunta por pregunta

\- ✅ \*\*Navegación flexible\*\* (avanzar, retroceder, comprobar)

\- ✅ \*\*Historial completo\*\* de intentos con estadísticas

\- ✅ \*\*Filtros de revisión\*\* (todas/correctas/incorrectas)

\- ✅ \*\*Diseño responsive\*\* para móvil y desktop

\- ✅ \*\*Interfaz moderna\*\* con Tailwind CSS



---



\## 🛠️ Tecnologías Utilizadas



\### Backend

\- \*\*ASP.NET Core 8.0\*\* (MVC)

\- \*\*C#\*\* como lenguaje principal

\- \*\*ADO.NET\*\* para acceso a datos

\- \*\*SQL Server\*\* como base de datos



\### Frontend

\- \*\*Razor Pages\*\* (Cshtml)

\- \*\*Tailwind CSS\*\* para estilos

\- \*\*JavaScript\*\* para interactividad



\### Librerías

\- \*\*DocX\*\* (Xceed) para procesamiento de archivos Word

\- \*\*System.Text.Json\*\* para serialización

\- \*\*Sessions\*\* para manejo de estado



---



\## 📦 Requisitos Previos



Antes de comenzar, asegúrate de tener instalado:



\- \[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

\- \[SQL Server 2019+](https://www.microsoft.com/sql-server/sql-server-downloads) o SQL Server Express

\- \[Visual Studio 2022](https://visualstudio.microsoft.com/) (recomendado) o VS Code

\- \[Git](https://git-scm.com/)



---



\## 🚀 Instalación



\### 1. Clonar el repositorio

```bash

git clone https://github.com/devenriquelugo/Examen-Transporte.git

cd Examen-Transporte

```



\### 2. Configurar la base de datos



Ejecuta el script SQL en SQL Server Management Studio:

Data/Script/BBDD\_Tablas.sql



\### 3. Configurar conexión



Edita `appsettings.json` con tu cadena de conexión:

```json

{

&nbsp; "ConnectionStrings": {

&nbsp;   "DefaultConnection": "Server=TU\_SERVIDOR;Database=ExamenTransporte;Trusted\_Connection=True;TrustServerCertificate=True;"

&nbsp; }

}

```



\### 4. Restaurar paquetes

```bash

dotnet restore

```



\### 5. Ejecutar la aplicación

```bash

dotnet run

```



O desde Visual Studio: presiona \*\*F5\*\*



La aplicación estará disponible en: `https://localhost:5001`



---



\## 📖 Uso



\### Cargar Exámenes



1\. Ve a \*\*"Cargar Examen"\*\*

2\. Sube dos archivos:

&nbsp;  - Archivo de preguntas (`.docx`)

&nbsp;  - Archivo de respuestas correctas (`.docx`)

3\. El sistema procesará automáticamente el contenido



\*\*Formato esperado del archivo de preguntas:\*\*

```

1.- Pregunta: ¿Texto de la pregunta?

A Opción A

B Opción B

C Opción C

D Opción D



2.- Pregunta: ¿Otra pregunta?

...

```



\*\*Formato esperado del archivo de respuestas:\*\*

```

1 B

2 A

3 D

...

```



\### Realizar Exámenes



1\. Ve a \*\*"Realizar Examen"\*\*

2\. Selecciona un examen disponible

3\. Responde pregunta por pregunta

4\. Usa los botones:

&nbsp;  - \*\*Atrás\*\*: Volver a la pregunta anterior

&nbsp;  - \*\*Comprobar\*\*: Verificar si tu respuesta es correcta

&nbsp;  - \*\*Siguiente\*\*: Avanzar a la siguiente pregunta



\### Ver Historial



1\. Ve a \*\*"Historial de Exámenes"\*\*

2\. Selecciona un examen

3\. Ve tus intentos con estadísticas

4\. Filtra por:

&nbsp;  - \*\*Todas\*\*: Ver todas las respuestas

&nbsp;  - \*\*Correctas\*\*: Solo respuestas correctas

&nbsp;  - \*\*Incorrectas\*\*: Solo respuestas incorrectas



---



\## 📁 Estructura del Proyecto

```

ExamenTransporte/

├── Controllers/

│   ├── CargaController.cs      # Carga de exámenes

│   ├── ExamenController.cs     # Realización e historial

│   └── HomeController.cs       # Página principal

├── Models/

│   ├── CargaExamenViewModel.cs

│   ├── ExamenViewModel.cs

│   └── HistorialViewModels.cs

├── Data/

│   └── ExamenRepository.cs     # Acceso a datos (ADO.NET)

├── Views/

│   ├── Home/

│   ├── Carga/

│   └── Examen/

│       ├── Index.cshtml        # Lista de exámenes

│       ├── Pregunta.cshtml     # Vista de pregunta

│       ├── Historial.cshtml    # Historial general

│       └── ...

├── wwwroot/

│   ├── css/

│   ├── js/

│   └── lib/

├── appsettings.json            # Configuración

└── Program.cs                  # Punto de entrada

```



---



\## 🎨 Paleta de Colores



El sistema utiliza una paleta personalizada:



\- \*\*Azul primario\*\*: `#2f6cd6`

\- \*\*Verde éxito\*\*: `#8cf26a`

\- \*\*Rojo error\*\*: `#f26a6a`



---



\## 🖼️ Capturas de Pantalla



\### Página Principal

!\[Home](docs/screenshots/home.png)



\### Realizar Examen

!\[Examen](docs/screenshots/examen.png)



\### Historial

!\[Historial](docs/screenshots/historial.png)



---



\## 🤝 Contribución



Las contribuciones son bienvenidas. Por favor:



1\. Fork el proyecto

2\. Crea una rama (`git checkout -b feature/nueva-caracteristica`)

3\. Commit tus cambios (`git commit -m 'Agregar nueva característica'`)

4\. Push a la rama (`git push origin feature/nueva-caracteristica`)

5\. Abre un Pull Request



---



\## 👨‍💻 Desarrollado por Enrique Lugo de BarnaStudio



\*\*\[BarnaStudio](https://barnastudio.com)\*\*



\- Sitio web: \[barnastudio.com](https://barnastudio.com)

\- GitHub: \[@devenriquelugo](https://github.com/devenriquelugo)



---



\## 📞 Soporte



Si tienes alguna pregunta o problema, por favor abre un \[issue](https://github.com/devenriquelugo/Examen-Transporte/issues).



---



⭐ Si te gusta este proyecto, dale una estrella en GitHub!


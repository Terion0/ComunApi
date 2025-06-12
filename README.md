# 🌐 API de Comunidades ASP.NET Core con JWT

Esta API proporciona funcionalidades para gestionar comunidades con un sistema completo de autenticación basado en JWT para proteger los endpoints.

---

## ⚙️ Configuración de entorno (`appsettings.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",               // Nivel de log general (info y superior)
      "Microsoft.AspNetCore": "Warning"      // Nivel de log específico para ASP.NET Core (warnings y errores)
    }
  },
  "AllowedHosts": "*",                      // Define qué hosts pueden hacer peticiones a la API. "*" permite cualquiera.

  "DbSettings": {
    "Host": "postgres",                     // Dirección del servidor de base de datos PostgreSQL (p. ej. localhost o nombre del servicio en Docker)
    "Port": 5433,                          // Puerto donde escucha PostgreSQL
    "Username": "postgres",                 // Usuario para conectarse a la base de datos
    "Password": "postgres",                 // Contraseña para el usuario de la base de datos
    "Database": "postgres"                  // Nombre de la base de datos usada por la API
  },

  "JwtSettings": {
    "SecretKey": "llavesecreta"             // Clave secreta usada para firmar y validar los tokens JWT (mantener en secreto)
  }
}

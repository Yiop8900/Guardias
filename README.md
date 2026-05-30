# Guardias

Aplicación web **ASP.NET Core MVC** para la gestión de rondas de vigilancia, guardias, incidencias y tareas en edificios. Permite a los operadores registrar rondas con fotos por área, reportar incidencias y a los supervisores firmar y exportar reportes en PDF.

## Características

- **Gestión de rondas**: registro de rondas por edificio y área, con estados (En curso, Finalizada, Firmada, Reporte directo).
- **Fotos por área**: captura de evidencia fotográfica en cada área recorrida, almacenada en la nube (Cloudinary / Google Drive).
- **Incidencias**: registro y seguimiento de incidencias con severidad (Leve, Moderada, Grave) y estado (Abierta, En proceso, Cerrada).
- **Tareas**: asignación y seguimiento de tareas pendientes para guardias.
- **Firma de supervisor**: las rondas pueden ser firmadas por un supervisor.
- **Exportación a PDF**: generación de reportes de ronda.
- **Panel de administración**: gestión de edificios, áreas, guardias, usuarios, tareas e incidencias.
- **Autenticación por cookies**: acceso protegido con sesión de 12 horas.

## Tecnologías

| Componente        | Tecnología                          |
| ----------------- | ----------------------------------- |
| Framework         | ASP.NET Core MVC (.NET 10)          |
| ORM               | Entity Framework Core 9             |
| Base de datos     | SQL Server                          |
| Autenticación     | Cookies (`CookieAuthentication`)    |
| Almacenamiento    | Cloudinary, Google Drive API        |

## Estructura del proyecto

```
Guardias/
├── Controllers/        # Controladores MVC (Account, Admin, Ronda, Incidencia, Tarea, Pendientes...)
├── Data/
│   ├── AppDbContext.cs # Contexto EF Core y configuración de relaciones
│   └── Migrations/     # Migraciones de base de datos
├── Models/             # Entidades del dominio (Edificio, Guardia, Ronda, Incidencia, Tarea, Area...)
├── Services/           # Servicios externos (CloudinaryService, GoogleDriveService)
├── Views/              # Vistas Razor
├── wwwroot/            # Recursos estáticos (css, js, lib, uploads)
└── Program.cs          # Punto de entrada y configuración
```

### Modelo de dominio

- **Edificio**: contiene áreas, guardias, rondas, tareas y usuarios.
- **Guardia**: operador asignado a un edificio y turno (Mañana, Tarde, Noche).
- **Ronda**: recorrido de un guardia por las áreas de un edificio.
- **AreaRonda**: relación entre una ronda y un área recorrida, con sus fotos e incidencias.
- **FotoRonda**: evidencia fotográfica asociada a un área de ronda.
- **Incidencia**: evento reportado durante una ronda.
- **Tarea**: actividad pendiente asignada a un guardia.

## Requisitos previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local o remoto)
- Cuenta de [Cloudinary](https://cloudinary.com/) (opcional, para subida de imágenes)
- Credenciales de cuenta de servicio de Google Drive (opcional)

## Configuración

1. Clona el repositorio:

   ```bash
   git clone <url-del-repositorio>
   cd Guardias
   ```

2. Configura las variables en `Guardias/appsettings.json` o, preferiblemente, mediante [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=...;Database=...;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=True;"
     },
     "Cloudinary": {
       "CloudName": "tu-cloud-name",
       "ApiKey": "tu-api-key",
       "ApiSecret": "tu-api-secret"
     },
     "Admin": {
       "Usuario": "admin",
       "Password": "una-contraseña-segura"
     }
   }
   ```

3. (Opcional) Coloca el archivo de credenciales de Google en `Guardias/credentials/service-account.json`.

> ⚠️ **Seguridad**: No subas credenciales reales al control de versiones. Usa User Secrets en desarrollo y variables de entorno en producción. Si ya hay credenciales expuestas en `appsettings.json`, rótalas.

## Ejecución

Las migraciones se aplican automáticamente al iniciar la aplicación (`db.Database.Migrate()`).

```bash
cd Guardias
dotnet restore
dotnet run
```

La aplicación quedará disponible en la URL indicada en `Properties/launchSettings.json` (normalmente `https://localhost:5001`). La ruta raíz redirige al historial de rondas y requiere autenticación.

## Migraciones de base de datos

Para crear una nueva migración tras modificar los modelos:

```bash
dotnet ef migrations add NombreDeLaMigracion
dotnet ef database update
```

> Requiere la herramienta `dotnet-ef`: `dotnet tool install --global dotnet-ef`

## Rutas principales

| Ruta                     | Descripción                          |
| ------------------------ | ------------------------------------ |
| `/` o `/Ronda/Historial` | Historial de rondas (requiere login) |
| `/Account/Login`         | Inicio de sesión                     |
| `/Admin`                 | Panel de administración              |

## Licencia

Este proyecto no especifica una licencia. Añade un archivo `LICENSE` si deseas definir las condiciones de uso.

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using DrivePermission = Google.Apis.Drive.v3.Data.Permission;

namespace Guardias.Services;

public class GoogleDriveService
{
    private readonly DriveService? _drive;
    private readonly string _folderId;
    private readonly ILogger<GoogleDriveService> _logger;

    public bool IsConfigured { get; }

    public GoogleDriveService(IConfiguration config, ILogger<GoogleDriveService> logger)
    {
        _logger = logger;
        _folderId = config["GoogleDrive:FolderId"] ?? string.Empty;
        var credPath = config["GoogleDrive:CredentialsPath"] ?? "credentials/service-account.json";

        if (string.IsNullOrWhiteSpace(_folderId))
        {
            _logger.LogWarning("Google Drive no configurado (FolderId vacío). Las fotos se guardarán localmente.");
            return;
        }

        if (!File.Exists(credPath))
        {
            _logger.LogWarning("Google Drive: archivo de credenciales no encontrado en '{Path}'. Las fotos se guardarán localmente.", credPath);
            return;
        }

        try
        {
            using var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read);
            var credential = GoogleCredential.FromStream(stream)
                .CreateScoped(DriveService.Scope.DriveFile);

            _drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Guardias"
            });

            IsConfigured = true;
            _logger.LogInformation("Google Drive inicializado correctamente. Carpeta destino: {FolderId}", _folderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar Google Drive. Las fotos se guardarán localmente.");
        }
    }

    /// <summary>
    /// Sube un archivo a Google Drive y lo hace público.
    /// Devuelve (fileId, urlDirecta).
    /// </summary>
    public async Task<(string FileId, string Url)> UploadAsync(
        Stream fileStream, string fileName, string contentType)
    {
        if (!IsConfigured || _drive is null)
            throw new InvalidOperationException("Google Drive no está configurado.");

        var metadata = new DriveFile
        {
            Name = fileName,
            Parents = new List<string> { _folderId }
        };

        var request = _drive.Files.Create(metadata, fileStream, contentType);
        request.Fields = "id";

        var progress = await request.UploadAsync();
        if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
            throw new Exception($"Error subiendo archivo a Drive: {progress.Exception?.Message}");

        var fileId = request.ResponseBody.Id;

        // Hacer el archivo público (solo lectura)
        var permission = new DrivePermission { Role = "reader", Type = "anyone" };
        await _drive.Permissions.Create(permission, fileId).ExecuteAsync();

        // URL directa para embeber imágenes
        var url = $"https://drive.google.com/thumbnail?id={fileId}&sz=w1200";
        return (fileId, url);
    }

    /// <summary>
    /// Elimina un archivo de Google Drive por su ID.
    /// </summary>
    public async Task DeleteAsync(string fileId)
    {
        if (!IsConfigured || _drive is null || string.IsNullOrEmpty(fileId)) return;
        try
        {
            await _drive.Files.Delete(fileId).ExecuteAsync();
            _logger.LogInformation("Archivo de Drive eliminado: {FileId}", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar el archivo de Drive {FileId}", fileId);
        }
    }
}

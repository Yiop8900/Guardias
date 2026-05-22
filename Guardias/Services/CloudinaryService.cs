using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Guardias.Services;

public class CloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    public bool IsConfigured { get; }

    public CloudinaryService(IConfiguration config, ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        var cloudName = config["Cloudinary:CloudName"] ?? string.Empty;
        var apiKey    = config["Cloudinary:ApiKey"]    ?? string.Empty;
        var apiSecret = config["Cloudinary:ApiSecret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
        {
            _logger.LogWarning("Cloudinary no configurado. Las fotos se guardarán localmente.");
            return;
        }

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        _cloudinary.Api.Secure = true;
        IsConfigured = true;
        _logger.LogInformation("Cloudinary inicializado correctamente. Cloud: {CloudName}", cloudName);
    }

    /// <summary>
    /// Sube una imagen a Cloudinary.
    /// Devuelve (publicId, urlSegura).
    /// </summary>
    public async Task<(string PublicId, string Url)> UploadAsync(
        Stream fileStream, string fileName, string contentType)
    {
        if (!IsConfigured || _cloudinary is null)
            throw new InvalidOperationException("Cloudinary no está configurado.");

        var publicId = $"guardias-rondas/{Path.GetFileNameWithoutExtension(fileName)}";

        var uploadParams = new ImageUploadParams
        {
            File       = new FileDescription(fileName, fileStream),
            PublicId   = publicId,
            Overwrite  = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error is not null)
            throw new Exception($"Error subiendo imagen a Cloudinary: {result.Error.Message}");

        return (result.PublicId, result.SecureUrl.ToString());
    }

    /// <summary>
    /// Elimina una imagen de Cloudinary por su PublicId.
    /// </summary>
    public async Task DeleteAsync(string publicId)
    {
        if (!IsConfigured || _cloudinary is null) return;

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
        if (result.Error is not null)
            _logger.LogWarning("No se pudo eliminar imagen de Cloudinary ({PublicId}): {Error}", publicId, result.Error.Message);
    }
}

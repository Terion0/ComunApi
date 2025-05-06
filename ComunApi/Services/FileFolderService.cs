using Microsoft.AspNetCore.Mvc;

namespace ProfApi.Services
{
   public class FileFolderService
   {
    private readonly ILogger<FileFolderService> _logger;

    public readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png" };
   




        public FileFolderService(ILogger<FileFolderService> logger)
    {
        _logger = logger;
    }

        public bool IsValidExtension(string fileExtension)
        {
            fileExtension = fileExtension.ToLower();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Extensión de archivo no permitida.");
                return false;
            }
            return true;
        }
      

        public bool IsValidFileSize(long fileSize, long maxSize)
        {
            if (fileSize > maxSize)
            {
                _logger.LogWarning("El archivo excede el tamaño máximo.");
                return false;
            }
            return true;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string name, string targetFolder)
        {
            try
            {
                var fileName = $"{name}_{file.FileName}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), targetFolder, fileName);
                var directoryPath = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return $"/{targetFolder}/{fileName}"; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar el archivo.");
                return null;
            }
        }
        public bool DeleteFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("Archivo eliminado");
                    return true;
                }
                else {
                    _logger.LogInformation("No existe la ruta");
                    return false;

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al eliminar archivo. Excepción: {ex.Message}");
                return false;
            }
        }

    }
}

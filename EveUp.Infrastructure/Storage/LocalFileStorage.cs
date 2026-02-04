using EveUp.Core.Interfaces;
using EveUp.Core.Validators;

namespace EveUp.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _storagePath;
    private readonly string _baseUrl;

    public LocalFileStorage(string storagePath, string baseUrl)
    {
        _storagePath = storagePath;
        _baseUrl = baseUrl.TrimEnd('/');

        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> StoreAsync(Stream fileStream, string fileName, string contentType)
    {
        // SECURITY: Validate file before storing
        var (isValid, errorMessage) = FileValidator.Validate(fileStream, fileName);
        if (!isValid)
            throw new InvalidOperationException($"File validation failed: {errorMessage}");

        // Gera nome único para evitar colisões
        var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        // Reset stream position after validation
        fileStream.Position = 0;

        using var fileStreamOut = File.Create(filePath);
        await fileStream.CopyToAsync(fileStreamOut);

        return $"{_baseUrl}/files/{uniqueFileName}";
    }

    public Task DeleteAsync(string fileUrl)
    {
        var fileName = ExtractFileNameFromUrl(fileUrl);
        var filePath = Path.Combine(_storagePath, fileName);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string fileUrl)
    {
        var fileName = ExtractFileNameFromUrl(fileUrl);
        var filePath = Path.Combine(_storagePath, fileName);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<Stream> GetAsync(string fileUrl)
    {
        var fileName = ExtractFileNameFromUrl(fileUrl);
        var filePath = Path.Combine(_storagePath, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {fileUrl}");

        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string ExtractFileNameFromUrl(string fileUrl)
    {
        return fileUrl.Split('/').Last();
    }
}

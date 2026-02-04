namespace EveUp.Core.Validators;

/// <summary>
/// Validador de arquivos para prevenir upload de malware e arquivos maliciosos
/// </summary>
public static class FileValidator
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    // Whitelist de extensões permitidas
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    // Magic numbers para validação de tipo real do arquivo
    private static readonly Dictionary<string, byte[][]> FileSignatures = new()
    {
        { ".pdf", new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }, // %PDF
        { ".jpg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new[] { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new[] { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".webp", new[] { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }
    };

    /// <summary>
    /// Valida se o arquivo é seguro para upload
    /// </summary>
    public static (bool isValid, string? errorMessage) Validate(Stream fileStream, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return (false, "File name is required");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // 1. Validar extensão
        if (!AllowedExtensions.Contains(extension))
            return (false, $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");

        // 2. Validar tamanho
        if (fileStream.Length > MaxFileSizeBytes)
            return (false, $"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB");

        if (fileStream.Length == 0)
            return (false, "File is empty");

        // 3. Validar magic numbers (tipo real do arquivo)
        if (!ValidateFileSignature(fileStream, extension))
            return (false, "File content does not match its extension. Possible file type forgery.");

        return (true, null);
    }

    private static bool ValidateFileSignature(Stream stream, string extension)
    {
        if (!FileSignatures.TryGetValue(extension, out var signatures))
            return true; // Se não temos signature definida, aceita (fail-open para extensões novas)

        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            foreach (var signature in signatures)
            {
                var buffer = new byte[signature.Length];
                var bytesRead = stream.Read(buffer, 0, signature.Length);

                if (bytesRead == signature.Length && buffer.SequenceEqual(signature))
                {
                    return true;
                }

                stream.Position = 0; // Reset para próxima signature
            }

            return false;
        }
        finally
        {
            stream.Position = originalPosition; // Restaura posição original
        }
    }
}

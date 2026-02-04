namespace EveUp.Core.Interfaces;

/// <summary>
/// Interface para armazenamento de arquivos (evidências, anexos, etc.)
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Armazena um arquivo e retorna a URL
    /// </summary>
    Task<string> StoreAsync(Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Remove um arquivo
    /// </summary>
    Task DeleteAsync(string fileUrl);

    /// <summary>
    /// Verifica se um arquivo existe
    /// </summary>
    Task<bool> ExistsAsync(string fileUrl);

    /// <summary>
    /// Obtém um arquivo como stream
    /// </summary>
    Task<Stream> GetAsync(string fileUrl);
}

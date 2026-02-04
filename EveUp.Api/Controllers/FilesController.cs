using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".mp4", ".mov",
        ".mp3", ".m4a", ".wav", ".ogg",
        ".pdf", ".doc", ".docx"
    };

    private const long MaxFileSize = 25 * 1024 * 1024; // 25MB

    private readonly IWebHostEnvironment _env;

    public FilesController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Upload de arquivo para chat (imagens, áudio, vídeos, documentos)
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(26_214_400)] // 25MB + overhead
    public async Task<ActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Nenhum arquivo enviado." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = "Arquivo excede o tamanho máximo de 25MB." });

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            return BadRequest(new { error = $"Tipo de arquivo não permitido: {extension}" });

        // Validar magic bytes do arquivo
        if (!ValidateFileContent(file, extension))
            return BadRequest(new { error = "O conteúdo do arquivo não corresponde à extensão informada." });

        // Gerar nome único
        var userId = GetUserId();
        var fileName = $"{Guid.NewGuid()}{extension}";
        var relativePath = Path.Combine("uploads", DateTime.UtcNow.ToString("yyyy-MM"), fileName);

        // Salvar no wwwroot
        var fullPath = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), relativePath);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Retornar URL relativa
        var url = $"/uploads/{DateTime.UtcNow:yyyy-MM}/{fileName}";

        return Ok(new
        {
            url,
            fileName = file.FileName,
            fileSize = file.Length,
            contentType = file.ContentType,
        });
    }

    private static bool ValidateFileContent(IFormFile file, string extension)
    {
        // Magic bytes para tipos comuns
        var signatures = new Dictionary<string, byte[][]>(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", [new byte[] { 0xFF, 0xD8, 0xFF }] },
            { ".jpeg", [new byte[] { 0xFF, 0xD8, 0xFF }] },
            { ".png", [new byte[] { 0x89, 0x50, 0x4E, 0x47 }] },
            { ".gif", [new byte[] { 0x47, 0x49, 0x46 }] },
            { ".pdf", [new byte[] { 0x25, 0x50, 0x44, 0x46 }] },
            { ".mp3", [new byte[] { 0x49, 0x44, 0x33 }, new byte[] { 0xFF, 0xFB }, new byte[] { 0xFF, 0xF3 }] },
            { ".wav", [new byte[] { 0x52, 0x49, 0x46, 0x46 }] },
            { ".ogg", [new byte[] { 0x4F, 0x67, 0x67, 0x53 }] },
            { ".mp4", [new byte[] { 0x00, 0x00, 0x00 }] },  // ftyp box (3rd byte varies)
        };

        // Se não temos assinatura para este tipo, permitir (doc/docx/m4a/mov/webp)
        if (!signatures.TryGetValue(extension, out var validSignatures))
            return true;

        using var reader = new BinaryReader(file.OpenReadStream());
        var headerBytes = reader.ReadBytes(8);
        file.OpenReadStream().Position = 0;

        return validSignatures.Any(sig =>
            headerBytes.Length >= sig.Length &&
            headerBytes.Take(sig.Length).SequenceEqual(sig));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}

namespace EveUp.Core.Interfaces;

/// <summary>
/// Abstração para capturar informações do usuário atual e contexto de execução
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}

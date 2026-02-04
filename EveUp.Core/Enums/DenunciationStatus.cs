namespace EveUp.Core.Enums;

public enum DenunciationStatus
{
    Open = 0,                    // Denúncia aberta, aguardando contestação
    AwaitingContestation = 1,    // Prazo de contestação expirado, aguardando revisão
    Contested = 2,               // Denunciado contestou
    InReview = 3,                // Em análise pelo admin
    ResolvedProInitiator = 4,    // Resolvida a favor do denunciante
    ResolvedProTarget = 5,       // Resolvida a favor do denunciado
    Dismissed = 6                // Denúncia descartada/improcedente
}

namespace EveUp.Core.Enums;

public enum AttendanceStatus
{
    Pending = 0,      // Aguardando check-in
    CheckedIn = 1,    // Check-in realizado
    CheckedOut = 2,   // Check-out realizado
    Confirmed = 3,    // Presença confirmada (24h sem contestação)
    Contested = 4,    // Presença contestada pela empresa
    NoShow = 5        // Profissional não compareceu
}

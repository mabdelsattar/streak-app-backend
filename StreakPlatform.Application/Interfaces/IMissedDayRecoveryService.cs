using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public interface IMissedDayRecoveryService
{
    /// <summary>Compute outstanding debt for the current user on this streak.</summary>
    Task<DebtDto> GetDebtAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);

    /// <summary>
    /// Pay the recovery fee for all outstanding missed days. Records one StreakProtection row
    /// (Status=Used, PointsCost=MissedDayRecoveryCost) per covered day and a single PointsTransaction.
    /// </summary>
    Task<PayDebtResultDto> PayDebtAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);

    /// <summary>
    /// Soft-kick the current user from the streak (Participant.IsActive = false). History preserved.
    /// </summary>
    Task LeaveAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
}

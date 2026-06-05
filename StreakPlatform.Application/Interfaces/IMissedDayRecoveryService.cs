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

    /// <summary>
    /// Called automatically on every login. Scans all active streaks for this user,
    /// creates StreakProtection rows for every missed day, and deducts the total cost
    /// from the user's balance (clamped at 0).
    /// Returns the new balance and whether it was insufficient to cover the full debt.
    /// </summary>
    Task<(int NewBalance, bool NeedsToBuyPoints)> DeductOnLoginAsync(Guid userId, CancellationToken ct = default);
}

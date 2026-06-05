namespace StreakPlatform.Domain.Entities;

public enum PointsTransactionReason
{
    StartingGrant = 0,
    CheckInReward = 1,
    ProtectionPurchase = 2,
    Restore = 3,
    Refund = 4,
    ReactionGiven = 5,
    ReactionReceivedLike = 6,
    ReactionReceivedDislike = 7,
    MissedDayRecovery = 8,
    Purchase = 9,
    StreakCreation = 10
}

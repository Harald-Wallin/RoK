public enum FavourState
{
    Unavailable,
    Available,
    Active,
    ReadyToTurnIn,
    Completed,
    Failed,
    Cooldown
}

public enum FavourType
{
    General,
    Slay,
    Collect,
    Deliver,
    Escort,
    Exploration,
    Interaction
}

public enum FavourActivationPolicy
{
    ExplicitAccept,
    DiscoverOnInteraction,
    TrackBeforeDiscovery
}

public enum FavourCompletionPolicy
{
    Automatic,
    ReturnToGiver,
    CompleteAtTarget,
    CompleteAtWorldObject
}

public enum FavourFailurePolicy
{
    None,
    RetryImmediately,
    RetryAfterCooldown,
    PermanentFailure,
    ResetObjectivesAndRetry
}

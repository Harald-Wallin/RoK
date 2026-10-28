public enum TargetingFailureReason
{
    None,

    MissingCaster,
    MissingAction,
    MissingTarget,

    TargetInvalid,
    TargetDead,
    TargetNotAllowed,

    TooClose,
    OutOfRange,

    NoLineOfSight,
    Blocked,

    NoValidTargets
}

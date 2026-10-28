using System;

[Flags]
public enum TargetRelation
{
    None = 0,

    Self = 1 << 0,
    Friendly = 1 << 1,
    Hostile = 1 << 2,
    Neutral = 1 << 3,

    Any =
        Self |
        Friendly |
        Hostile |
        Neutral
}

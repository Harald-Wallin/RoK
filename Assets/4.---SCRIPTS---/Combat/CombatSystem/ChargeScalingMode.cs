using System;

[Flags]
public enum ChargeScalingMode
{
    None = 0,

    Damage = 1 << 0,

    Range = 1 << 1
}


public readonly struct EffectContext
{
    public CharacterStats Target { get; }

    public DamageSourceContext DamageSource { get; }

    public ModifierSourceType ModifierSourceType
    {
        get;
    }

    public EffectContext(
        CharacterStats target,
        DamageSourceContext damageSource,
        ModifierSourceType modifierSourceType)
    {
        Target = target;

        DamageSource = damageSource;

        ModifierSourceType =
            modifierSourceType;
    }
}


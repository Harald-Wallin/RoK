using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Bleed"
)]
public sealed class BleedEffect :
    AbilityEffect
{
    [Min(0)]
    public int damagePerTick = 2;

    [Min(0f)]
    public float duration = 6f;

    [Min(0.01f)]
    public float tickInterval = 2f;

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Target == null)
        {
            return;
        }

        BuffSystem buffs =
            context.Target.GetComponent<BuffSystem>();

        buffs?.ApplyEffect(
            this,
            context.DamageSource
        );
    }

    public override ActiveBuff CreateActiveBuff(
        DamageSourceContext source,
        CharacterStats target)
    {
        return new ActiveBleed(
            this,
            source
        );
    }

    public override ActiveBuff CreateActiveBuff(
        CharacterStats source,
        CharacterStats target)
    {
        return new ActiveBleed(
            this,
            DamageSourceContext.FromDirectSource(
                source
            )
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        return
            $"Bleed: {damagePerTick} damage every " +
            $"{tickInterval:0.#}s for {duration:0.#}s";
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        damagePerTick =
            Mathf.Max(
                0,
                damagePerTick
            );

        duration =
            Mathf.Max(
                0f,
                duration
            );

        tickInterval =
            Mathf.Max(
                0.01f,
                tickInterval
            );
    }
#endif
}
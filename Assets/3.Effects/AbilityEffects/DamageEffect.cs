
using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Damage"
)]
public sealed class DamageEffect :
    AbilityEffect
{
    [Header("Damage")]
    [SerializeField]
    [Min(0)]
    private int flatDamage;

    [SerializeField]
    private DamageScalingEntry[] scaling;

    [SerializeField]
    private DamageType damageType =
        DamageType.Physical;

    [Header("Rules")]
    [SerializeField]
    [Tooltip(
        "Bypasses armor, block, crit and " +
        "normal resistance calculations."
    )]
    private bool dealsRawDamage;

    // =====================================================
    // BASE DAMAGE
    // =====================================================

    public int CalculateBaseDamage(
        CharacterStats caster,
        AbilityData ability = null,
        float chargeProgress = 1f)
    {
        float damage =
            Mathf.Max(
                0,
                flatDamage
            );

        if (scaling != null)
        {
            for (int i = 0;
                 i < scaling.Length;
                 i++)
            {
                DamageScalingEntry entry =
                    scaling[i];

                if (entry == null)
                    continue;

                damage += entry.Evaluate(
                    caster
                );
            }
        }

        if (ability != null &&
            ability.ChargeSettings != null)
        {
            float chargeMultiplier =
                ability.ChargeSettings
                    .GetDamageMultiplier(
                        chargeProgress
                    );

            damage *= chargeMultiplier;
        }

        return Mathf.Max(
            0,
            Mathf.RoundToInt(damage)
        );
    }

    // =====================================================
    // EXECUTION
    // =====================================================

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Caster == null ||
            context.Target == null)
        {
            return;
        }

        if (!context.TargetWasSuccessful)
            return;

        int damage =
            CalculateBaseDamage(
                context.Caster,
                context.Ability,
                context.ChargeProgress
            );

        if (damage <= 0)
            return;

        DamageSourceContext source =
            context.DamageSource;

        if (dealsRawDamage)
        {
            CombatResolver.DealRawDamage(
                source,
                context.Target,
                damage
            );

            return;
        }

        DamageResult result =
            CombatResolver
                .ResolveDamageAfterSuccessfulHit(
                    context.Caster,
                    context.Target,
                    damage,
                    context.Ability,
                    damageType,
                    source
                );

        context.Target.TakeDamage(
            result,
            source
        );
    }

    // =====================================================
    // TOOLTIP
    // =====================================================

    public override string GetTooltipText(
        CharacterStats caster)
    {
        int damage =
            CalculateBaseDamage(
                caster,
                null,
                1f
            );

        return dealsRawDamage
            ? $"Deals {damage} raw damage"
            : $"Deals approximately {damage} " +
              $"{damageType} damage";
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        flatDamage = Mathf.Max(
            0,
            flatDamage
        );
    }
#endif
}

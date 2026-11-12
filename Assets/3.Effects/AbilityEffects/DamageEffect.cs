
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
        return CalculateBaseDamage(caster, ability, chargeProgress, null, out _);
    }

    private int CalculateBaseDamage(
        CharacterStats caster,
        AbilityData ability,
        float chargeProgress,
        AbilityEffectExecutionContext context,
        out bool usedWeaponRange)
    {
        usedWeaponRange = false;
        float damage = Mathf.Max(0, flatDamage);
        float? weaponDamage = null;

        if (scaling != null)
        {
            foreach (DamageScalingEntry entry in scaling)
            {
                if (entry == null)
                    continue;

                if (entry.Stat == StatType.WeaponDamage && entry.Multiplier != 0f)
                {
                    if (!weaponDamage.HasValue)
                    {
                        weaponDamage = context != null
                            ? context.GetWeaponDamage(out usedWeaponRange)
                            : WeaponDamageSnapshot.GetPreviewValue(caster);
                    }

                    damage += weaponDamage.Value * entry.Multiplier;
                }
                else
                {
                    damage += entry.Evaluate(caster);
                }
            }
        }

        if (ability != null && ability.ChargeSettings != null)
            damage *= ability.ChargeSettings.GetDamageMultiplier(chargeProgress);

        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    public override void Execute(AbilityEffectExecutionContext context)
    {
        if (context == null || context.Caster == null ||
            context.Target == null || !context.TargetWasSuccessful)
        {
            return;
        }

        int damage = CalculateBaseDamage(
            context.Caster,
            context.Ability,
            context.ChargeProgress,
            context,
            out bool usedWeaponRange);

        if (damage <= 0)
            return;

        DamageSourceContext source = context.DamageSource;

        if (dealsRawDamage)
        {
            CombatResolver.DealRawDamage(source, context.Target, damage);
            return;
        }

        DamageResult result = CombatResolver.ResolveDamageAfterSuccessfulHit(
            context.Caster,
            context.Target,
            damage,
            context.Ability,
            damageType,
            source,
            applyDefaultDamageVariance: !usedWeaponRange);

        context.Target.TakeDamage(result, source);
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

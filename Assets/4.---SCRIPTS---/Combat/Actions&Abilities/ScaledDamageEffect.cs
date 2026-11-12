using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Scaled Damage"
)]
public sealed class ScaledDamageEffect :
    AbilityEffect
{
    [SerializeField]
    private float strengthMultiplier = 1.5f;

    [SerializeField]
    private float weaponDamageMultiplier = 1f;

    [SerializeField]
    private int flatDamage;

    public override void Execute(AbilityEffectExecutionContext context)
    {
        if (context == null || context.Caster == null ||
            context.Target == null || !context.TargetWasSuccessful)
        {
            return;
        }

        bool usedWeaponRange = false;

        float weaponDamage = weaponDamageMultiplier != 0f
            ? context.GetWeaponDamage(out usedWeaponRange)
            : 0f;

        int baseDamage = CalculateBaseDamage(context.Caster, weaponDamage);

        DamageResult result = CombatResolver.ResolveDamageAfterSuccessfulHit(
            context.Caster,
            context.Target,
            baseDamage,
            context.Ability,
            applyDefaultDamageVariance: !usedWeaponRange);

        context.Target.TakeDamage(result, context.DamageSource);
    }

    public int CalculateBaseDamage(CharacterStats caster)
    {
        if (caster == null)
            return 0;

        float weaponDamage = weaponDamageMultiplier != 0f
            ? WeaponDamageSnapshot.GetPreviewValue(caster)
            : 0f;

        return CalculateBaseDamage(caster, weaponDamage);
    }

    private int CalculateBaseDamage(CharacterStats caster, float weaponDamage)
    {
        float damage =
            flatDamage +
            caster.GetStat(StatType.Strength) * strengthMultiplier +
            weaponDamage * weaponDamageMultiplier;

        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        if (caster == null)
            return string.Empty;

        int damage =
            CalculateBaseDamage(
                caster
            );

        return
            $"Deals approximately {damage} damage";
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        strengthMultiplier =
            Mathf.Max(
                0f,
                strengthMultiplier
            );

        weaponDamageMultiplier =
            Mathf.Max(
                0f,
                weaponDamageMultiplier
            );
    }
#endif
}
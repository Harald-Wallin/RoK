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

        int baseDamage =
            CalculateBaseDamage(
                context.Caster
            );

        DamageResult result =
            CombatResolver
                .ResolveDamageAfterSuccessfulHit(
                    context.Caster,
                    context.Target,
                    baseDamage,
                    context.Ability
                );

        context.Target.TakeDamage(
            result,
            context.DamageSource
        );
    }

    public int CalculateBaseDamage(
        CharacterStats caster)
    {
        if (caster == null)
            return 0;

        float damage =
            flatDamage;

        damage +=
            caster.GetStat(
                StatType.Strength
            ) *
            strengthMultiplier;

        damage +=
            caster.GetStat(
                StatType.WeaponDamage
            ) *
            weaponDamageMultiplier;

        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                damage
            )
        );
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
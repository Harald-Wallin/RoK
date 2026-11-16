using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Heal"
)]
public sealed class HealEffect :
    AbilityEffect
{
    [Header("Healing")]

    [SerializeField]
    [Min(0)]
    private int flatHealing;

    [SerializeField]
    private HealScalingEntry[] scaling;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Lägger till en procentandel av targetets MaxHP. " +
        "0.1 innebär 10 procent."
    )]
    private float targetMaximumHealthPercentage;

    [Header("Charge")]

    [SerializeField]
    private bool scalesWithCharge;

    [SerializeField]
    [Min(0f)]
    private float minimumChargeMultiplier = 0.25f;

    public int CalculateHealing(
        CharacterStats caster,
        CharacterStats target,
        float chargeProgress = 1f)
    {
        float healing =
            Mathf.Max(
                0,
                flatHealing
            );

        if (scaling != null)
        {
            for (int i = 0;
                 i < scaling.Length;
                 i++)
            {
                HealScalingEntry entry =
                    scaling[i];

                if (entry == null)
                    continue;

                healing +=
                    entry.Evaluate(
                        caster
                    );
            }
        }

        if (target != null &&
            targetMaximumHealthPercentage > 0f)
        {
            healing +=
                target.GetMaxHP() *
                targetMaximumHealthPercentage;
        }

        if (scalesWithCharge)
        {
            float chargeMultiplier =
                Mathf.Lerp(
                    Mathf.Max(
                        0f,
                        minimumChargeMultiplier
                    ),
                    1f,
                    Mathf.Clamp01(
                        chargeProgress
                    )
                );

            healing *= chargeMultiplier;
        }

        return Mathf.Max(
            0,
            Mathf.RoundToInt(
                healing
            )
        );
    }

    public override void Execute(AbilityEffectExecutionContext context)
    {
        if (context == null || context.Target == null)
            return;

        int healing = CalculateHealing(
            context.Caster, context.Target, context.ChargeProgress);

        if (healing <= 0)
            return;

        context.Target.ApplyHealing(context.Caster, healing, canCrit: false);
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        int healing =
            CalculateHealing(
                caster,
                caster
            );

        if (targetMaximumHealthPercentage > 0f)
        {
            float percentage =
                targetMaximumHealthPercentage *
                100f;

            return
                $"Heals for {healing} plus " +
                $"{percentage:0.#}% of maximum health";
        }

        return
            $"Heals for approximately {healing}";
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        flatHealing =
            Mathf.Max(
                0,
                flatHealing
            );

        minimumChargeMultiplier =
            Mathf.Max(
                0f,
                minimumChargeMultiplier
            );
    }
#endif
}

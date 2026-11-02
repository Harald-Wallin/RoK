
using UnityEngine;

[CreateAssetMenu(
    menuName = "RöK/Effects/Damage/Damage Type Bonus"
)]
public sealed class DamageTypeBonusEffectDefinition :
    EffectDefinition
{
    [Header("Damage Type")]
    [SerializeField]
    private DamageType damageType =
        DamageType.Fire;

    [Header("Damage Bonus")]
    [SerializeField]
    private float bonus = 0.10f;

    public override IEffectRuntime CreateRuntime(
        EffectContext context)
    {
        return new Runtime(
            context,
            damageType,
            bonus
        );
    }

    public override string GetDescription()
    {
        return
            $"+{bonus * 100f:0.#}% " +
            $"{damageType} damage";
    }

    private sealed class Runtime :
        DamageQueryEffectRuntime
    {
        private readonly DamageType damageType;
        private readonly float bonus;

        public Runtime(
            EffectContext context,
            DamageType damageType,
            float bonus)
            : base(context)
        {
            this.damageType = damageType;
            this.bonus = bonus;
        }

        public override void Modify(
            DamageContext context)
        {
            if (Stacks <= 0 ||
                Owner == null ||
                context == null ||
                context.Attacker != Owner ||
                context.DamageType != damageType)
            {
                return;
            }

            context.AddOutgoingBonus(
                bonus * Stacks
            );
        }
    }
}


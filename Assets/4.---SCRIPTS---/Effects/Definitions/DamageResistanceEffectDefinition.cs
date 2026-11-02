
using UnityEngine;

[CreateAssetMenu(
    menuName = "RöK/Effects/Damage/Damage Resistance"
)]
public sealed class DamageResistanceEffectDefinition :
    EffectDefinition
{
    [Header("Resistance")]
    [SerializeField]
    private DamageType damageType =
        DamageType.Fire;

    [SerializeField]
    private float resistance = 0.10f;

    public override IEffectRuntime CreateRuntime(
        EffectContext context)
    {
        return new Runtime(
            context,
            damageType,
            resistance
        );
    }

    public override string GetDescription()
    {
        return
            $"+{resistance * 100f:0.#}% " +
            $"{damageType} resistance";
    }

    private sealed class Runtime :
        DamageQueryEffectRuntime
    {
        private readonly DamageType damageType;
        private readonly float resistance;

        public Runtime(
            EffectContext context,
            DamageType damageType,
            float resistance)
            : base(context)
        {
            this.damageType = damageType;
            this.resistance = resistance;
        }

        public override void Modify(
            DamageContext context)
        {
            if (Stacks <= 0 ||
                Owner == null ||
                context == null ||
                context.Defender != Owner ||
                context.DamageType != damageType)
            {
                return;
            }

            context.AddResistanceBonus(
                resistance * Stacks
            );
        }
    }
}


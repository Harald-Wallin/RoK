
using UnityEngine;

[CreateAssetMenu(
    menuName = "RöK/Effects/Damage/Creature Damage Bonus"
)]
public sealed class CreatureDamageBonusEffectDefinition :
    EffectDefinition
{
    [Header("Target Creatures")]
    [SerializeField]
    private CreatureType targetTypes =
        CreatureType.Beast;

    [Header("Damage Bonus")]
    [SerializeField]
    private float bonus = 0.10f;

    public override IEffectRuntime CreateRuntime(
        EffectContext context)
    {
        return new Runtime(
            context,
            targetTypes,
            bonus
        );
    }

    public override string GetDescription()
    {
        return
            $"+{bonus * 100f:0.#}% damage " +
            $"against {targetTypes}";
    }

    private sealed class Runtime :
        DamageQueryEffectRuntime
    {
        private readonly CreatureType targetTypes;
        private readonly float bonus;

        public Runtime(
            EffectContext context,
            CreatureType targetTypes,
            float bonus)
            : base(context)
        {
            this.targetTypes = targetTypes;
            this.bonus = bonus;
        }

        public override void Modify(
            DamageContext context)
        {
            if (Stacks <= 0 ||
                Owner == null ||
                context == null ||
                context.Attacker != Owner ||
                targetTypes == CreatureType.None)
            {
                return;
            }

            bool matches =
                (context.TargetCreatureTypes &
                 targetTypes) != CreatureType.None;

            if (!matches)
                return;

            context.AddOutgoingBonus(
                bonus * Stacks
            );
        }
    }
}

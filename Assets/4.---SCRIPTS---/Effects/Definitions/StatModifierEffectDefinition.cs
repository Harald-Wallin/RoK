
using UnityEngine;

[CreateAssetMenu(
    menuName = "RöK/Effects/Stat Modifier"
)]
public sealed class StatModifierEffectDefinition :
    EffectDefinition
{
    [Header("Stat Modifier")]

    [SerializeField]
    private StatType stat;

    [SerializeField]
    private ModifierType modifierType =
        ModifierType.Percent;

    [SerializeField]
    private float value = 0.10f;

    // =====================================================
    // RUNTIME FACTORY
    // =====================================================

    public override IEffectRuntime CreateRuntime(
        EffectContext context)
    {
        return new Runtime(
            context,
            stat,
            modifierType,
            value
        );
    }

    // =====================================================
    // DESCRIPTIONS
    // =====================================================

    public override string GetDescription()
    {
        return GetDescriptionForStacks(1);
    }

    public override string GetDescriptionForStacks(
        int stacks)
    {
        return StatFormatting.FormatModifier(
            stat,
            modifierType,
            value * Mathf.Max(0, stacks)
        );
    }

    // =====================================================
    // EFFECT RUNTIME
    // =====================================================

    private sealed class Runtime :
        IEffectRuntime
    {
        private readonly EffectContext context;

        private readonly StatType stat;
        private readonly ModifierType type;
        private readonly float value;

        private bool applied;

        public Runtime(
            EffectContext context,
            StatType stat,
            ModifierType type,
            float value)
        {
            this.context = context;
            this.stat = stat;
            this.type = type;
            this.value = value;
        }

        // =================================================
        // ACTIVATION
        // =================================================

        public void Activate(int stacks)
        {
            Rebuild(stacks);
        }

        // =================================================
        // STACK UPDATES
        // =================================================

        public void SetStacks(int stacks)
        {
            Rebuild(stacks);
        }

        // =================================================
        // REBUILD MODIFIER
        // =================================================

        private void Rebuild(int stacks)
        {
            CharacterStats target =
                context.Target;

            if (target == null)
                return;

            // Remove only this runtime's modifiers.
            if (applied)
            {
                target.RemoveModifiersFromSource(
                    this
                );

                applied = false;
            }

            float effectiveValue =
                value * Mathf.Max(1, stacks);

            if (Mathf.Approximately(
                    effectiveValue,
                    0f))
            {
                return;
            }

            StatModifier modifier =
                new StatModifier(
                    stat,
                    effectiveValue,
                    type,
                    this,
                    context.ModifierSourceType
                );

            target.AddModifier(modifier);

            applied = true;
        }

        // =================================================
        // CLEANUP
        // =================================================

        public void Deactivate()
        {
            if (!applied)
                return;

            if (context.Target != null)
            {
                context.Target.RemoveModifiersFromSource(
                    this
                );
            }

            applied = false;
        }
    }
}

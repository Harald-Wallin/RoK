
using UnityEngine;

/// <summary>
/// Reusable Ward capacity effect.
///
/// Does not generate Wards or unlock Ward generation.
/// The owning system controls lifetime and stacks.
/// </summary>
[CreateAssetMenu(
    menuName = "RöK/Effects/Ward Capacity"
)]
public sealed class WardCapacityEffectDefinition :
    EffectDefinition
{
    [Header("Ward Capacity")]

    [SerializeField, Min(0)]
    [Tooltip(
        "Additional maximum Wards per stack or talent point.\n\n" +
        "Example:\n" +
        "2 Wards per stack and 3 talent points = " +
        "+6 Maximum Wards.\n\n" +
        "This effect does not unlock Ward generation."
    )]
    private int wardsPerStack = 2;

    // =====================================================
    // RUNTIME FACTORY
    // =====================================================

    public override IEffectRuntime CreateRuntime(
        EffectContext context)
    {
        return new Runtime(
            context.Target,
            wardsPerStack
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
        int amount =
            wardsPerStack * Mathf.Max(0, stacks);

        return $"+{amount} Maximum Wards";
    }

    // =====================================================
    // EFFECT RUNTIME
    // =====================================================

    private sealed class Runtime :
        IEffectRuntime,
        IEffectQuery<WardCapacityQuery>
    {
        private readonly CharacterStats owner;

        private readonly int wardsPerStack;

        private int stacks;

        public Runtime(
            CharacterStats owner,
            int wardsPerStack)
        {
            this.owner = owner;

            this.wardsPerStack =
                Mathf.Max(0, wardsPerStack);
        }

        // =================================================
        // ACTIVATION
        // =================================================

        public void Activate(int value)
        {
            SetStacks(value);
        }

        // =================================================
        // STACK UPDATES
        // =================================================

        public void SetStacks(int value)
        {
            stacks = Mathf.Max(0, value);
        }

        // =================================================
        // DEACTIVATION
        // =================================================

        public void Deactivate()
        {
            stacks = 0;
        }

        // =================================================
        // WARD CAPACITY QUERY
        // =================================================

        public void Modify(
            WardCapacityQuery query)
        {
            if (query == null ||
                owner == null ||
                query.Owner != owner ||
                stacks <= 0)
            {
                return;
            }

            query.Add(
                wardsPerStack * stacks
            );
        }
    }
}


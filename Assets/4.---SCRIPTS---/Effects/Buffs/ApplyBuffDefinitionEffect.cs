
using UnityEngine;

/// <summary>
/// AbilityEffect adapter that applies a
/// shared BuffDefinition.
///
/// The existing ability pipeline remains
/// responsible for targeting and hit resolution.
/// </summary>
[CreateAssetMenu(
    menuName = "RöK/Effects/Apply Buff Definition"
)]
public sealed class ApplyBuffDefinitionEffect :
    AbilityEffect
{
    [SerializeField]
    private BuffDefinition buffDefinition;

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Target == null ||
            buffDefinition == null)
        {
            return;
        }

        BuffSystem buffs =
            context.Target.GetComponent<BuffSystem>();

        if (buffs == null)
            return;

        buffs.ApplyDefinition(
            buffDefinition,
            context.DamageSource
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        if (buffDefinition == null)
            return string.Empty;

        return buffDefinition.GetDescription() +
            $"\nDuration: " +
            $"{buffDefinition.Duration:0.#}s";
    }
}


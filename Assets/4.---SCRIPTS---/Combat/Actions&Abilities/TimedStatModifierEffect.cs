using System.Collections;
using UnityEngine;

[CreateAssetMenu(
    menuName =
        "RPG/Effects/Timed Stat Modifier"
)]
public class TimedStatModifierEffect :
    AbilityEffect
{
    public StatType stat;
    public float value;
    public ModifierType type;
    public float duration = 2f;

    public override void Apply(
        CharacterStats caster,
        CharacterStats target)
    {
        if (target == null)
            return;

        object runtimeSource =
            new object();

        StatModifier modifier =
            new StatModifier(
                stat,
                value,
                type,
                runtimeSource,
                ModifierSourceType.Buff
            );

        target.AddModifier(modifier);

        target.StartCoroutine(
            RemoveAfterDuration(
                target,
                runtimeSource
            )
        );
    }

    private IEnumerator RemoveAfterDuration(
        CharacterStats target,
        object runtimeSource)
    {
        yield return new WaitForSeconds(
            duration
        );

        if (target == null)
            yield break;

        target.RemoveModifiersFromSource(
            runtimeSource
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        string modifierText =
            StatFormatting.FormatModifier(
                stat,
                type,
                value
            );

        return
            $"{modifierText} for {duration:0.#}s";
    }
}

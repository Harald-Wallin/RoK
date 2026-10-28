using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Stat Modifier"
)]
public class StatModifierEffect :
    AbilityEffect
{
    public StatType stat;
    public float value;
    public ModifierType type;

    public override void Apply(
        CharacterStats caster,
        CharacterStats target)
    {
        if (target == null)
            return;

        target.AddModifier(
            new StatModifier(
                stat,
                value,
                type,
                this,
                ModifierSourceType.Buff
            )
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        return StatFormatting.FormatModifier(
            stat,
            type,
            value
        );
    }
}

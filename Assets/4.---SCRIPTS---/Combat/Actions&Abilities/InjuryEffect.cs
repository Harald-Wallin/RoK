using UnityEngine;

[CreateAssetMenu(
    menuName = "RPG/Effects/Legacy/Injury"
)]
public sealed class InjuryEffect :
    AbilityEffect
{
    public StatType stat;

    public float value;

    public ModifierType type;

    [Min(0f)]
    public float duration = 240f;

    public override void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null ||
            context.Target == null)
        {
            return;
        }

        BuffSystem buffs =
            context.Target.GetComponent<
                BuffSystem
            >();

        buffs?.ApplyEffect(
            this,
            context.Caster
        );
    }

    public override ActiveBuff CreateActiveBuff(
        CharacterStats source,
        CharacterStats target)
    {
        return new ActiveInjury(
            this
        );
    }

    public override string GetTooltipText(
        CharacterStats caster)
    {
        if (type ==
            ModifierType.Percent)
        {
            return
                $"{stat} {value * 100f:0.#}% " +
                $"for {duration:0.#}s";
        }

        return
            $"{stat} {value:0.#} " +
            $"for {duration:0.#}s";
    }
}
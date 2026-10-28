
using UnityEngine;

public class ActiveInjury : ActiveBuff
{
    private readonly InjuryEffect injury;

    public ActiveInjury(InjuryEffect effect)
    {
        injury = effect;
        sourceEffect = effect;
        duration = effect.duration;
    }

    public override bool IsStackable =>
        injury.stackable;

    public override int MaxStacks =>
        Mathf.Max(1, injury.maxStacks);

    public override void OnApplied(
        CharacterStats target)
    {
        ApplyStacks(target);
    }

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        // Time progression only.
        // Modifiers are owned by the buff lifecycle.
        elapsed += deltaTime;
    }

    public override void OnStackChanged(
        CharacterStats target)
    {
        if (target == null)
            return;

        target.RemoveModifiersFromSource(
            sourceEffect
        );

        ApplyStacks(target);
    }

    public override void OnRemoved(
        CharacterStats target)
    {
        if (target == null)
            return;

        target.RemoveModifiersFromSource(
            sourceEffect
        );
    }

    private void ApplyStacks(
        CharacterStats target)
    {
        if (target == null)
            return;

        for (int i = 0; i < stacks; i++)
        {
            StatModifier modifier =
                new StatModifier(
                    injury.stat,
                    injury.value,
                    injury.type,
                    sourceEffect,
                    ModifierSourceType.Buff
                );

            target.AddModifier(modifier);
        }
    }
}

using System.Collections.Generic;

/// <summary>
/// Gemensam hjälpare för att registrera combat activity.
/// </summary>
public static class CombatActivityUtility
{
    public static void Notify(
        ActionExecutionContext action,
        IReadOnlyList<AbilityTargetHitResult> targets)
    {
        if (action == null ||
            action.Ability == null ||
            action.Caster == null ||
            !action.Ability.entersCombatState)
        {
            return;
        }

        NotifyCharacter(
            action.Caster
        );

        if (targets == null)
            return;

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            NotifyCharacter(
                targets[i]?.Target
            );
        }
    }

    public static void Notify(
        ActionExecutionContext action,
        AbilityTargetHitResult target)
    {
        if (action == null ||
            action.Ability == null ||
            action.Caster == null ||
            !action.Ability.entersCombatState)
        {
            return;
        }

        NotifyCharacter(
            action.Caster
        );

        NotifyCharacter(
            target?.Target
        );
    }

    private static void NotifyCharacter(
        CharacterStats character)
    {
        if (character == null)
            return;

        CharacterStateController state =
            character.GetComponent<
                CharacterStateController
            >();

        state?.NotifyCombatActivity();
    }
}

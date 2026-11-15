using System;

public static class CharacterCombatEvents
{
    public static event Action<CharacterDefeatedResult>
        CharacterDefeated;

    public static void RaiseCharacterDefeated(
        CharacterDefeatedResult result)
    {
        if (result == null)
            return;

        CharacterDefeated?.Invoke(
            result
        );
    }

    public static event Action<DamageAppliedEvent> DamageApplied;

    public static void RaiseDamageApplied(DamageAppliedEvent damage)
    {
        DamageApplied?.Invoke(damage);
    }
}

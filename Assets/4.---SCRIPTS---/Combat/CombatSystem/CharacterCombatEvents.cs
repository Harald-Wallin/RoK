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
}

using System;

public static class CharacterCombatEvents
{
    public static event Action<CharacterDefeatedResult> CharacterDefeated;
    public static event Action<DamageAppliedEvent> DamageApplied;
    public static event Action<CharacterStats, CharacterStats, HealingResult>
        HealingApplied;

    public static void RaiseCharacterDefeated(CharacterDefeatedResult result)
    {
        if (result != null)
            CharacterDefeated?.Invoke(result);
    }

    public static void RaiseDamageApplied(DamageAppliedEvent damage)
    {
        DamageApplied?.Invoke(damage);
    }

    public static void RaiseHealingApplied(
        CharacterStats healer,
        CharacterStats target,
        HealingResult result)
    {
        if (target != null && result.AppliedAmount > 0)
            HealingApplied?.Invoke(healer, target, result);
    }
}
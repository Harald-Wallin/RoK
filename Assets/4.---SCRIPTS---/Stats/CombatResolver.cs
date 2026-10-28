using UnityEngine;

public static class CombatResolver
{
    public static int DealDamage(
        CharacterStats attacker,
        CharacterStats target,
        int baseDamage)
    {
        return DealDamage(
            DamageSourceContext.FromDirectSource(
                attacker
            ),
            target,
            baseDamage
        );
    }

    public static int DealDamage(
        DamageSourceContext source,
        CharacterStats target,
        int baseDamage)
    {
        CharacterStats attacker =
            source.DirectSource;

        if (attacker == null ||
            target == null)
        {
            return 0;
        }

        DamageResult result =
            DamageCalculator.CalculateDamage(
                baseDamage,
                attacker.GetStat(
                    StatType.CritChance
                ),
                attacker.GetStat(
                    StatType.CritMultiplier
                ),
                attacker,
                target
            );

        return target.TakeDamage(
            result,
            source
        );
    }

    public static int DealRawDamage(
        CharacterStats attacker,
        CharacterStats target,
        int damage)
    {
        return DealRawDamage(
            DamageSourceContext.FromDirectSource(
                attacker
            ),
            target,
            damage
        );
    }

    public static int DealRawDamage(
        DamageSourceContext source,
        CharacterStats target,
        int damage)
    {
        if (target == null)
            return 0;

        return target.TakeRawDamage(
            damage,
            source
        );
    }

    public static bool RollHit(
        CharacterStats attacker,
        CharacterStats target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        float hitChance =
            attacker.GetStat(
                StatType.HitChance
            );

        int levelDifference =
            GetLevel(target) -
            GetLevel(attacker);

        hitChance -=
            levelDifference * 0.07f;

        hitChance =
            Mathf.Clamp01(
                hitChance
            );

        return
            Random.value <=
            hitChance;
    }

    public static bool RollDodge(
        CharacterStats attacker,
        CharacterStats target)
    {
        if (attacker == null ||
            target == null)
        {
            return false;
        }

        float evasion =
            target.GetStat(
                StatType.Evasion
            );

        int levelDifference =
            GetLevel(target) -
            GetLevel(attacker);

        evasion +=
            levelDifference * 0.02f;

        evasion =
            Mathf.Clamp01(
                evasion
            );

        return
            Random.value <=
            evasion;
    }

    public static DamageResult
        ResolveDamageAfterSuccessfulHit(
            CharacterStats attacker,
            CharacterStats target,
            int baseDamage,
            AbilityData ability)
    {
        if (attacker == null ||
            target == null)
        {
            return default;
        }

        bool canCrit =
            ability == null ||
            ability.canCrit;

        return DamageCalculator
            .CalculateDamageAfterSuccessfulHit(
                baseDamage,
                attacker.GetStat(
                    StatType.CritChance
                ),
                attacker.GetStat(
                    StatType.CritMultiplier
                ),
                attacker,
                target,
                canCrit
            );
    }

    public static DamageResult ResolveEffectDamage(
        CharacterStats attacker,
        CharacterStats target,
        int baseDamage,
        AbilityData ability)
    {
        if (attacker == null ||
            target == null)
        {
            return default;
        }

        bool shouldRollHit =
            ability != null &&
            ability.canMiss &&
            !ability.alwaysHits;

        if (shouldRollHit)
        {
            if (!RollHit(
                    attacker,
                    target))
            {
                return new DamageResult
                {
                    isMiss = true
                };
            }

            if (RollDodge(
                    attacker,
                    target))
            {
                return new DamageResult
                {
                    isEvaded = true
                };
            }
        }

        return ResolveDamageAfterSuccessfulHit(
            attacker,
            target,
            baseDamage,
            ability
        );
    }

    public static DamageResult ResolveAbilityHit(
        CharacterStats attacker,
        CharacterStats target,
        int baseDamage)
    {
        if (attacker == null ||
            target == null)
        {
            return default;
        }

        return DamageCalculator.CalculateDamage(
            baseDamage,
            attacker.GetStat(
                StatType.CritChance
            ),
            attacker.GetStat(
                StatType.CritMultiplier
            ),
            attacker,
            target
        );
    }

    private static int GetLevel(
        CharacterStats stats)
    {
        return stats != null
            ? stats.level
            : 1;
    }
}
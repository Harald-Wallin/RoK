using UnityEngine;

public struct DamageResult
{
    public int damage;

    public bool isCrit;
    public bool isMiss;
    public bool isEvaded;

    public bool isBlocked;
    public int blockedAmount;
}

public static class DamageCalculator
{
    /// <summary>
    /// Legacy/full damage resolution.
    ///
    /// Används av kod som ännu inte har fått ett hitresultat från
    /// AbilityEffectPipeline.
    /// </summary>
    public static DamageResult CalculateDamage(
        int baseDamage,
        float critChance,
        float critMultiplier,
        CharacterStats attacker,
        CharacterStats defender)
    {
        if (attacker == null ||
            defender == null)
        {
            return default;
        }

        if (!CombatResolver.RollHit(
                attacker,
                defender))
        {
            return new DamageResult
            {
                damage = 0,
                isMiss = true
            };
        }

        if (CombatResolver.RollDodge(
                attacker,
                defender))
        {
            return new DamageResult
            {
                damage = 0,
                isEvaded = true
            };
        }

        return CalculateDamageAfterSuccessfulHit(
            baseDamage,
            critChance,
            critMultiplier,
            attacker,
            defender,
            true
        );
    }

    /// <summary>
    /// Beräknar damage efter att hit och dodge redan har avgjorts.
    ///
    /// Den här metoden får aldrig göra ett nytt hit/dodge-slag.
    /// </summary>
    public static DamageResult
        CalculateDamageAfterSuccessfulHit(
            int baseDamage,
            float critChance,
            float critMultiplier,
            CharacterStats attacker,
            CharacterStats defender,
            bool canCrit)
    {
        if (attacker == null ||
            defender == null)
        {
            return default;
        }

        float damage =
            Mathf.Max(
                0,
                baseDamage
            );

        damage *=
            Random.Range(
                0.8f,
                1.2f
            );

        bool isCrit = false;

        if (canCrit)
        {
            float safeCritChance =
                Mathf.Clamp01(
                    critChance
                );

            float safeCritMultiplier =
                Mathf.Max(
                    1f,
                    critMultiplier
                );

            isCrit =
                Random.value <
                safeCritChance;

            if (isCrit)
            {
                damage *=
                    safeCritMultiplier;
            }
        }

        float armor =
            Mathf.Max(
                0f,
                defender.GetStat(
                    StatType.Armor
                )
            );

        damage *=
            100f /
            (100f + armor);

        bool blocked = false;
        int blockedAmount = 0;

        float blockChance =
            Mathf.Clamp01(
                defender.GetStat(
                    StatType.BlockChance
                )
            );

        if (Random.value < blockChance)
        {
            blocked = true;

            int blockValue =
    Mathf.Max(
        0,
        Mathf.RoundToInt(
            defender.GetStat(
                StatType.BlockValue
            )
        )
    );

            blockedAmount =
                Mathf.Min(
                    blockValue,
                    Mathf.Max(
                        0,
                        Mathf.CeilToInt(
                            damage
                        )
                    )
                );

            damage -= blockedAmount;

            damage -= blockedAmount;
        }

        int finalDamage =
            Mathf.Max(
                0,
                Mathf.FloorToInt(
                    damage
                )
            );

        return new DamageResult
        {
            damage = finalDamage,
            isCrit = isCrit,
            isMiss = false,
            isEvaded = false,
            isBlocked = blocked,
            blockedAmount = blockedAmount
        };
    }
}
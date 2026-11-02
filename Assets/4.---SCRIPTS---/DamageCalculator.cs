
using UnityEngine;

public struct DamageResult
{
    public int damage;

    public DamageType damageType;

    public bool isCrit;
    public bool isMiss;
    public bool isEvaded;

    public bool isBlocked;
    public int blockedAmount;
}

public static class DamageCalculator
{
    // =====================================================
    // FULL DAMAGE RESOLUTION (LEGACY ENTRY)
    // =====================================================

    public static DamageResult CalculateDamage(
        int baseDamage,
        float critChance,
        float critMultiplier,
        CharacterStats attacker,
        CharacterStats defender,
        DamageType damageType = DamageType.Physical,
        DamageSourceContext? sourceContext = null)
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
                damageType = damageType,
                isMiss = true
            };
        }

        if (CombatResolver.RollDodge(
                attacker,
                defender))
        {
            return new DamageResult
            {
                damageType = damageType,
                isEvaded = true
            };
        }

        return CalculateDamageAfterSuccessfulHit(
            baseDamage,
            critChance,
            critMultiplier,
            attacker,
            defender,
            true,
            damageType,
            sourceContext
        );
    }

    // =====================================================
    // DAMAGE AFTER SUCCESSFUL HIT
    // =====================================================

    /// <summary>
    /// Does not roll hit or dodge.
    ///
    /// The existing AbilityHitResolver is authoritative
    /// for action-based hit resolution.
    /// </summary>
    public static DamageResult
        CalculateDamageAfterSuccessfulHit(
            int baseDamage,
            float critChance,
            float critMultiplier,
            CharacterStats attacker,
            CharacterStats defender,
            bool canCrit,
            DamageType damageType = DamageType.Physical,
            DamageSourceContext? sourceContext = null)
    {
        if (attacker == null ||
            defender == null)
        {
            return default;
        }

        DamageSourceContext source =
            sourceContext ??
            DamageSourceContext.FromDirectSource(
                attacker
            );

        // =================================================
        // CONTEXTUAL EFFECTS
        // =================================================

        DamageContext context =
            new DamageContext(
                source,
                attacker,
                defender,
                damageType
            );

        CharacterEffectController attackerEffects =
            attacker.GetComponent<
                CharacterEffectController>();

        attackerEffects?.ApplyTo(context);

        // Avoid processing a shared controller twice
        // when attacker and defender are the same.
        if (defender != attacker)
        {
            CharacterEffectController defenderEffects =
                defender.GetComponent<
                    CharacterEffectController>();

            defenderEffects?.ApplyTo(context);
        }

        // =================================================
        // BASE DAMAGE + OUTGOING MODIFIERS
        // =================================================

        float damage =
            Mathf.Max(0f, baseDamage);

        float outgoingMultiplier =
            Mathf.Max(
                0f,
                1f + context.OutgoingBonus
            );

        damage *= outgoingMultiplier;

        // =================================================
        // RANDOM VARIATION
        // =================================================

        damage *= Random.Range(
            0.8f,
            1.2f
        );

        // =================================================
        // CRITICAL HIT
        // =================================================

        bool isCrit = false;

        if (canCrit)
        {
            float safeCritChance =
                Mathf.Clamp01(critChance);

            float safeCritMultiplier =
                Mathf.Max(
                    1f,
                    critMultiplier
                );

            isCrit =
                Random.value < safeCritChance;

            if (isCrit)
            {
                damage *= safeCritMultiplier;
            }
        }

        // =================================================
        // ARMOR – PHYSICAL DAMAGE
        // =================================================

        if (damageType == DamageType.Physical)
        {
            float armor =
                Mathf.Max(
                    0f,
                    defender.GetStat(
                        StatType.Armor
                    )
                );

            damage *=
                100f / (100f + armor);
        }

        // =================================================
        // RESISTANCES
        // =================================================

        float baseResistance =
            GetBaseResistance(
                defender,
                damageType
            );

        float totalResistance =
            Mathf.Clamp(
                baseResistance +
                context.ResistanceBonus,
                -1f,
                1f
            );

        damage *=
            1f - totalResistance;

        // =================================================
        // BLOCK – PHYSICAL DAMAGE
        // =================================================

        bool blocked = false;
        int blockedAmount = 0;

        if (damageType == DamageType.Physical)
        {
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
                            Mathf.CeilToInt(damage)
                        )
                    );

                // Exactly one block subtraction.
                damage -= blockedAmount;
            }
        }

        // =================================================
        // FINAL DAMAGE
        // =================================================

        int finalDamage =
            Mathf.Max(
                0,
                Mathf.FloorToInt(damage)
            );

        return new DamageResult
        {
            damage = finalDamage,
            damageType = damageType,

            isCrit = isCrit,

            isMiss = false,
            isEvaded = false,

            isBlocked = blocked,
            blockedAmount = blockedAmount
        };
    }

    // =====================================================
    // BASE CREATURE RESISTANCE
    // =====================================================

    private static float GetBaseResistance(
        CharacterStats defender,
        DamageType damageType)
    {
        if (defender == null)
            return 0f;

        CreatureIdentity identity =
            defender.GetComponent<
                CreatureIdentity>();

        CreatureDefinition definition =
            identity != null
                ? identity.Definition
                : null;

        return definition != null
            ? definition.GetResistance(damageType)
            : 0f;
    }
}

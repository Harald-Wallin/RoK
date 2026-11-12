
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
    public static DamageResult CalculateDamageAfterSuccessfulHit(
    int baseDamage,
    float critChance,
    float critMultiplier,
    CharacterStats attacker,
    CharacterStats defender,
    bool canCrit,
    DamageType damageType = DamageType.Physical,
    DamageSourceContext? sourceContext = null,
    bool applyDefaultDamageVariance = true)
    {
        if (attacker == null || defender == null)
            return default;

        DamageSourceContext source =
            sourceContext ?? DamageSourceContext.FromDirectSource(attacker);

        DamageContext context = new DamageContext(
            source, attacker, defender, damageType);

        attacker.GetComponent<CharacterEffectController>()?.ApplyTo(context);

        if (defender != attacker)
            defender.GetComponent<CharacterEffectController>()?.ApplyTo(context);

        float damage =
            Mathf.Max(0f, baseDamage) * Mathf.Max(0f, 1f + context.OutgoingBonus);

        if (applyDefaultDamageVariance)
            damage *= Random.Range(0.8f, 1.2f);

        bool isCrit = false;

        if (canCrit)
        {
            isCrit = Random.value < Mathf.Clamp01(critChance);

            if (isCrit)
                damage *= Mathf.Max(1f, critMultiplier);
        }

        if (damageType == DamageType.Physical)
        {
            float armor = Mathf.Max(0f, defender.GetStat(StatType.Armor));
            damage *= 100f / (100f + armor);
        }

        float totalResistance = Mathf.Clamp(
            GetBaseResistance(defender, damageType) + context.ResistanceBonus,
            -1f,
            1f);

        damage *= 1f - totalResistance;

        bool blocked = false;
        int blockedAmount = 0;

        if (damageType == DamageType.Physical)
        {
            float blockChance = Mathf.Clamp01(defender.GetStat(StatType.BlockChance));

            if (Random.value < blockChance)
            {
                blocked = true;

                int blockValue = Mathf.Max(
                    0, Mathf.RoundToInt(defender.GetStat(StatType.BlockValue)));

                blockedAmount = Mathf.Min(
                    blockValue, Mathf.Max(0, Mathf.CeilToInt(damage)));

                damage -= blockedAmount;
            }
        }

        return new DamageResult
        {
            damage = Mathf.Max(0, Mathf.FloorToInt(damage)),
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

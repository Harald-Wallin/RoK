
using UnityEngine;

/// <summary>
/// Mutable calculation context for one damage component.
///
/// Shared EffectRuntime instances can contribute
/// outgoing damage bonuses or resistance modifiers.
///
/// This does not perform hit/dodge rolls, apply HP
/// damage or distribute combat credit.
/// </summary>
public sealed class DamageContext
{
    public DamageSourceContext Source { get; }

    public CharacterStats Attacker { get; }

    public CharacterStats Defender { get; }

    public DamageType DamageType { get; }

    public CreatureType TargetCreatureTypes =>
        Defender != null
            ? Defender.CreatureTypes
            : CreatureType.None;

    public float OutgoingBonus { get; private set; }

    public float ResistanceBonus { get; private set; }

    public DamageContext(
        DamageSourceContext source,
        CharacterStats attacker,
        CharacterStats defender,
        DamageType damageType)
    {
        Source = source;
        Attacker = attacker;
        Defender = defender;
        DamageType = damageType;
    }

    public void AddOutgoingBonus(float bonus)
    {
        if (float.IsNaN(bonus) ||
            float.IsInfinity(bonus))
        {
            return;
        }

        OutgoingBonus += bonus;
    }

    public void AddResistanceBonus(float bonus)
    {
        if (float.IsNaN(bonus) ||
            float.IsInfinity(bonus))
        {
            return;
        }

        ResistanceBonus += bonus;
    }
}


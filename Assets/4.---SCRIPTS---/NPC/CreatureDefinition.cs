
using System;
using UnityEngine;

[Serializable]
public sealed class CreatureResistanceEntry
{
    public DamageType damageType;

    [Tooltip(
        "0.20 = 20% resistance. " +
        "-0.20 = 20% vulnerability."
    )]
    [Range(-1f, 1f)]
    public float resistance;
}

[CreateAssetMenu(
    menuName = "RPG/Characters/Creature Definition"
)]
public sealed class CreatureDefinition :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string displayName;

    [Header("Presentation")]
    [SerializeField]
    private Sprite icon;

    [Header("Base Damage Resistances")]
    [SerializeField]
    private CreatureResistanceEntry[] resistances =
        Array.Empty<CreatureResistanceEntry>();

    public string Id =>
        PersistentIdUtility.FromDisplayName(
            DisplayName
        );

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
            ? name
            : displayName;

    public Sprite Icon => icon;

    // =====================================================
    // BASE RESISTANCES
    // =====================================================

    public float GetResistance(
        DamageType type)
    {
        if (resistances == null)
            return 0f;

        float total = 0f;

        foreach (CreatureResistanceEntry entry
                 in resistances)
        {
            if (entry == null ||
                entry.damageType != type)
            {
                continue;
            }

            total += entry.resistance;
        }

        return total;
    }
}

using System;
using UnityEngine;

[Serializable]
public struct WeaponComboStep
{
    [SerializeField, Min(0f)]
    private float windowFromPreviousHit;

    [SerializeField, Min(0f)]
    private float weaponDamageBonusPercent;

    public float WindowFromPreviousHit => Mathf.Max(0f, windowFromPreviousHit);

    public float WeaponDamageBonus =>
        Mathf.Max(0f, weaponDamageBonusPercent) / 100f;

    public WeaponComboStep(float windowFromPreviousHit, float weaponDamageBonusPercent)
    {
        this.windowFromPreviousHit = windowFromPreviousHit;
        this.weaponDamageBonusPercent = weaponDamageBonusPercent;
    }
}

[CreateAssetMenu(fileName = "New Weapon Family", menuName = "RPG/Weapons/Weapon Family")]
public sealed class WeaponFamilyDefinition : ScriptableObject
{
    [SerializeField]
    [Tooltip("Base Attacks that award XP when a weapon from this family is equipped.")]
    private AbilityData[] masteryAttacks = Array.Empty<AbilityData>();

    [SerializeField, Min(0)]
    private int maxTargetLevelsBelowPlayer = 3;

    [Header("Presentation")]

    [SerializeField] private string displayName;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();

    [Header("Mastery Levels")]

    [SerializeField]
    [Tooltip("XP required for each next level. Element 0 is the cost from level 1 to level 2.")]
    private int[] experiencePerLevel = { 5, 10, 15, 20, 25, 30 };

    [SerializeField]
    private WeaponMasteryTreeDefinition masteryTree;
    public WeaponMasteryTreeDefinition MasteryTree => masteryTree;

    [Header("Weapon Combo")]

    [SerializeField]
    private bool enableCombo;

    [SerializeField]
    private WeaponComboStep[] comboSteps =
    {
        new WeaponComboStep(0f, 0f),
        new WeaponComboStep(3f, 10f),
        new WeaponComboStep(2.4f, 20f),
        new WeaponComboStep(1.8f, 30f),
        new WeaponComboStep(1.2f, 40f),
        new WeaponComboStep(0.8f, 50f)
    };

    public int GetExperienceRequiredForNextLevel(int level)
    {
        const int maxMasteryLevel = 100;

        if (level < 1 || level >= maxMasteryLevel)
            return 0;

        if (experiencePerLevel == null || experiencePerLevel.Length == 0)
            return 5;

        int costIndex = Mathf.Min(level - 1, experiencePerLevel.Length - 1);
        return Mathf.Max(1, experiencePerLevel[costIndex]);
    }

    public bool CanTrainWith(AbilityData ability)
    {
        return ability != null &&
               ability.IsBaseAttack &&
               masteryAttacks != null &&
               Array.IndexOf(masteryAttacks, ability) >= 0;
    }

    public int GetMinimumTargetLevel(int playerLevel)
    {
        return Mathf.Max(1, playerLevel - Mathf.Max(0, maxTargetLevelsBelowPlayer));
    }

    public bool TryGetComboStep(int index, out WeaponComboStep step)
    {
        step = default;

        if (!enableCombo || comboSteps == null ||
            index < 0 || index >= comboSteps.Length)
        {
            return false;
        }

        step = comboSteps[index];
        return true;
    }
}

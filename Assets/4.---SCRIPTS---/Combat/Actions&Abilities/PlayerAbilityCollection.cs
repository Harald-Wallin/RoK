using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAbilityCollection :
    MonoBehaviour
{
    // =========================================================
    // CONSTANTS
    // =========================================================

    public const int ActionSlotCount = 9;

    public const int BaseAttackSlotCount = 2;

    public const int PrimaryBaseAttackSlotIndex = 0;
    public const int SecondaryBaseAttackSlotIndex = 1;

    // =========================================================
    // LEARNED
    // =========================================================

    [Header("Learned Abilities")]

    [SerializeField]
    private List<AbilityData> learnedAbilities =
        new();

    // =========================================================
    // ACTION BAR
    // =========================================================

    [Header("Action Bar")]

    [SerializeField]
    private AbilityData[] equippedAbilities =
        new AbilityData[
            ActionSlotCount
        ];

    // =========================================================
    // BASE ATTACKS
    // =========================================================

    [Header("Base Attacks")]

    [SerializeField]
    private AbilityData[] baseAttacks =
        new AbilityData[
            BaseAttackSlotCount
        ];

    // =========================================================
    // EVENTS
    // =========================================================

    public event Action<int, AbilityData>
        OnActionSlotChanged;

    public event Action<int, AbilityData>
        OnBaseAttackSlotChanged;

    public event Action<AbilityData>
        OnActiveBaseAttackChanged;

    public event Action<AbilityData>
        OnAbilityLearned;

    // =========================================================
    // LEARNED API
    // =========================================================

    public IReadOnlyList<AbilityData>
        LearnedAbilities =>
            learnedAbilities;

    public bool HasLearned(
        AbilityData ability)
    {
        return
            ability != null &&
            learnedAbilities.Contains(
                ability
            );
    }

    /// <summary>
    /// Alla AbilityData-assets lärs genom samma system.
    ///
    /// AbilityUsageType bestämmer senare vilka typer av slots
    /// abilityn får utrustas i.
    /// </summary>
    public bool LearnAbility(
        AbilityData ability)
    {
        if (ability == null)
            return false;

        if (learnedAbilities.Contains(
                ability))
        {
            return false;
        }

        learnedAbilities.Add(
            ability
        );

        AnnouncementSpawner.Instance
            ?.ShowAbilityLearned(
            ability.abilityName
            );

        SpellbookNotificationManager.Instance
            ?.NotifyNewEntry();

        OnAbilityLearned?.Invoke(
            ability
        );

        return true;
    }

    public List<AbilityData>
        GetLearnedAbilities()
    {
        return learnedAbilities;
    }

    public List<AbilityData>
        GetAllSpellbookEntries()
    {
        return new List<AbilityData>(
            learnedAbilities
        );
    }

    // =========================================================
    // ACTION BAR API
    // =========================================================

    public AbilityData[]
        GetEquippedAbilities()
    {
        return equippedAbilities;
    }

    public AbilityData GetEquippedAbility(
        int slotIndex)
    {
        if (!IsValidActionSlot(
                slotIndex))
        {
            return null;
        }

        return equippedAbilities[
            slotIndex
        ];
    }

    public bool SetEquippedAbility(
        int slotIndex,
        AbilityData ability)
    {
        if (!IsValidActionSlot(
                slotIndex))
        {
            return false;
        }

        /*
         * Base Attacks får endast ligga i
         * Base Attack-slotarna.
         */
        if (ability != null &&
            ability.IsBaseAttack)
        {
            return false;
        }

        if (equippedAbilities[
                slotIndex] ==
            ability)
        {
            return true;
        }

        equippedAbilities[
            slotIndex] =
            ability;

        OnActionSlotChanged?.Invoke(
            slotIndex,
            ability
        );

        return true;
    }

    public bool ClearEquippedAbility(
        int slotIndex)
    {
        return SetEquippedAbility(
            slotIndex,
            null
        );
    }

    // =========================================================
    // BASE ATTACK API
    // =========================================================

    public AbilityData ActiveBaseAttack =>
        GetBaseAttack(
            PrimaryBaseAttackSlotIndex
        );

    public AbilityData PrimaryBaseAttack =>
        GetBaseAttack(
            PrimaryBaseAttackSlotIndex
        );

    public AbilityData SecondaryBaseAttack =>
        GetBaseAttack(
            SecondaryBaseAttackSlotIndex
        );

    public bool CanSwapBaseAttacks =>
        SecondaryBaseAttack != null;

    public AbilityData GetBaseAttack(
        int slotIndex)
    {
        if (!IsValidBaseAttackSlot(
                slotIndex))
        {
            return null;
        }

        return baseAttacks[
            slotIndex
        ];
    }

    public AbilityData GetActiveBaseAttack()
    {
        return ActiveBaseAttack;
    }

    public bool EquipBaseAttack(
        int slotIndex,
        AbilityData attack)
    {
        if (!IsValidBaseAttackSlot(
                slotIndex))
        {
            Debug.LogWarning(
                $"Ogiltigt Base Attack-slotindex: " +
                $"{slotIndex}.",
                this
            );

            return false;
        }

        if (attack != null &&
            !attack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Ability '{attack.abilityName}' är inte " +
                $"markerad som BaseAttack och kan därför " +
                $"inte utrustas i en Base Attack-slot.",
                this
            );

            return false;
        }

        AbilityData currentAttack =
            baseAttacks[
                slotIndex
            ];

        if (currentAttack == attack)
            return true;

        int otherSlotIndex =
            GetOtherBaseAttackSlotIndex(
                slotIndex
            );

        AbilityData otherAttack =
            baseAttacks[
                otherSlotIndex
            ];

        /*
         * Samma AbilityData får inte ligga i båda
         * Base Attack-slotarna.
         *
         * Om attacken redan ligger i den andra slotten
         * byter slotarna plats.
         */
        if (attack != null &&
            otherAttack == attack)
        {
            baseAttacks[
                slotIndex] =
                attack;

            baseAttacks[
                otherSlotIndex] =
                currentAttack;

            RaiseBaseAttackSlotChanged(
                slotIndex
            );

            RaiseBaseAttackSlotChanged(
                otherSlotIndex
            );

            RaiseActiveBaseAttackChanged();

            return true;
        }

        baseAttacks[
            slotIndex] =
            attack;

        RaiseBaseAttackSlotChanged(
            slotIndex
        );

        if (slotIndex ==
            PrimaryBaseAttackSlotIndex)
        {
            RaiseActiveBaseAttackChanged();
        }

        return true;
    }

    public bool ClearBaseAttack(
        int slotIndex)
    {
        return EquipBaseAttack(
            slotIndex,
            null
        );
    }

    public bool SwapBaseAttacks()
    {
        if (!CanSwapBaseAttacks)
            return false;

        AbilityData previousPrimary =
            baseAttacks[
                PrimaryBaseAttackSlotIndex
            ];

        baseAttacks[
            PrimaryBaseAttackSlotIndex] =
            baseAttacks[
                SecondaryBaseAttackSlotIndex
            ];

        baseAttacks[
            SecondaryBaseAttackSlotIndex] =
            previousPrimary;

        RaiseBaseAttackSlotChanged(
            PrimaryBaseAttackSlotIndex
        );

        RaiseBaseAttackSlotChanged(
            SecondaryBaseAttackSlotIndex
        );

        RaiseActiveBaseAttackChanged();

        return true;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private static bool IsValidActionSlot(
        int slotIndex)
    {
        return
            slotIndex >= 0 &&
            slotIndex <
            ActionSlotCount;
    }

    private static bool IsValidBaseAttackSlot(
        int slotIndex)
    {
        return
            slotIndex >= 0 &&
            slotIndex <
            BaseAttackSlotCount;
    }

    private static int
        GetOtherBaseAttackSlotIndex(
            int slotIndex)
    {
        return
            slotIndex ==
            PrimaryBaseAttackSlotIndex
                ? SecondaryBaseAttackSlotIndex
                : PrimaryBaseAttackSlotIndex;
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void RaiseBaseAttackSlotChanged(
        int slotIndex)
    {
        OnBaseAttackSlotChanged?.Invoke(
            slotIndex,
            GetBaseAttack(
                slotIndex
            )
        );
    }

    private void RaiseActiveBaseAttackChanged()
    {
        OnActiveBaseAttackChanged?.Invoke(
            ActiveBaseAttack
        );
    }

#if UNITY_EDITOR

    // =========================================================
    // EDITOR VALIDATION
    // =========================================================

    private void OnValidate()
    {
        learnedAbilities ??=
            new List<AbilityData>();

        EnsureActionSlotArray();
        EnsureBaseAttackSlotArray();

        RemoveDuplicateLearnedAbilities();

        ValidateActionSlots();
        ValidateBaseAttackSlots();
    }

    private void EnsureActionSlotArray()
    {
        if (equippedAbilities != null &&
            equippedAbilities.Length ==
                ActionSlotCount)
        {
            return;
        }

        AbilityData[] resized =
            new AbilityData[
                ActionSlotCount
            ];

        if (equippedAbilities != null)
        {
            int copyCount =
                Mathf.Min(
                    equippedAbilities.Length,
                    resized.Length
                );

            for (int i = 0;
                 i < copyCount;
                 i++)
            {
                resized[i] =
                    equippedAbilities[i];
            }
        }

        equippedAbilities =
            resized;
    }

    private void EnsureBaseAttackSlotArray()
    {
        if (baseAttacks != null &&
            baseAttacks.Length ==
                BaseAttackSlotCount)
        {
            return;
        }

        AbilityData[] resized =
            new AbilityData[
                BaseAttackSlotCount
            ];

        if (baseAttacks != null)
        {
            int copyCount =
                Mathf.Min(
                    baseAttacks.Length,
                    resized.Length
                );

            for (int i = 0;
                 i < copyCount;
                 i++)
            {
                resized[i] =
                    baseAttacks[i];
            }
        }

        baseAttacks =
            resized;
    }

    private void RemoveDuplicateLearnedAbilities()
    {
        HashSet<AbilityData> seen =
            new();

        for (int i =
                 learnedAbilities.Count - 1;
             i >= 0;
             i--)
        {
            AbilityData ability =
                learnedAbilities[i];

            if (ability == null)
                continue;

            if (seen.Add(
                    ability))
            {
                continue;
            }

            learnedAbilities.RemoveAt(
                i
            );
        }
    }

    private void ValidateActionSlots()
    {
        for (int i = 0;
             i < equippedAbilities.Length;
             i++)
        {
            AbilityData ability =
                equippedAbilities[i];

            if (ability == null ||
                !ability.IsBaseAttack)
            {
                continue;
            }

            Debug.LogWarning(
                $"'{ability.abilityName}' låg i vanlig " +
                $"ActionBar-slot {i}, men är en BaseAttack. " +
                $"Slotten har tömts.",
                this
            );

            equippedAbilities[i] =
                null;
        }
    }

    private void ValidateBaseAttackSlots()
    {
        for (int i = 0;
             i < baseAttacks.Length;
             i++)
        {
            AbilityData ability =
                baseAttacks[i];

            if (ability == null ||
                ability.IsBaseAttack)
            {
                continue;
            }

            Debug.LogWarning(
                $"'{ability.abilityName}' låg i Base Attack-slot " +
                $"{i}, men är inte BaseAttack. Slotten har tömts.",
                this
            );

            baseAttacks[i] =
                null;
        }

        if (baseAttacks[
                PrimaryBaseAttackSlotIndex] !=
                null &&
            baseAttacks[
                PrimaryBaseAttackSlotIndex] ==
            baseAttacks[
                SecondaryBaseAttackSlotIndex])
        {
            baseAttacks[
                SecondaryBaseAttackSlotIndex] =
                null;
        }
    }

#endif
}
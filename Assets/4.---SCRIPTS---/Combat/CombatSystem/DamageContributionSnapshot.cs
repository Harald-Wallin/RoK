using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DamageContributionEntry
{
    public CharacterStats CreditOwner { get; }

    public int AppliedDamage { get; }

    public DamageContributionEntry(
        CharacterStats creditOwner,
        int appliedDamage)
    {
        CreditOwner = creditOwner;

        AppliedDamage =
            Mathf.Max(
                0,
                appliedDamage
            );
    }
}

public sealed class DamageContributionSnapshot
{
    private static readonly
        IReadOnlyList<DamageContributionEntry>
        EmptyEntries =
            Array.Empty<DamageContributionEntry>();

    private readonly DamageContributionEntry[] entries;

    public IReadOnlyList<DamageContributionEntry> Entries =>
        entries ??
        EmptyEntries;

    /// <summary>
    /// All faktisk HP-förlust i encountert, inklusive skada
    /// utan identifierad credit owner.
    /// </summary>
    public int TotalAppliedDamage { get; }

    public int UnattributedDamage { get; }

    public DamageContributionSnapshot(
        IReadOnlyDictionary<CharacterStats, int> contributions,
        int unattributedDamage)
    {
        UnattributedDamage =
            Mathf.Max(
                0,
                unattributedDamage
            );

        List<DamageContributionEntry> copiedEntries =
            new();

        int attributedTotal = 0;

        if (contributions != null)
        {
            foreach (KeyValuePair<CharacterStats, int> pair
                     in contributions)
            {
                if (pair.Key == null ||
                    pair.Value <= 0)
                {
                    continue;
                }

                copiedEntries.Add(
                    new DamageContributionEntry(
                        pair.Key,
                        pair.Value
                    )
                );

                attributedTotal +=
                    pair.Value;
            }
        }

        entries =
            copiedEntries.ToArray();

        TotalAppliedDamage =
            attributedTotal +
            UnattributedDamage;
    }

    public int GetAppliedDamage(
        CharacterStats creditOwner)
    {
        if (creditOwner == null ||
            entries == null)
        {
            return 0;
        }

        for (int i = 0;
             i < entries.Length;
             i++)
        {
            DamageContributionEntry entry =
                entries[i];

            if (entry == null)
                continue;

            if (entry.CreditOwner ==
                creditOwner)
            {
                return entry.AppliedDamage;
            }
        }

        return 0;
    }

    public float GetDamageShare(
        CharacterStats creditOwner)
    {
        if (creditOwner == null ||
            TotalAppliedDamage <= 0)
        {
            return 0f;
        }

        int damage =
            GetAppliedDamage(
                creditOwner
            );

        return
            (float)damage /
            TotalAppliedDamage;
    }

    public bool HasMinimumShare(
        CharacterStats creditOwner,
        float minimumShare)
    {
        float required =
            Mathf.Clamp01(
                minimumShare
            );

        return
            GetDamageShare(
                creditOwner
            ) + 0.0001f >=
            required;
    }

    public CharacterStats GetTopContributor()
    {
        if (entries == null ||
            entries.Length == 0)
        {
            return null;
        }

        CharacterStats topContributor =
            null;

        int highestDamage = 0;

        for (int i = 0;
             i < entries.Length;
             i++)
        {
            DamageContributionEntry entry =
                entries[i];

            if (entry == null ||
                entry.CreditOwner == null)
            {
                continue;
            }

            if (entry.AppliedDamage <=
                highestDamage)
            {
                continue;
            }

            highestDamage =
                entry.AppliedDamage;

            topContributor =
                entry.CreditOwner;
        }

        return topContributor;
    }

    public int GetHighestAppliedDamage()
    {
        if (entries == null ||
            entries.Length == 0)
        {
            return 0;
        }

        int highestDamage =
            0;

        for (int i = 0;
             i < entries.Length;
             i++)
        {
            DamageContributionEntry entry =
                entries[i];

            if (entry == null ||
                entry.CreditOwner == null)
            {
                continue;
            }

            highestDamage =
                Mathf.Max(
                    highestDamage,
                    entry.AppliedDamage
                );
        }

        return highestDamage;
    }

    /// <summary>
    /// Returnerar true om creditOwner delar eller ensam har
    /// den högsta damage contribution.
    ///
    /// Detta är avsiktligt INTE samma sak som att ha >= 50 %
    /// av all damage.
    ///
    /// Exempel:
    ///
    /// Player 40
    /// Guard A 30
    /// Guard B 30
    ///
    /// Player är top contributor.
    /// </summary>
    public bool IsTopContributor(
        CharacterStats creditOwner,
        bool allowTies = true)
    {
        if (creditOwner == null)
            return false;

        int ownerDamage =
            GetAppliedDamage(
                creditOwner
            );

        if (ownerDamage <= 0)
            return false;

        int highestDamage =
            GetHighestAppliedDamage();

        if (highestDamage <= 0)
            return false;

        if (allowTies)
        {
            return
                ownerDamage >=
                highestDamage;
        }

        /*
         * Om ties inte tillåts måste ingen ANNAN contributor
         * ha lika mycket eller mer damage.
         */
        if (entries == null)
            return false;

        for (int i = 0;
             i < entries.Length;
             i++)
        {
            DamageContributionEntry entry =
                entries[i];

            if (entry == null ||
                entry.CreditOwner == null ||
                entry.CreditOwner ==
                creditOwner)
            {
                continue;
            }

            if (entry.AppliedDamage >=
                ownerDamage)
            {
                return false;
            }
        }

        return true;
    }
}

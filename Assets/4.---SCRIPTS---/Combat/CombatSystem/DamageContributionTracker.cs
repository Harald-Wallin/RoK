using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageContributionTracker :
    MonoBehaviour
{
    private readonly Dictionary<CharacterStats, int>
        contributions =
            new();

    private int unattributedDamage;

    public int TotalAppliedDamage
    {
        get
        {
            int total =
                unattributedDamage;

            foreach (KeyValuePair<CharacterStats, int> pair
                     in contributions)
            {
                total +=
                    Mathf.Max(
                        0,
                        pair.Value
                    );
            }

            return total;
        }
    }

    public void RegisterDamage(
        DamageSourceContext source,
        int appliedDamage)
    {
        if (appliedDamage <= 0)
            return;

        CharacterStats creditOwner =
            source.CreditOwner;

        if (creditOwner == null)
        {
            unattributedDamage +=
                appliedDamage;

            return;
        }

        if (contributions.TryGetValue(
                creditOwner,
                out int existingDamage))
        {
            contributions[creditOwner] =
                existingDamage +
                appliedDamage;

            return;
        }

        contributions.Add(
            creditOwner,
            appliedDamage
        );
    }

    public int GetAppliedDamage(
        CharacterStats creditOwner)
    {
        if (creditOwner == null)
            return 0;

        return contributions.TryGetValue(
            creditOwner,
            out int damage)
                ? damage
                : 0;
    }

    public float GetDamageShare(
        CharacterStats creditOwner)
    {
        int total =
            TotalAppliedDamage;

        if (creditOwner == null ||
            total <= 0)
        {
            return 0f;
        }

        return
            (float)GetAppliedDamage(
                creditOwner
            ) /
            total;
    }

    public DamageContributionSnapshot CreateSnapshot()
    {
        return new DamageContributionSnapshot(
            contributions,
            unattributedDamage
        );
    }

    public void ResetContributions()
    {
        contributions.Clear();
        unattributedDamage = 0;
    }
}

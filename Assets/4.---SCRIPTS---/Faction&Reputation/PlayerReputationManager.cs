using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReputationManager : MonoBehaviour
{
    public ReputationLevelDefinition levelDefinition;
    //public event Action<FactionReputationData> OnReputationChanged;
    public event System.Action<FactionReputationData> OnReputationChanged;

    public List<FactionReputationData> reputations = new List<FactionReputationData>();

    public ReputationLevelDefinition LevelDefinition =>
    levelDefinition;

    //Temporär hostility
    private Dictionary<Faction, float> temporaryHostilityTimers = new Dictionary<Faction, float>();

    [SerializeField]
    private float defaultCrimeHostilityDuration = 300f;

    public FactionReputationData GetReputation(Faction faction)
    {
        foreach (var rep in reputations)
        {
            if (rep.faction == faction)
                return rep;
        }

        return null;
    }

    public void DiscoverFaction(
    Faction faction)
    {
        if (faction == null)
            return;

        FactionReputationData reputation =
            GetReputation(
                faction
            );

        if (reputation == null)
        {
            reputation =
                new FactionReputationData
                {
                    faction = faction,
                    discovered = true,
                    level = 1,
                    currentXP = 0
                };

            reputations.Add(
                reputation
            );
        }
        else
        {
            if (reputation.discovered)
                return;

            reputation.discovered = true;
        }

        if (faction.showInReputationWindow)
        {
            AnnouncementSpawner.Instance
                ?.ShowFactionDiscovered(
                faction.factionName
                );

            FactionNotificationManager.Instance
                ?.RegisterNewFaction();
        }

        OnReputationChanged?.Invoke(
            reputation
        );
    }

    public bool AddReputation(
    Faction faction,
    int amount)
    {
        if (faction == null)
            return false;

        if (amount == 0)
            return false;

        if (levelDefinition == null)
        {
            Debug.LogError(
                "PlayerReputationManager saknar ReputationLevelDefinition.",
                this
            );

            return false;
        }

        FactionReputationData rep =
            GetReputation(
                faction
            );

        int oldLevel =
            rep != null
                ? rep.level
                : 1;

        if (rep == null)
        {
            rep =
                new FactionReputationData
                {
                    faction = faction,
                    discovered = false,
                    level = 1,
                    currentXP = 0
                };

            reputations.Add(
                rep
            );
        }

        rep.level =
            Mathf.Clamp(
                rep.level,
                1,
                Mathf.Max(
                    1,
                    levelDefinition.maxLevel
                )
            );

        rep.currentXP += amount;

        while (rep.level <
               levelDefinition.maxLevel)
        {
            int requiredXP =
                Mathf.Max(
                    1,
                    levelDefinition.GetXPRequired(
                        rep.level
                    )
                );

            if (rep.currentXP <
                requiredXP)
            {
                break;
            }

            rep.currentXP -=
                requiredXP;

            rep.level++;
        }

        while (rep.currentXP < 0 &&
               rep.level > 1)
        {
            rep.level--;

            rep.currentXP +=
                Mathf.Max(
                    1,
                    levelDefinition.GetXPRequired(
                        rep.level
                    )
                );
        }

        if (rep.level <= 1 &&
            rep.currentXP < 0)
        {
            rep.currentXP = 0;
        }

        if (rep.level >=
            levelDefinition.maxLevel)
        {
            rep.level =
                levelDefinition.maxLevel;

            rep.currentXP =
                Mathf.Max(
                    0,
                    rep.currentXP
                );
        }

        if (rep.level != oldLevel)
        {
            string tierName =
                GetReputationTierName(
                    rep.level
                );

            Color tierColor =
                ReputationColorUtility.GetColor(
                    rep.level
                );

            AudioClip rankSound =
                levelDefinition.GetTierSound(
                    rep.level
                );

            AnnouncementSpawner.Instance
                ?.ShowReputationRankChanged(
                    tierName,
                    tierColor,
                    faction.factionName,
                    rankSound
                );
        }

        OnReputationChanged?.Invoke(
            rep
        );

        return true;
    }

    public int GetReputationLevel(
    Faction faction)
    {
        if (faction == null)
            return 0;

        FactionReputationData reputation =
            GetReputation(
                faction
            );

        /*
         * En undiscovered eller helt okänd faction räknas som
         * nivå 0 för requirements.
         */
        if (reputation == null ||
            !reputation.discovered)
        {
            return 0;
        }

        return Mathf.Max(
            1,
            reputation.level
        );
    }

    public bool HasReputationLevel(
        Faction faction,
        int minimumLevel)
    {
        if (faction == null)
            return false;

        int requiredLevel =
            Mathf.Max(
                1,
                minimumLevel
            );

        return GetReputationLevel(
                   faction
               ) >= requiredLevel;
    }

    public string GetReputationTierName(
        int level)
    {
        if (levelDefinition == null)
        {
            return $"Rank {Mathf.Max(1, level)}";
        }

        if (levelDefinition.tiers == null ||
            levelDefinition.tiers.Count == 0)
        {
            return $"Rank {Mathf.Max(1, level)}";
        }

        return levelDefinition.GetTierName(
            Mathf.Max(
                1,
                level
            )
        );
    }

    public int GetMaximumReputationLevel()
    {
        if (levelDefinition == null)
            return 1;

        return Mathf.Max(
            1,
            levelDefinition.maxLevel
        );
    }

    public FactionReputationData GetTrackedFaction()
    {
        foreach (var rep in reputations)
        {
            if (rep.tracked)
                return rep;
        }

        return null;
    }


    public ReputationState GetReputationState(Faction faction)
    {
        var rep = GetReputation(faction);

        if (rep == null)
            return ReputationState.Indifferent;

        int level = rep.level;

        if (level <= 1)
            return ReputationState.Hated;
        else if (level == 2)
            return ReputationState.Untrusted;
        else if (level == 3)
            return ReputationState.Indifferent;
        else if (level == 4)
            return ReputationState.Favoured;
        else if (level == 5)
            return ReputationState.Renowned;
        else if (level == 6)    
            return ReputationState.Praised;
        else 
            return ReputationState.Revered;
    }

    public bool IsTracked(Faction faction)
    {
        var data = GetReputation(faction);
        return data != null && data.tracked;
    }

    public void SetTracked(Faction faction, bool value)
    {
        foreach (var rep in reputations)
            rep.tracked = false;

        if (!value)
        {
            OnReputationChanged?.Invoke(GetTrackedFaction());
            return;
        }

        var data = GetReputation(faction);
        if (data == null) return;

        data.tracked = true;

        OnReputationChanged?.Invoke(data);
    }

    public bool IsMurderEnabled(
    Faction faction)
    {
        if (faction == null)
            return false;

        FactionReputationData data =
            GetReputation(faction);

        return data != null &&
               data.murderEnabled;
    }

    public void SetMurderEnabled(Faction faction, bool value)
    {
        var data = GetReputation(faction);
        if (data == null) return;

        data.murderEnabled = value;
        OnReputationChanged?.Invoke(data);
    }

}


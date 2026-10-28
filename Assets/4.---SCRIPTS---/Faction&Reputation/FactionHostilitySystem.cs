using System.Collections.Generic;
using UnityEngine;

public class FactionHostilitySystem : MonoBehaviour
{
    public static FactionHostilitySystem Instance;

    private class HostilityEntry
    {
        public PlayerStats player;
        public float timer;
    }

    private readonly Dictionary<Faction,
        List<HostilityEntry>> factionHostilities =
        new();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        foreach (var kvp in factionHostilities)
        {
            List<HostilityEntry> entries =
                kvp.Value;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                entries[i].timer -= Time.deltaTime;

                if (entries[i].timer <= 0f)
                {
                    entries.RemoveAt(i);
                }
            }
        }
    }

    public void AddHostility(
        Faction faction,
        PlayerStats player,
        float duration
    )
    {
        if (faction == null || player == null)
            return;

        if (!factionHostilities.ContainsKey(faction))
        {
            factionHostilities[faction] =
                new List<HostilityEntry>();
        }

        List<HostilityEntry> entries =
            factionHostilities[faction];

        foreach (var entry in entries)
        {
            if (entry.player == player)
            {
                entry.timer =
                    Mathf.Max(entry.timer, duration);

                return;
            }
        }

        entries.Add(
            new HostilityEntry
            {
                player = player,
                timer = duration
            }
        );
    }

    public bool IsHostileToPlayer(
        Faction faction,
        PlayerStats player
    )
    {
        if (faction == null || player == null)
            return false;

        if (!factionHostilities.TryGetValue(
                faction,
                out var entries))
        {
            return false;
        }

        foreach (var entry in entries)
        {
            if (entry.player == player)
                return true;
        }

        return false;
    }
}

using System;
using UnityEngine;

public class FactionNotificationManager : MonoBehaviour
{
    public static FactionNotificationManager Instance;

    public bool HasUnreadFactions =>
        unreadDiscoveries > 0;

    public event Action<bool> OnUnreadStateChanged;

    private int unreadDiscoveries;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterNewFaction()
    {
        unreadDiscoveries++;

        OnUnreadStateChanged?.Invoke(
            HasUnreadFactions
        );
    }

    public void ClearUnread()
    {
        unreadDiscoveries = 0;

        OnUnreadStateChanged?.Invoke(false);
    }
}

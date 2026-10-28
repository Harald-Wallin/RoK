using System;
using UnityEngine;

public class TalentNotificationManager : MonoBehaviour
{
    public static TalentNotificationManager Instance;

    public event Action<bool> OnUnreadStateChanged;

    public bool HasUnreadPoints { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void NotifyNewTalentPoints()
    {
        HasUnreadPoints = true;
        OnUnreadStateChanged?.Invoke(true);
    }

    public void MarkAsViewed()
    {
        if (!HasUnreadPoints)
            return;

        HasUnreadPoints = false;

        OnUnreadStateChanged?.Invoke(false);
    }
}
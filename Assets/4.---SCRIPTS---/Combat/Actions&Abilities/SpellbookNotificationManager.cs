using UnityEngine;

public class SpellbookNotificationManager : MonoBehaviour
{
    public static SpellbookNotificationManager Instance;

    public bool HasUnreadEntries { get; private set; }

    public System.Action<bool> OnUnreadStateChanged;

    void Awake()
    {
        Instance = this;
    }

    public void NotifyNewEntry()
    {
        HasUnreadEntries = true;

        OnUnreadStateChanged?.Invoke(true);
    }

    public void ClearUnread()
    {
        if (!HasUnreadEntries)
            return;

        HasUnreadEntries = false;

        OnUnreadStateChanged?.Invoke(false);
    }
}

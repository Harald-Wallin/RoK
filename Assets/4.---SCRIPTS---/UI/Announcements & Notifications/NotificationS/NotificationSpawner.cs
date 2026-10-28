using System.Collections.Generic;
using UnityEngine;

public sealed class NotificationSpawner :
    MonoBehaviour
{
    public static NotificationSpawner Instance
    {
        get;
        private set;
    }

    [SerializeField]
    private NotificationInstance prefab;

    [SerializeField]
    private Transform parentContainer;

    [SerializeField]
    private NotificationDatabase database;

    public NotificationDatabase Database =>
        database;

    private readonly Dictionary<string, float>
        cooldowns =
            new();

    private const float
        DuplicateCooldown =
            0.5f;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }

        Instance =
            this;
    }

    // =========================================================
    // STATIC NOTIFICATION
    // =========================================================

    public void Show(
        NotificationData data)
    {
        if (data == null)
            return;

        Show(
            data,
            data.message
        );
    }

    // =========================================================
    // DYNAMIC NOTIFICATION
    // =========================================================

    public void Show(
        NotificationData data,
        string message)
    {
        if (data == null ||
            prefab == null ||
            parentContainer == null)
        {
            return;
        }

        string resolvedMessage =
            string.IsNullOrWhiteSpace(
                message)
                ? data.message
                : message;

        string cooldownKey =
            BuildCooldownKey(
                data,
                resolvedMessage
            );

        if (cooldowns.ContainsKey(
                cooldownKey))
        {
            return;
        }

        NotificationInstance notification =
            Instantiate(
                prefab,
                parentContainer
            );

        notification.Initialize(
            data,
            resolvedMessage
        );

        cooldowns[cooldownKey] =
            DuplicateCooldown;

        UISoundManager.Instance?.Play(
            data.sound
        );
    }

    private void Update()
    {
        if (cooldowns.Count == 0)
            return;

        float deltaTime =
            Time.unscaledDeltaTime;

        List<string> keys =
            new List<string>(
                cooldowns.Keys
            );

        foreach (string key
                 in keys)
        {
            cooldowns[key] -=
                deltaTime;

            if (cooldowns[key] <= 0f)
            {
                cooldowns.Remove(
                    key
                );
            }
        }
    }

    private static string BuildCooldownKey(
        NotificationData data,
        string message)
    {
        return
            data.GetInstanceID() +
            "|" +
            message;
    }
}
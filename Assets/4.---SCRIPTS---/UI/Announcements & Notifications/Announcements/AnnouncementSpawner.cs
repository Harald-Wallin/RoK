using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnnouncementSpawner : MonoBehaviour
{
    public static AnnouncementSpawner Instance;

    [SerializeField]
    private AnnouncementInstance prefab;

    [SerializeField]
    private AnnouncementDatabase database;

    [SerializeField]
    private Transform parentContainer;

    private readonly Queue<AnnouncementRequest> queue =
        new();

    private bool isPlaying;

    private void Awake()
    {
        Instance = this;
    }

    // =========================================================
    // PUBLIC PRESENTATION API
    // =========================================================

    public void ShowLevelUp(
        int level)
    {
        QueueAnnouncement(
            database != null
                ? database.levelUp
                : null,
            BuildLevelUpMessage(
                level
            )
        );
    }

    public void ShowAbilityLearned(
        string abilityName)
    {
        QueueAnnouncement(
            database != null
                ? database.abilityLearned
                : null,
            BuildAbilityLearnedMessage(
                abilityName
            )
        );
    }

    public void ShowFactionDiscovered(
        string factionName)
    {
        QueueAnnouncement(
            database != null
                ? database.factionDiscovered
                : null,
            BuildFactionDiscoveryMessage(
                factionName
            )
        );
    }

    public void ShowReputationRankChanged(
        string rankName,
        Color rankColor,
        string factionName,
        AudioClip overrideSound = null)
    {
        QueueAnnouncement(
            database != null
                ? database.reputationRankChanged
                : null,
            BuildReputationRankMessage(
                rankName,
                rankColor,
                factionName
            ),
            overrideSound
        );
    }

    public void ShowFavourFailed(
        string favourName)
    {
        QueueAnnouncement(
            database != null
                ? database.favourFailed
                : null,
            BuildFavourFailedMessage(
                favourName
            )
        );
    }

    // =========================================================
    // MESSAGE BUILDING
    // =========================================================

    private static string BuildLevelUpMessage(
        int level)
    {
        return
            $"Hail!\n" +
            $"You reached\n" +
            $"Level {level}";
    }

    private static string BuildAbilityLearnedMessage(
        string abilityName)
    {
        return
            $"<size=24>Learned:</size>\n" +
            $"<size=48>{abilityName}</size>";
    }

    private static string BuildFactionDiscoveryMessage(
        string factionName)
    {
        return
            $"<size=55>Faction Discovered</size>\n" +
            $"<size=110>{factionName}</size>";
    }

    private static string BuildReputationRankMessage(
        string rankName,
        Color rankColor,
        string factionName)
    {
        string colorHex =
            ColorUtility.ToHtmlStringRGB(
                rankColor
            );

        return
            $"<size=24>You are now</size>\n" +
            $"<size=42><color=#{colorHex}>{rankName}</color></size>\n" +
            $"<size=24>with</size>\n" +
            $"<size=42>{factionName}</size>";
    }

    private static string BuildFavourFailedMessage(
        string favourName)
    {
        return
            $"<size=42>{favourName}</size>\n" +
            $"<size=55>FAILED</size>";
    }

    // =========================================================
    // QUEUE
    // =========================================================

    private void QueueAnnouncement(
        AnnouncementData data,
        string customMessage = null,
        AudioClip overrideSound = null)
    {
        if (data == null)
            return;

        queue.Enqueue(
            new AnnouncementRequest(
                data,
                customMessage,
                overrideSound
            )
        );

        if (!isPlaying)
        {
            StartCoroutine(
                ProcessQueue()
            );
        }
    }

    private IEnumerator ProcessQueue()
    {
        isPlaying = true;

        while (queue.Count > 0)
        {
            AnnouncementRequest request =
                queue.Dequeue();

            AnnouncementData data =
                request.data;

            AnnouncementInstance instance =
                Instantiate(
                    prefab,
                    parentContainer
                );

            instance.Initialize(
                data,
                request.customMessage
            );

            UISoundManager.Instance?.Play(
                request.overrideSound != null
                    ? request.overrideSound
                    : data.sound
            );

            yield return new WaitForSeconds(
                data.duration + 0.25f
            );
        }

        isPlaying = false;
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TrackedReputationUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerReputationManager
        reputationManager;

    [SerializeField]
    private Image reputationFill;

    [SerializeField]
    private TextMeshProUGUI factionNameText;

    [SerializeField]
    private TextMeshProUGUI tierText;

    [SerializeField]
    private TMP_Text reputationText;


    [Header("Tracked Presentation")]

    [Tooltip(
        "Objekt som endast ska synas när en faction trackas. " +
        "Exempel: ReputationFill, FactionNameText, TierText och ReputationText.")]
    [SerializeField]
    private GameObject[] trackedContentObjects;


    [Header("No Tracked Reputation")]

    [Tooltip(
        "Knappen med !-ikonen som visas när ingen reputation trackas.")]
    [SerializeField]
    private Button untrackedReputationButton;


    private void Awake()
    {
        ConfigureFillImage(
            reputationFill
        );

        ConfigureUntrackedButton();

        /*
         * Viktigt:
         * vi börjar alltid från ett verkligt tomt state,
         * oavsett vilka testvärden som ligger i Inspectorn.
         */
        ShowUntrackedState();
    }


    private void Start()
    {
        if (reputationManager == null)
        {
            reputationManager =
                FindFirstObjectByType<
                    PlayerReputationManager>();
        }

        if (reputationManager == null)
        {
            Debug.LogWarning(
                $"{nameof(TrackedReputationUI)} kunde inte hitta " +
                $"{nameof(PlayerReputationManager)}.",
                this
            );

            ShowUntrackedState();

            return;
        }

        reputationManager
            .OnReputationChanged -=
            Refresh;

        reputationManager
            .OnReputationChanged +=
            Refresh;

        Refresh(
            reputationManager
                .GetTrackedFaction()
        );
    }


    private void OnDestroy()
    {
        if (reputationManager != null)
        {
            reputationManager
                .OnReputationChanged -=
                Refresh;
        }

        if (untrackedReputationButton != null)
        {
            untrackedReputationButton
                .onClick
                .RemoveListener(
                    HandleUntrackedReputationClicked
                );
        }
    }


    public void Refresh(
        FactionReputationData data)
    {
        if (reputationManager == null)
        {
            ShowUntrackedState();
            return;
        }

        var tracked =
            reputationManager
                .GetTrackedFaction();

        if (tracked == null ||
            tracked.faction == null)
        {
            ShowUntrackedState();

            return;
        }

        ShowTrackedState();


        if (factionNameText != null)
        {
            factionNameText.text =
                tracked.faction.factionName;
        }


        if (tierText != null)
        {
            tierText.text =
                reputationManager
                    .levelDefinition
                    .GetTierName(
                        tracked.level
                    );
        }


        float maximum =
            reputationManager
                .levelDefinition
                .GetXPRequired(
                    tracked.level
                );


        if (reputationFill != null)
        {
            reputationFill.fillAmount =
                maximum > 0f
                    ? Mathf.Clamp01(
                        tracked.currentXP /
                        maximum
                    )
                    : 0f;

            reputationFill.color =
                ReputationColorUtility
                    .GetColor(
                        tracked.level
                    );
        }


        if (reputationText != null)
        {
            reputationText.text =
                $"{tracked.currentXP}/{maximum}";
        }
    }


    private void ShowTrackedState()
    {
        SetTrackedContentVisible(
            true
        );

        if (untrackedReputationButton != null)
        {
            untrackedReputationButton
                .gameObject
                .SetActive(
                    false
                );
        }
    }


    private void ShowUntrackedState()
    {
        ClearTrackedPresentation();

        SetTrackedContentVisible(
            false
        );

        if (untrackedReputationButton != null)
        {
            untrackedReputationButton
                .gameObject
                .SetActive(
                    true
                );
        }
    }


    private void ClearTrackedPresentation()
    {
        if (reputationFill != null)
        {
            reputationFill.fillAmount =
                0f;
        }

        if (factionNameText != null)
        {
            factionNameText.text =
                string.Empty;
        }

        if (tierText != null)
        {
            tierText.text =
                string.Empty;
        }

        if (reputationText != null)
        {
            reputationText.text =
                string.Empty;
        }
    }


    private void SetTrackedContentVisible(
        bool visible)
    {
        if (trackedContentObjects == null)
            return;

        for (int i = 0;
             i < trackedContentObjects.Length;
             i++)
        {
            GameObject content =
                trackedContentObjects[i];

            if (content != null)
            {
                content.SetActive(
                    visible
                );
            }
        }
    }


    private void ConfigureUntrackedButton()
    {
        if (untrackedReputationButton == null)
            return;

        untrackedReputationButton
            .onClick
            .RemoveListener(
                HandleUntrackedReputationClicked
            );

        untrackedReputationButton
            .onClick
            .AddListener(
                HandleUntrackedReputationClicked
            );
    }


    private void HandleUntrackedReputationClicked()
    {
        GlobalUIManager.Instance
            ?.ToggleReputationWindow();
    }


    private static void ConfigureFillImage(
        Image image)
    {
        if (image == null)
            return;

        image.type =
            Image.Type.Filled;

        image.fillMethod =
            Image.FillMethod.Horizontal;

        image.fillOrigin =
            (int)Image.OriginHorizontal.Left;
    }
}
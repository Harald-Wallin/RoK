
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DeedDetailsWindowUI :
    MonoBehaviour,
    IUIWindow
{
    [Header("Header")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private Button closeButton;

    [Header("Content")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Progress")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image progressFill;

    [Header("Rewards")]
    [SerializeField] private Transform rewardContainer;
    [SerializeField] private DeedRewardRowUI rewardRowPrefab;

    [Tooltip("Optional: heading shown above rewards.")]
    [SerializeField] private GameObject rewardsHeader;

    private readonly List<DeedRewardRowUI>
        rewardRows = new();

    [Header("Tracking")]
    [SerializeField] private Toggle trackToggle;
    [SerializeField] private TMP_Text trackLabel;

    private DeedRuntime currentDeed;
    private PlayerDeedManager deedManager;

    // Null means the current active rank.
    private DeedRankDefinition rankOverride;

    public bool IsOpen => gameObject.activeSelf;

    public DeedRuntime CurrentDeed =>
        currentDeed;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        if (trackToggle != null)
        {
            trackToggle.onValueChanged.AddListener(
                HandleTrackChanged
            );
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        if (trackToggle != null)
        {
            trackToggle.onValueChanged.RemoveListener(
                HandleTrackChanged
            );
        }
    }

    // =====================================================
    // OPEN / CLOSE
    // =====================================================

    public void Show(
        DeedRuntime deed,
        PlayerDeedManager manager,
        DeedRankDefinition selectedRank = null)
    {
        if (deed == null || manager == null)
            return;

        currentDeed = deed;
        deedManager = manager;
        rankOverride = selectedRank;

        gameObject.SetActive(true);

        Refresh();
    }

    public bool IsShowing(
        DeedRuntime deed,
        DeedRankDefinition selectedRank)
    {
        return IsOpen &&
               ReferenceEquals(currentDeed, deed) &&
               ReferenceEquals(rankOverride, selectedRank);
    }

    public void Close()
    {
        currentDeed = null;
        deedManager = null;
        rankOverride = null;

        HideRewardRows();

        gameObject.SetActive(false);
    }

    // =====================================================
    // PRESENTATION
    // =====================================================

    public void Refresh()
    {
        if (currentDeed == null ||
            currentDeed.Definition == null)
        {
            return;
        }

        DeedDefinition definition =
            currentDeed.Definition;

        DeedRankDefinition rank =
            rankOverride ?? currentDeed.CurrentRank;

        if (rank == null)
        {
            HideRewardRows();
            return;
        }

        // =================================================
        // UNKNOWN DEED
        // =================================================

        if (!currentDeed.IsRevealed)
        {
            SetText(
                titleText,
                "Unknown Deed"
            );

            SetText(
                categoryText,
                "Undiscovered"
            );

            SetText(
                descriptionText,
                "The details of this deed " +
                "have not yet been revealed."
            );

            if (artworkImage != null)
            {
                artworkImage.sprite = null;
                artworkImage.enabled = false;
            }

            SetText(
                progressText,
                "Unknown"
            );

            if (progressFill != null)
                progressFill.fillAmount = 0f;

            HideRewardRows();
            UpdateTrackToggle();

            return;
        }

        // =================================================
        // TITLE
        // =================================================

        SetText(
            titleText,
            $"{definition.DisplayName} {rank.RankLabel}".Trim()
        );

        // =================================================
        // CATEGORY
        // =================================================

        SetText(
            categoryText,
            definition.Category != null
                ? definition.Category.DisplayName
                : "General"
        );

        // =================================================
        // ARTWORK
        // =================================================

        Sprite artwork =
            rank.ImageOverride != null
                ? rank.ImageOverride
                : definition.Image;

        if (artworkImage != null)
        {
            artworkImage.sprite = artwork;
            artworkImage.enabled = artwork != null;
        }

        // =================================================
        // DESCRIPTION
        // =================================================

        SetText(
            descriptionText,
            !string.IsNullOrWhiteSpace(
                rank.LongDescription)
                ? rank.LongDescription
                : definition.LongDescription
        );

        // =================================================
        // PROGRESS
        // =================================================

        bool completed =
            rankOverride != null
                ? currentDeed.HasCompleted(rankOverride)
                : currentDeed.AllRanksComplete;

        int required =
            Mathf.Max(
                1,
                rank.RequiredTotalProgress
            );

        int current =
            Mathf.Clamp(
                currentDeed.Progress,
                0,
                required
            );

        SetText(
            progressText,
            completed
                ? $"Completed · {required}/{required}"
                : $"{current}/{required}"
        );

        if (progressFill != null)
        {
            progressFill.fillAmount =
                completed
                    ? 1f
                    : (float)current / required;
        }

        // =================================================
        // REWARDS / TRACKING
        // =================================================

        RefreshRewardRows(rank);

        UpdateTrackToggle();
    }

    // =====================================================
    // REWARD ROWS
    // =====================================================

    private void RefreshRewardRows(
        DeedRankDefinition rank)
    {
        IReadOnlyList<DeedRewardPreview> rewards =
            rank?.Rewards;

        int validCount = 0;

        if (rewards != null)
        {
            foreach (DeedRewardPreview reward
                     in rewards)
            {
                if (reward != null)
                    validCount++;
            }
        }

        // No rewards = no reward section.
        if (validCount == 0 ||
            rewardContainer == null ||
            rewardRowPrefab == null)
        {
            HideRewardRows();
            return;
        }

        EnsureRewardRows(validCount);

        int index = 0;

        foreach (DeedRewardPreview reward
                 in rewards)
        {
            if (reward == null)
                continue;

            rewardRows[index].Bind(reward);
            index++;
        }

        for (int i = index;
             i < rewardRows.Count;
             i++)
        {
            rewardRows[i]?.Hide();
        }

        rewardContainer.gameObject.SetActive(true);

        if (rewardsHeader != null)
            rewardsHeader.SetActive(true);
    }

    private void EnsureRewardRows(int required)
    {
        while (rewardRows.Count < required)
        {
            DeedRewardRowUI row =
                Instantiate(
                    rewardRowPrefab,
                    rewardContainer
                );

            row.Hide();
            rewardRows.Add(row);
        }
    }

    private void HideRewardRows()
    {
        foreach (DeedRewardRowUI row
                 in rewardRows)
        {
            row?.Hide();
        }

        if (rewardContainer != null)
        {
            rewardContainer.gameObject.SetActive(
                false
            );
        }

        if (rewardsHeader != null)
        {
            rewardsHeader.SetActive(false);
        }
    }

    // =====================================================
    // TRACKING
    // =====================================================

    private void HandleTrackChanged(bool value)
    {
        if (currentDeed == null ||
            deedManager == null ||
            !currentDeed.IsRevealed)
        {
            UpdateTrackToggle();
            return;
        }

        if (currentDeed.IsTracked != value)
        {
            deedManager.SetTracked(
                currentDeed.Definition,
                value
            );
        }

        UpdateTrackToggle();
    }

    private void UpdateTrackToggle()
    {
        if (trackToggle == null)
            return;

        bool canTrack =
            currentDeed != null &&
            currentDeed.IsRevealed;

        trackToggle.interactable = canTrack;

        trackToggle.SetIsOnWithoutNotify(
            canTrack &&
            currentDeed.IsTracked
        );

        if (trackLabel != null)
        {
            trackLabel.text =
                !canTrack
                    ? "Undiscovered"
                    : currentDeed.IsTracked
                        ? "Tracking"
                        : "Track Deed";
        }
    }

    private static void SetText(
        TMP_Text field,
        string value)
    {
        if (field != null)
            field.text = value;
    }
}


using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class DeedCardUI : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Progress")]
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject completedIndicator;

    [Header("Rewards")]
    [SerializeField] private TMP_Text rewardText;

    private Button button;

    private DeedRuntime runtime;

    // Null = show current rank.
    // Assigned = show a specific completed rank.
    private DeedRankDefinition selectedRank;

    private Action<DeedRuntime, DeedRankDefinition>
        clicked;

    private void Awake()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(
            HandleClicked
        );
    }

    // =====================================================
    // BINDING
    // =====================================================

    public void Bind(
        DeedRuntime deed,
        DeedRankDefinition rankOverride,
        Action<DeedRuntime, DeedRankDefinition> onClicked)
    {
        runtime = deed;
        selectedRank = rankOverride;
        clicked = onClicked;

        Refresh();
    }

    // =====================================================
    // PRESENTATION
    // =====================================================

    public void Refresh()
    {
        if (runtime == null ||
            runtime.Definition == null)
        {
            return;
        }

        DeedDefinition definition =
            runtime.Definition;

        DeedRankDefinition rank =
            selectedRank ?? runtime.CurrentRank;

        if (rank == null)
            return;

        // =================================================
        // HIDDEN / UNKNOWN
        // =================================================

        if (!runtime.IsRevealed)
        {
            SetText(titleText, "???");

            SetText(
                descriptionText,
                "Undiscovered deed"
            );

            SetText(
                progressText,
                "Unknown"
            );

            if (artworkImage != null)
            {
                artworkImage.sprite = null;
                artworkImage.enabled = false;
            }

            if (progressFill != null)
                progressFill.fillAmount = 0f;

            if (completedIndicator != null)
                completedIndicator.SetActive(false);

            SetRewardText(string.Empty);

            return;
        }

        // =================================================
        // RANK STATUS
        // =================================================

        bool completed =
            selectedRank != null
                ? runtime.HasCompleted(selectedRank)
                : runtime.AllRanksComplete;

        // =================================================
        // TITLE
        // =================================================

        SetText(
            titleText,
            $"{definition.DisplayName} {rank.RankLabel}".Trim()
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

        string description =
            !string.IsNullOrWhiteSpace(
                rank.ShortDescription)
                ? rank.ShortDescription
                : definition.ShortDescription;

        SetText(descriptionText, description);

        // =================================================
        // PROGRESS
        // =================================================

        int required =
            Mathf.Max(
                1,
                rank.RequiredTotalProgress
            );

        int current =
            Mathf.Clamp(
                runtime.Progress,
                0,
                required
            );

        if (progressFill != null)
        {
            progressFill.fillAmount =
                completed
                    ? 1f
                    : (float)current / required;
        }

        SetText(
            progressText,
            completed
                ? $"Completed · {required}/{required}"
                : $"{current}/{required}"
        );

        if (completedIndicator != null)
        {
            completedIndicator.SetActive(
                completed
            );
        }

        // =================================================
        // REWARDS
        // =================================================

        SetRewardText(
            BuildRewardPreview(rank)
        );
    }

    // =====================================================
    // REWARD PREVIEW
    // =====================================================

    private void SetRewardText(string value)
    {
        if (rewardText == null)
            return;

        bool hasText =
            !string.IsNullOrWhiteSpace(value);

        rewardText.text =
            hasText ? value : string.Empty;

        rewardText.gameObject.SetActive(
            hasText
        );
    }

    private static string BuildRewardPreview(
        DeedRankDefinition rank)
    {
        // No rewards = no text.
        if (rank == null ||
            rank.Rewards == null ||
            rank.Rewards.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        int shown = 0;
        int remaining = 0;

        foreach (DeedRewardPreview reward
                 in rank.Rewards)
        {
            if (reward == null)
                continue;

            if (shown >= 2)
            {
                remaining++;
                continue;
            }

            if (builder.Length > 0)
                builder.Append(" · ");

            switch (reward.Kind)
            {
                case DeedRewardKind.Currency:
                    builder.Append(
                        $"Coins: {reward.Amount}"
                    );
                    break;

                case DeedRewardKind.Experience:
                    builder.Append(
                        $"EXP: {reward.Amount}"
                    );
                    break;

                default:
                    builder.Append(
                        $"{GetRewardKindName(reward.Kind)}: " +
                        reward.DisplayName
                    );
                    break;
            }

            shown++;
        }

        if (remaining > 0)
        {
            builder.Append(
                $" · +{remaining} more"
            );
        }

        return builder.ToString();
    }

    private static string GetRewardKindName(
        DeedRewardKind kind)
    {
        return kind switch
        {
            DeedRewardKind.Title => "Title",

            DeedRewardKind.TrainableAbility =>
                "Ability",

            DeedRewardKind.Item => "Item",

            DeedRewardKind.Cosmetic =>
                "Cosmetic",

            _ => "Reward"
        };
    }

    private static void SetText(
        TMP_Text field,
        string value)
    {
        if (field != null)
            field.text = value;
    }

    // =====================================================
    // CLICK
    // =====================================================

    private void HandleClicked()
    {
        if (runtime == null)
            return;

        clicked?.Invoke(
            runtime,
            selectedRank
        );
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClicked
            );
        }

        clicked = null;
    }
}

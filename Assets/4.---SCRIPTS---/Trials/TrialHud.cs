using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TrialHud : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private TrialZone zone;
    [SerializeField] private PlayerStats player;

    [Header("Player HUD")]
    [SerializeField] private GameObject trialPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private Image difficultyImage;
    [SerializeField] private Sprite oneSkullSprite;
    [SerializeField] private Sprite twoSkullsSprite;
    [SerializeField] private Sprite threeSkullsSprite;

    [Header("Editor / Development Build")]
    [SerializeField] private bool showDebugScoreboard = true;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TMP_Text[] debugColumns = new TMP_Text[7];

    private readonly StringBuilder objectiveLines = new();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private readonly List<TrialZone.Score> scoreRows = new();
    private readonly StringBuilder[] columnLines =
    {
        new(), new(), new(), new(), new(), new(), new()
    };
#endif

    private float nextRefresh;

    private void OnEnable()
    {
        nextRefresh = 0f;
        SetVisible(trialPanel, false);
        SetVisible(debugPanel, false);
    }

    private void OnDisable()
    {
        SetVisible(trialPanel, false);
        SetVisible(debugPanel, false);
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime < nextRefresh)
            return;

        nextRefresh = Time.unscaledTime + 0.2f;

        bool visible = zone != null && zone.isActiveAndEnabled &&
                       zone.Contains(player) && (zone.IsActive || zone.IsCoolingDown);

        SetVisible(trialPanel, visible);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        SetVisible(debugPanel, visible && showDebugScoreboard);

        if (visible && showDebugScoreboard && debugPanel != null)
            DrawScores();
#else
    SetVisible(debugPanel, false);
#endif

        if (!visible)
            return;

        RefreshDifficultySkulls(zone.DifficultySkullCount);

        titleText.text = string.IsNullOrWhiteSpace(zone.StageLabel)
            ? zone.TrialTitle
            : $"{zone.TrialTitle} • {zone.StageLabel}";

        SetVisible(timerText.gameObject, zone.HasTimer);

        if (zone.HasTimer)
        {
            int seconds = (int)Math.Ceiling(zone.RemainingSeconds);
            string clock = $"{seconds / 60:00}:{seconds % 60:00}";
            timerText.text = zone.IsCoolingDown ? $"Reset {clock}" : clock;
        }

        objectiveLines.Clear();

        if (zone.IsCoolingDown)
        {
            objectiveLines.Append(zone.ResetStatus);

            string rewardSummary = zone.GetRewardSummary(player);

            if (!string.IsNullOrWhiteSpace(rewardSummary))
                objectiveLines.AppendLine().Append(rewardSummary);
        }
        else
        {
            for (int i = 0; i < zone.ObjectiveCount; i++)
            {
                if (i > 0)
                    objectiveLines.AppendLine();

                objectiveLines.Append(zone.GetObjectiveText(i));
            }
        }

        objectiveText.text = objectiveLines.ToString();
    }

    private void RefreshDifficultySkulls(int count)
    {
        if (difficultyImage == null)
            return;

        (Sprite sprite, string label) = count switch
        {
            1 => (oneSkullSprite, "Simple"),
            2 => (twoSkullsSprite, "Challenging"),
            3 => (threeSkullsSprite, "Heroic"),
            _ => (null, string.Empty)
        };

        if (difficultyImage.sprite != sprite)
            difficultyImage.sprite = sprite;

        difficultyImage.enabled = sprite != null;

        if (difficultyImage.TryGetComponent(out UIButtonTooltip tooltip))
            tooltip.SetText(sprite != null ? label : string.Empty);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void DrawScores()
    {
        if (debugColumns.Length != columnLines.Length)
            return;

        foreach (StringBuilder column in columnLines)
            column.Clear();

        zone.CopyScoresTo(scoreRows);

        float previousContribution = -1f;
        int placement = 0;

        for (int i = 0; i < scoreRows.Count; i++)
        {
            TrialZone.Score score = scoreRows[i];
            float contribution = zone.GetContribution(score);

            if (!Mathf.Approximately(contribution, previousContribution))
                placement = i + 1;

            previousContribution = contribution;

            double total =
                (double)score.Damage + score.Healing + score.Tanking + score.Objectives;

            columnLines[0].AppendLine(placement.ToString());
            columnLines[1].AppendLine(score.Name);
            columnLines[2].AppendLine(score.Damage.ToString());
            columnLines[3].AppendLine(score.Healing.ToString());
            columnLines[4].AppendLine(score.Tanking.ToString("0.0"));
            columnLines[5].AppendLine("0");
            columnLines[6].AppendLine($"{total:0.#} ({contribution:P0})");
        }

        for (int i = 0; i < debugColumns.Length; i++)
        {
            if (debugColumns[i] != null)
                debugColumns[i].text = columnLines[i].ToString();
        }
    }
#endif

    private static void SetVisible(GameObject panel, bool visible)
    {
        if (panel != null && panel.activeSelf != visible)
            panel.SetActive(visible);
    }
}

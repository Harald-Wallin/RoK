using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PlayerStatRowUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text valueText;

    [SerializeField]
    private RectTransform tooltipTarget;

    [SerializeField]
    private ItemTooltip.TooltipAnchorMode
        tooltipAnchorMode =
            ItemTooltip
                .TooltipAnchorMode
                .TopRight;

    private CharacterStats stats;
    private StatDefinition definition;

    private RectTransform TooltipTarget =>
        tooltipTarget != null
            ? tooltipTarget
            : transform as RectTransform;

    public StatDefinition Definition =>
        definition;

    public void Bind(
        CharacterStats characterStats,
        StatDefinition statDefinition)
    {
        stats =
            characterStats;

        definition =
            statDefinition;

        Refresh();
    }

    public void Refresh()
    {
        if (stats == null ||
            definition == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        if (nameText != null)
        {
            nameText.text =
                definition.DisplayName;
        }

        if (valueText != null)
        {
            StatValueBreakdown breakdown =
                stats.GetStatValueBreakdown(
                    definition.stat
                );

            float displayValue =
                definition.GetDisplayValue(
                    breakdown.FinalValue
                );

            /*
             * TemporaryModifierDelta måste normaliseras på
             * samma sätt som slutvärdet.
             *
             * Detta påverkar endast färgvalets riktning:
             * positivt, negativt eller neutralt.
             */
            float displayTemporaryDelta =
                definition.GetDisplayValue(
                    breakdown
                        .TemporaryModifierDelta
                );

            string formattedValue =
                StatValueFormatter.FormatValue(
                    definition,
                    displayValue
                );

            valueText.text =
                StatValueFormatter
                    .GetColoredValue(
                        formattedValue,
                        displayTemporaryDelta
                    );
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (stats == null ||
            definition == null ||
            ItemTooltip.Instance == null)
        {
            return;
        }

        RectTransform target =
            TooltipTarget;

        if (target == null)
            return;

        /*
         * Primary stats som påverkar andra stats behåller sin
         * detaljerade scaling-tooltip.
         */
        if (stats.HasScalingContributions(
                definition.stat))
        {
            ItemTooltip.Instance.Show(
                new StatTooltipProvider(
                    stats,
                    definition
                ),
                target,
                stats,
                tooltipAnchorMode
            );

            return;
        }

        /*
         * Derived stats och andra stats utan scaling-output visar
         * istället sin korta beskrivning.
         */
        if (!string.IsNullOrWhiteSpace(
                definition.Description))
        {
            ItemTooltip.Instance.Show(
            new StatDescriptionTooltipProvider(
            definition
            ),
            target,stats,tooltipAnchorMode
            );
        }
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();
    }

    private void OnDisable()
    {
        ItemTooltip.Instance?.Hide();
    }
}
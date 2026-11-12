using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class WeaponTechniqueSlotUI : MonoBehaviour,
    ITooltipProvider, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Image frame;
    [SerializeField] private Material desaturatedIconMaterial;
    [SerializeField] private Color lockedTint = new Color32(130, 130, 130, 255);
    [SerializeField] private Color lockedFrame = new Color32(115, 115, 115, 255);
    [SerializeField] private Color trainableFrame = new Color32(104, 204, 109, 255);

    private Button button;
    private Color learnedTint;
    private Material learnedMaterial;

    private AbilityData ability;
    private DeedRuntime deed;
    private DeedRankDefinition rank;
    private Action<DeedRuntime, DeedRankDefinition> openDeed;
    private bool learned;
    private bool hovering;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (icon == null || frame == null)
        {
            Debug.LogError("Assign Icon and Frame on WeaponTechniqueSlotUI.", this);
            return;
        }

        learnedTint = icon.color;
        learnedMaterial = icon.material;
        button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Bind(
        AbilityData technique,
        DeedRuntime sourceDeed,
        DeedRankDefinition rewardRank,
        bool isLearned,
        Action<DeedRuntime, DeedRankDefinition> onOpenDeed)
    {
        if (icon == null || frame == null)
            return;

        ability = technique;
        deed = sourceDeed;
        rank = rewardRank;
        openDeed = onOpenDeed;
        learned = isLearned;

        bool occupied = ability != null;
        bool trainable = occupied && deed.HasCompleted(rank);

        button.interactable = occupied;
        icon.sprite = occupied ? ability.icon : null;
        icon.enabled = icon.sprite != null;
        icon.color = learned ? learnedTint : lockedTint;
        icon.material = !learned && desaturatedIconMaterial != null
            ? desaturatedIconMaterial
            : learnedMaterial;

        frame.enabled = occupied && !learned;
        frame.color = trainable ? trainableFrame : lockedFrame;

        if (!occupied)
            HideTooltip();
        else if (hovering)
            ItemTooltip.Instance?.Refresh();
    }

    public TooltipData GetTooltipData(CharacterStats viewer = null)
    {
        if (ability == null)
            return new TooltipData();

        TooltipData tooltip = ability.GetTooltipData(viewer);

        if (!learned && deed.HasCompleted(rank))
        {
            tooltip.subtitle =
                $"<color=#68CC6D>Available at trainer</color>\n{tooltip.subtitle}";
        }

        int required = rank.RequiredTotalProgress;
        int current = Mathf.Clamp(deed.Progress, 0, required);

        tooltip.requirements.Add(
            $"Deed: {deed.Definition.DisplayName} {rank.RankLabel} · {current}/{required}");

        return tooltip;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ability == null || ItemTooltip.Instance == null)
            return;

        hovering = true;
        ItemTooltip.Instance.Show(this, icon.rectTransform, PlayerReference.Player);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void HandleClick()
    {
        if (deed == null)
            return;

        HideTooltip();
        openDeed?.Invoke(deed, rank);
    }

    private void HideTooltip()
    {
        if (!hovering)
            return;

        hovering = false;
        ItemTooltip.Instance?.Hide();
    }
}

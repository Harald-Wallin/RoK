using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AbilityEntryUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    private ITooltipProvider tooltipProvider;
    private bool showingTooltip;

    public void Setup(AbilityData ability)
    {
        Setup(ability.abilityName, ability.icon, ability);
    }

    public void Setup(string displayName, Sprite entryIcon, ITooltipProvider provider)
    {
        tooltipProvider = provider;
        icon.sprite = entryIcon;
        nameText.text = displayName;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipProvider == null ||
            DraggableAbility.dragged ||
            ItemTooltip.Instance == null)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            tooltipProvider,
            icon.rectTransform,
            PlayerReference.Player,
            ItemTooltip.TooltipAnchorMode.TopRight);

        showingTooltip = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (!showingTooltip)
            return;

        showingTooltip = false;
        ItemTooltip.Instance?.Hide();
    }
}
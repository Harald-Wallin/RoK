using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltipText;

    public void SetText(string text)
    {
        if (tooltipText == text)
            return;

        tooltipText = text;

        ItemTooltip tooltip = ItemTooltip.Instance;
        RectTransform anchor = transform as RectTransform;

        if (tooltip == null || !tooltip.IsShowingFor(anchor))
            return;

        if (string.IsNullOrWhiteSpace(text))
            tooltip.Hide();
        else
            tooltip.ShowSimple(text, anchor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemTooltip tooltip = ItemTooltip.Instance;

        if (tooltip != null && !string.IsNullOrWhiteSpace(tooltipText))
            tooltip.ShowSimple(tooltipText, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideOwnTooltip();
    }

    private void OnDisable()
    {
        HideOwnTooltip();
    }

    private void HideOwnTooltip()
    {
        ItemTooltip tooltip = ItemTooltip.Instance;

        if (tooltip != null && tooltip.IsShowingFor(transform as RectTransform))
            tooltip.Hide();
    }
}
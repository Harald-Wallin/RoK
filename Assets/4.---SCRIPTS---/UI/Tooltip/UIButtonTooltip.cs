using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltipText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemTooltip.Instance.ShowSimple(tooltipText, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }
}

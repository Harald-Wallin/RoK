using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class CurrencyTooltipTarget : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField]
    private ScriptableObject currency;

    private bool hovered;

    private RectTransform Anchor =>
        transform as RectTransform;

    public static void Bind(
        Image image,
        ScriptableObject definition)
    {
        if (image == null)
            return;

        CurrencyTooltipTarget target =
            image.GetComponent<CurrencyTooltipTarget>();

        if (definition == null)
        {
            if (target != null)
                target.SetCurrency(null);

            image.raycastTarget = false;
            return;
        }

        if (target == null)
        {
            target =
                image.gameObject.AddComponent<CurrencyTooltipTarget>();
        }

        image.raycastTarget = true;
        target.SetCurrency(definition);
    }

    public void SetCurrency(
        ScriptableObject definition)
    {
        if (currency == definition)
            return;

        currency = definition;

        if (!hovered)
            return;

        if (currency != null)
        {
            CurrencyTooltipUI.Instance?.Show(
                currency,
                Anchor);
        }
        else
        {
            CurrencyTooltipUI.Instance?.HideFor(Anchor);
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        hovered = true;

        if (currency != null)
        {
            CurrencyTooltipUI.Instance?.Show(
                currency,
                Anchor);
        }
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        hovered = false;
        CurrencyTooltipUI.Instance?.HideFor(Anchor);
    }

    private void OnDisable()
    {
        hovered = false;
        CurrencyTooltipUI.Instance?.HideFor(Anchor);
    }
}

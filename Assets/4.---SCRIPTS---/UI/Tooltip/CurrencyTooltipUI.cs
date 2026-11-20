using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class CurrencyTooltipUI : MonoBehaviour
{
    public static CurrencyTooltipUI Instance
    {
        get;
        private set;
    }

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private Image rarityFrame;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private Vector2 offset = new(8f, 8f);

    private CanvasGroup group;
    private RectTransform panel;
    private RectTransform anchor;
    private ScriptableObject currency;

    private void Awake()
    {
        Instance = this;

        group = GetComponent<CanvasGroup>();
        panel = transform as RectTransform;

        foreach (Graphic graphic in
                 GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(
        ScriptableObject definition,
        RectTransform target)
    {
        if (definition == null || target == null)
        {
            Hide();
            return;
        }

        string label;
        Sprite sprite;
        Color color;

        if (definition is ItemData item)
        {
            label = item.DisplayName;
            sprite = item.icon;
            color = ItemRarityColors.GetColor(item.rarity);
        }
        else if (definition is CurrencyData coins)
        {
            label = coins.DisplayName;
            sprite = coins.Icon;
            color = Color.white;
        }
        else
        {
            Hide();
            return;
        }

        currency = definition;
        anchor = target;

        nameText.text = label;
        nameText.color = color;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
        iconImage.preserveAspect = true;

        if (rarityFrame != null)
            rarityFrame.color = color;

        transform.SetAsLastSibling();

        group.alpha = 1f;
        Position(true);
    }

    public void HideFor(RectTransform target)
    {
        if (anchor == target)
            Hide();
    }

    private void Hide()
    {
        anchor = null;
        currency = null;

        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void LateUpdate()
    {
        if (group.alpha <= 0f)
            return;

        if (currency == null ||
            anchor == null ||
            !anchor.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        foreach (CanvasGroup parent in
                 anchor.GetComponentsInParent<CanvasGroup>())
        {
            if (parent.alpha <= 0f)
            {
                Hide();
                return;
            }
        }

        Position();
    }

    private void Position(
        bool rebuildLayout = false)
    {
        RectTransform parentRect =
            panel.parent as RectTransform;

        Canvas canvas =
            GetComponentInParent<Canvas>();

        if (parentRect == null ||
            canvas == null ||
            anchor == null)
        {
            return;
        }

        if (rebuildLayout)
        {
            Canvas.ForceUpdateCanvases();

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                panel);
        }

        Camera camera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);

        Vector2 screen =
            RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[2]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screen,
            camera,
            out Vector2 point);

        Vector2 size = panel.rect.size;

        Vector2 desired =
            point +
            offset +
            Vector2.Scale(panel.pivot, size);

        Vector2 min =
            desired - Vector2.Scale(panel.pivot, size);

        Vector2 max =
            min + size;

        Rect bounds = parentRect.rect;

        if (min.x < bounds.xMin)
            desired.x += bounds.xMin - min.x;

        if (max.x > bounds.xMax)
            desired.x += bounds.xMax - max.x;

        if (min.y < bounds.yMin)
            desired.y += bounds.yMin - min.y;

        if (max.y > bounds.yMax)
            desired.y += bounds.yMax - max.y;

        panel.localPosition = new Vector3(
            desired.x,
            desired.y,
            panel.localPosition.z);
    }
}

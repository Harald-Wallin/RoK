using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    public static ItemTooltip Instance;

    public enum TooltipAnchorMode
    {
        TopRight,
        BottomLeft,
        BottomRight,
        TopLeft,
        FixedBottomLeft,
        FixedBottomRightScreen
    }

    [Header("UI")]

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI weaponDamageText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private TextMeshProUGUI usageHintText;

    [SerializeField] private GameObject typeRow;
    [SerializeField] private GameObject priceRow;

    [FormerlySerializedAs("coinIcon")]
    [SerializeField]
    private GameObject currencyIcon;

    [Header("Hover")]

    [SerializeField, Min(0f)]
    private float hoverGraceSeconds = 0.2f;

    [Header("Canvas Group")]

    [SerializeField]
    private CanvasGroup canvasGroup;

    private RectTransform rectTransform;

    private Sprite originalFooterIcon;
    private bool footerIconCaptured;

    private bool canHoverCurrency;
    private bool hideRequested;
    private float hideAt;

    private const float SimpleHorizontalPadding = 16f;

    private bool simpleWidthActive;
    private float regularWidth;
    private float regularPanelSizeDeltaX;
    private float regularTitleSizeDeltaX;

    private ContentSizeFitter tooltipSizeFitter;
    private ContentSizeFitter.FitMode regularHorizontalFit;

    private ITooltipProvider currentProvider;
    private RectTransform currentTarget;
    private CharacterStats currentCaster;
    private TooltipAnchorMode currentAnchorMode;

    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();

        typeText.richText = true;

        foreach (Graphic graphic in
                 GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }

        HideImmediate();
    }

    private void Update()
    {
        if (currentProvider != null &&
            currentAnchorMode !=
                TooltipAnchorMode.FixedBottomRightScreen &&
            (currentTarget == null ||
             !currentTarget.gameObject.activeInHierarchy))
        {
            HideImmediate();
            return;
        }

        if (hideRequested)
        {
            if (!canHoverCurrency || currentProvider == null)
            {
                HideImmediate();
            }
            else if (IsPointerInsideTooltip())
            {
                hideAt =
                    Time.unscaledTime + hoverGraceSeconds;
            }
            else if (Time.unscaledTime >= hideAt)
            {
                HideImmediate();
            }
        }

        if (canvasGroup.alpha > 0f &&
            currentProvider != null)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (currentProvider == null)
            return;

        ApplyData(
            currentProvider.GetTooltipData(currentCaster));
    }

    public void Show(
        ITooltipProvider provider,
        RectTransform target,
        CharacterStats caster,
        TooltipAnchorMode mode = TooltipAnchorMode.TopRight)
    {
        if (provider == null ||
            target == null ||
            KeepCurrentTooltip(target))
        {
            return;
        }

        hideRequested = false;
        RestoreRegularWidth();

        currentProvider = provider;
        currentTarget = target;
        currentCaster = caster;
        currentAnchorMode = mode;

        ApplyData(provider.GetTooltipData(caster));
        SetPosition(target, mode);
    }

    public void ShowFixedBottomRight(
        ITooltipProvider provider,
        CharacterStats viewer)
    {
        if (provider == null ||
            KeepCurrentTooltip(null))
        {
            return;
        }

        hideRequested = false;
        RestoreRegularWidth();

        currentProvider = provider;
        currentTarget = null;
        currentCaster = viewer;

        currentAnchorMode =
            TooltipAnchorMode.FixedBottomRightScreen;

        ApplyData(provider.GetTooltipData(viewer));
        SetFixedBottomRightPosition();
    }

    public void ShowTalent(
        TalentData data,
        int currentPoints,
        RectTransform target)
    {
        if (data == null || target == null)
            return;

        RestoreRegularWidth();
        ResetSections();

        currentProvider = null;
        currentTarget = null;
        currentCaster = null;

        TooltipData tooltip = data.GetTooltipData(
            PlayerReference.Player,
            currentPoints);

        nameText.text = tooltip.title;
        nameText.color = tooltip.titleColor;

        descriptionText.text =
            $"<color=#ffc70f>{tooltip.description}</color>";

        descriptionText.gameObject.SetActive(
            !string.IsNullOrEmpty(tooltip.description));

        statsText.text =
            string.Join("\n", tooltip.stats);

        statsText.gameObject.SetActive(
            tooltip.stats.Count > 0);

        canvasGroup.alpha = 1f;
        SetPosition(target, TooltipAnchorMode.TopRight);
    }

    private void ApplyData(TooltipData data)
    {
        if (data == null)
        {
            HideImmediate();
            return;
        }

        nameText.text = data.title;
        nameText.color = data.titleColor;

        string subtitle = string.Empty;

        if (!string.IsNullOrWhiteSpace(data.itemFavourSource))
            subtitle = data.itemFavourSource;

        if (!string.IsNullOrWhiteSpace(data.itemFavourRequirement))
        {
            subtitle +=
                (subtitle.Length > 0 ? "\n" : "") +
                data.itemFavourRequirement;
        }

        if (!string.IsNullOrEmpty(data.subtitle))
        {
            subtitle +=
                (subtitle.Length > 0 ? "\n" : "") +
                data.subtitle;
        }

        typeText.text = subtitle;
        typeText.gameObject.SetActive(subtitle.Length > 0);

        bool hasDamage =
            !string.IsNullOrWhiteSpace(data.weaponDamage);

        if (weaponDamageText != null)
        {
            weaponDamageText.text =
                data.weaponDamage ?? string.Empty;

            weaponDamageText.gameObject.SetActive(hasDamage);
        }

        if (typeRow != null)
        {
            typeRow.SetActive(
                subtitle.Length > 0 || hasDamage);
        }

        descriptionText.text =
            $"<color=#ffc70f>{data.description}</color>";

        descriptionText.gameObject.SetActive(
            !string.IsNullOrEmpty(data.description));

        string body =
            string.Join("\n", data.stats);

        if (data.requirements.Count > 0)
        {
            body +=
                (body.Length > 0 ? "\n\n" : "") +
                string.Join("\n", data.requirements);
        }

        if (data.favourContext.Count > 0)
        {
            body +=
                (body.Length > 0 ? "\n\n" : "") +
                string.Join("\n\n", data.favourContext);
        }

        statsText.text = body;
        statsText.gameObject.SetActive(body.Length > 0);

        ApplyFooter(data);

        if (usageHintText != null)
        {
            usageHintText.text =
                data.usageHint ?? string.Empty;

            usageHintText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(data.usageHint));
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = canHoverCurrency;
        canvasGroup.interactable = canHoverCurrency;

        if (currentAnchorMode ==
            TooltipAnchorMode.FixedBottomRightScreen)
        {
            SetFixedBottomRightPosition();
        }
        else if (currentTarget != null)
        {
            SetPosition(
                currentTarget,
                currentAnchorMode);
        }
    }

    private void ResetSections()
    {
        hideRequested = false;
        canHoverCurrency = false;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        typeText.gameObject.SetActive(false);

        if (weaponDamageText != null)
            weaponDamageText.gameObject.SetActive(false);

        if (typeRow != null)
            typeRow.SetActive(false);

        descriptionText.gameObject.SetActive(false);
        statsText.gameObject.SetActive(false);
        sellPriceText.gameObject.SetActive(false);

        if (currencyIcon != null)
            currencyIcon.SetActive(false);

        if (priceRow != null)
            priceRow.SetActive(false);

        if (usageHintText != null)
            usageHintText.gameObject.SetActive(false);
    }

    private void ApplyFooter(TooltipData data)
    {
        bool visible =
            data.showFooter &&
            !string.IsNullOrWhiteSpace(data.footer);

        if (priceRow != null)
            priceRow.SetActive(visible);

        sellPriceText.gameObject.SetActive(visible);

        if (visible)
            sellPriceText.text = data.footer;

        canHoverCurrency = false;

        if (currencyIcon == null)
            return;

        Image image =
            currencyIcon.GetComponent<Image>();

        if (image == null)
        {
            image =
                currencyIcon.GetComponentInChildren<Image>(true);
        }

        if (image == null)
        {
            currencyIcon.SetActive(false);
            return;
        }

        if (!footerIconCaptured)
        {
            originalFooterIcon = image.sprite;
            footerIconCaptured = true;
        }

        CurrencyData coins =
            PlayerCurrency.Instance != null
                ? PlayerCurrency.Instance.CurrencyDefinition
                : null;

        Sprite sprite = data.footerIcon != null
            ? data.footerIcon
            : coins != null && coins.Icon != null
                ? coins.Icon
                : originalFooterIcon;

        ScriptableObject definition =
            data.footerCurrencyDefinition != null
                ? data.footerCurrencyDefinition
                : coins;

        bool showIcon =
            visible &&
            data.showFooterIcon &&
            sprite != null;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.enabled = true;

        canHoverCurrency =
            showIcon && definition != null;

        CurrencyTooltipTarget.Bind(
            image,
            canHoverCurrency ? definition : null);

        currencyIcon.SetActive(showIcon);
    }

    public bool IsShowingFor(RectTransform target)
    {
        return target != null &&
               currentTarget == target;
    }

    public void Hide()
    {
        if (currentProvider == null ||
            !canHoverCurrency ||
            canvasGroup.alpha <= 0f)
        {
            HideImmediate();
            return;
        }

        hideRequested = true;

        hideAt =
            Time.unscaledTime + hoverGraceSeconds;
    }

    public void HideImmediate()
    {
        currentProvider = null;
        currentTarget = null;
        currentCaster = null;

        canvasGroup.alpha = 0f;

        ResetSections();
    }

    private bool KeepCurrentTooltip(
        RectTransform nextTarget)
    {
        return hideRequested &&
               canHoverCurrency &&
               nextTarget != currentTarget &&
               IsPointerInsideTooltip();
    }

    private bool IsPointerInsideTooltip()
    {
        Canvas canvas =
            GetComponentInParent<Canvas>();

        if (canvas == null)
            return false;

        Camera camera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            Input.mousePosition,
            camera);
    }

    private void SetFixedBottomRightPosition()
    {
        Canvas canvas =
            GetComponentInParent<Canvas>();

        if (canvas == null)
            return;

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            rectTransform);

        const float horizontalMargin = 24f;
        const float verticalMargin = 24f;

        Rect bounds = canvasRect.rect;
        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        rectTransform.anchoredPosition = new Vector2(
            bounds.xMax - horizontalMargin -
                (1f - pivot.x) * size.x,
            bounds.yMin + verticalMargin +
                pivot.y * size.y);

        ClampToScreen();
    }

    public void SetPosition(
        RectTransform target,
        TooltipAnchorMode mode)
    {
        if (target == null)
            return;

        Canvas canvas =
            GetComponentInParent<Canvas>();

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        Camera camera =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            rectTransform);

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 topRightScreen =
            RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[2]);

        Vector2 bottomLeftScreen =
            RectTransformUtility.WorldToScreenPoint(
                camera,
                corners[0]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            topRightScreen,
            camera,
            out Vector2 topRight);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            bottomLeftScreen,
            camera,
            out Vector2 bottomLeft);

        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        Vector2 bottomLeftOffset = new(
            -pivot.x * size.x,
            -pivot.y * size.y);

        Vector2 topRightOffset = new(
            (1f - pivot.x) * size.x,
            (1f - pivot.y) * size.y);

        const float padding = 6f;

        Vector2 desired;

        switch (mode)
        {
            case TooltipAnchorMode.TopRight:
                desired =
                    topRight - bottomLeftOffset +
                    new Vector2(padding, padding);
                break;

            case TooltipAnchorMode.BottomLeft:
                desired =
                    bottomLeft - topRightOffset +
                    new Vector2(-padding, -padding);
                break;

            case TooltipAnchorMode.BottomRight:
                Vector2 bottomRightScreen =
                    RectTransformUtility.WorldToScreenPoint(
                        camera,
                        corners[3]);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    bottomRightScreen,
                    camera,
                    out Vector2 bottomRight);

                desired =
                    bottomRight - bottomLeftOffset +
                    new Vector2(padding, -padding);
                break;

            case TooltipAnchorMode.TopLeft:
                Vector2 topLeftScreen =
                    RectTransformUtility.WorldToScreenPoint(
                        camera,
                        corners[1]);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    topLeftScreen,
                    camera,
                    out Vector2 topLeft);

                desired =
                    topLeft - bottomLeftOffset +
                    new Vector2(-padding, padding);
                break;

            case TooltipAnchorMode.FixedBottomLeft:
                desired =
                    bottomLeft - topRightOffset +
                    new Vector2(-padding, -padding);
                break;

            default:
                desired =
                    topRight - bottomLeftOffset +
                    new Vector2(padding, padding);
                break;
        }

        rectTransform.anchoredPosition = desired;
        ClampToScreen();
    }

    private void ClampToScreen()
    {
        RectTransform canvasRect =
            GetComponentInParent<Canvas>().transform
                as RectTransform;

        Vector2 position = rectTransform.anchoredPosition;
        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        Vector2 min = position + new Vector2(
            -pivot.x * size.x,
            -pivot.y * size.y);

        Vector2 max = min + size;
        Rect bounds = canvasRect.rect;

        Vector2 correction = Vector2.zero;

        if (min.x < bounds.xMin)
            correction.x = bounds.xMin - min.x;

        if (max.x > bounds.xMax)
            correction.x = bounds.xMax - max.x;

        if (min.y < bounds.yMin)
            correction.y = bounds.yMin - min.y;

        if (max.y > bounds.yMax)
            correction.y = bounds.yMax - max.y;

        rectTransform.anchoredPosition =
            position + correction;
    }

    public void ShowSimple(
        string text,
        RectTransform target)
    {
        currentProvider = null;
        currentTarget = target;
        currentCaster = null;

        currentAnchorMode =
            TooltipAnchorMode.TopRight;

        ResetSections();

        nameText.color = Color.white;
        nameText.text = text;

        FitSimpleWidth(text);

        canvasGroup.alpha = 1f;
        SetPosition(target, currentAnchorMode);
    }

    private void FitSimpleWidth(string text)
    {
        RectTransform titleRect =
            nameText.rectTransform;

        if (!simpleWidthActive)
        {
            simpleWidthActive = true;
            regularWidth = rectTransform.rect.width;

            regularPanelSizeDeltaX =
                rectTransform.sizeDelta.x;

            regularTitleSizeDeltaX =
                titleRect.sizeDelta.x;

            if (TryGetComponent(out tooltipSizeFitter))
            {
                regularHorizontalFit =
                    tooltipSizeFitter.horizontalFit;

                tooltipSizeFitter.horizontalFit =
                    ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        float padding = SimpleHorizontalPadding;

        if (TryGetComponent(out LayoutGroup layoutGroup))
        {
            padding = Mathf.Max(
                padding,
                layoutGroup.padding.horizontal);
        }

        float preferredWidth =
            nameText.GetPreferredValues(text).x;

        float width =
            Mathf.Ceil(preferredWidth + padding);

        if (regularWidth > 0f)
            width = Mathf.Min(width, regularWidth);

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width);

        if (Mathf.Approximately(
                titleRect.anchorMin.x,
                titleRect.anchorMax.x))
        {
            titleRect.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                Mathf.Max(1f, width - padding));
        }
    }

    private void RestoreRegularWidth()
    {
        if (!simpleWidthActive)
            return;

        simpleWidthActive = false;

        rectTransform.sizeDelta = new Vector2(
            regularPanelSizeDeltaX,
            rectTransform.sizeDelta.y);

        RectTransform titleRect =
            nameText.rectTransform;

        titleRect.sizeDelta = new Vector2(
            regularTitleSizeDeltaX,
            titleRect.sizeDelta.y);

        if (tooltipSizeFitter != null)
        {
            tooltipSizeFitter.horizontalFit =
                regularHorizontalFit;
        }
    }
}
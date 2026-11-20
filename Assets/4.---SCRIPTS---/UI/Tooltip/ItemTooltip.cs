using TMPro;
using UnityEngine;
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
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private GameObject coinIcon;

    [SerializeField]
    private TextMeshProUGUI usageHintText;

    [Header("Canvas Group")]

    [SerializeField]
    private CanvasGroup canvasGroup;

    private RectTransform rectTransform;

    private Sprite originalFooterIcon;
    private bool footerIconCaptured;

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

        Hide();
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
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
        if (provider == null || target == null)
            return;

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
        if (provider == null)
            return;

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

        canvasGroup.blocksRaycasts = false;

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
            Hide();
            return;
        }

        ResetSections();

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

        if (!string.IsNullOrEmpty(data.description))
        {
            descriptionText.text =
                $"<color=#ffc70f>{data.description}</color>";

            descriptionText.gameObject.SetActive(true);
        }

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
        typeText.gameObject.SetActive(false);
        descriptionText.gameObject.SetActive(false);
        statsText.gameObject.SetActive(false);
        sellPriceText.gameObject.SetActive(false);

        if (coinIcon != null)
            coinIcon.SetActive(false);

        if (usageHintText != null)
            usageHintText.gameObject.SetActive(false);
    }

    private void ApplyFooter(TooltipData data)
    {
        bool visible =
            data.showFooter &&
            !string.IsNullOrWhiteSpace(data.footer);

        sellPriceText.gameObject.SetActive(visible);

        if (visible)
            sellPriceText.text = data.footer;

        if (coinIcon == null)
            return;

        Image image = coinIcon.GetComponent<Image>();

        if (image == null)
            image = coinIcon.GetComponentInChildren<Image>(true);

        if (image == null)
        {
            coinIcon.SetActive(false);
            return;
        }

        if (!footerIconCaptured)
        {
            originalFooterIcon = image.sprite;
            footerIconCaptured = true;
        }

        Sprite coins =
            PlayerCurrency.Instance != null &&
            PlayerCurrency.Instance.CurrencyDefinition != null
                ? PlayerCurrency.Instance.CurrencyDefinition.Icon
                : null;

        Sprite sprite = data.footerIcon != null
            ? data.footerIcon
            : coins != null
                ? coins
                : originalFooterIcon;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = true;

        coinIcon.SetActive(
            visible &&
            data.showFooterIcon &&
            sprite != null);
    }

    public bool IsShowingFor(RectTransform target)
    {
        return target != null &&
               currentTarget == target;
    }

    public void Hide()
    {
        currentProvider = null;
        currentTarget = null;
        currentCaster = null;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void SetFixedBottomRightPosition()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

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

        Rect canvasBounds = canvasRect.rect;
        Vector2 tooltipSize = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        float rightEdge =
            canvasBounds.xMax - horizontalMargin;

        float bottomEdge =
            canvasBounds.yMin + verticalMargin;

        rectTransform.anchoredPosition = new Vector2(
            rightEdge - (1f - pivot.x) * tooltipSize.x,
            bottomEdge + pivot.y * tooltipSize.y);

        ClampToScreen();
    }

    public void SetPosition(
        RectTransform target,
        TooltipAnchorMode mode)
    {
        if (target == null)
            return;

        Canvas canvas = GetComponentInParent<Canvas>();

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        Camera cam =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            rectTransform);

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 targetTopRightScreen =
            RectTransformUtility.WorldToScreenPoint(
                cam,
                corners[2]);

        Vector2 targetBottomLeftScreen =
            RectTransformUtility.WorldToScreenPoint(
                cam,
                corners[0]);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetTopRightScreen,
            cam,
            out Vector2 targetTopRightLocal);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetBottomLeftScreen,
            cam,
            out Vector2 targetBottomLeftLocal);

        Vector2 tooltipSize = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        Vector2 bottomLeftOffset = new(
            -pivot.x * tooltipSize.x,
            -pivot.y * tooltipSize.y);

        Vector2 topRightOffset = new(
            (1f - pivot.x) * tooltipSize.x,
            (1f - pivot.y) * tooltipSize.y);

        Vector2 desiredAnchoredPos;
        float padding = 6f;

        switch (mode)
        {
            case TooltipAnchorMode.TopRight:
                desiredAnchoredPos =
                    targetTopRightLocal -
                    bottomLeftOffset +
                    new Vector2(padding, padding);
                break;

            case TooltipAnchorMode.BottomLeft:
                desiredAnchoredPos =
                    targetBottomLeftLocal -
                    topRightOffset +
                    new Vector2(-padding, -padding);
                break;

            case TooltipAnchorMode.BottomRight:
                Vector2 targetBottomRightScreen =
                    RectTransformUtility.WorldToScreenPoint(
                        cam,
                        corners[3]);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    targetBottomRightScreen,
                    cam,
                    out Vector2 targetBottomRightLocal);

                desiredAnchoredPos =
                    targetBottomRightLocal -
                    bottomLeftOffset +
                    new Vector2(padding, -padding);
                break;

            case TooltipAnchorMode.TopLeft:
                Vector2 targetTopLeftScreen =
                    RectTransformUtility.WorldToScreenPoint(
                        cam,
                        corners[1]);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    targetTopLeftScreen,
                    cam,
                    out Vector2 targetTopLeftLocal);

                desiredAnchoredPos =
                    targetTopLeftLocal -
                    bottomLeftOffset +
                    new Vector2(-padding, padding);
                break;

            case TooltipAnchorMode.FixedBottomLeft:
                Vector2 targetBLScreen =
                    RectTransformUtility.WorldToScreenPoint(
                        cam,
                        corners[0]);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    targetBLScreen,
                    cam,
                    out Vector2 targetBLLocal);

                desiredAnchoredPos =
                    targetBLLocal -
                    topRightOffset +
                    new Vector2(-padding, -padding);
                break;

            default:
                desiredAnchoredPos =
                    targetTopRightLocal -
                    bottomLeftOffset +
                    new Vector2(padding, padding);
                break;
        }

        rectTransform.anchoredPosition =
            desiredAnchoredPos;

        ClampToScreen();
    }

    private void ClampToScreen()
    {
        RectTransform canvasRect =
            GetComponentInParent<Canvas>().transform
                as RectTransform;

        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        Vector2 tooltipMin = pos + new Vector2(
            -pivot.x * size.x,
            -pivot.y * size.y);

        Vector2 tooltipMax =
            tooltipMin + size;

        Rect canvasR = canvasRect.rect;
        Vector2 correction = Vector2.zero;

        if (tooltipMin.x < canvasR.xMin)
            correction.x = canvasR.xMin - tooltipMin.x;

        if (tooltipMax.x > canvasR.xMax)
            correction.x = canvasR.xMax - tooltipMax.x;

        if (tooltipMin.y < canvasR.yMin)
            correction.y = canvasR.yMin - tooltipMin.y;

        if (tooltipMax.y > canvasR.yMax)
            correction.y = canvasR.yMax - tooltipMax.y;

        rectTransform.anchoredPosition =
            pos + correction;
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

            regularWidth =
                rectTransform.rect.width;

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
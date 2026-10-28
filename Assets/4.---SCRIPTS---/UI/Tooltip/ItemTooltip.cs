using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ItemData;
using static UnityEngine.EventSystems.EventTrigger;

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


    [Header("Canvas Group")]
    [SerializeField] private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private ITooltipProvider currentProvider;
    private RectTransform currentTarget;
    private CharacterStats currentCaster;
    private TooltipAnchorMode currentAnchorMode;
    private PlayerStats cachedPlayer;

    private void Awake()
    {
        Instance = this;    
        rectTransform = GetComponent<RectTransform>();
        cachedPlayer = PlayerReference.Player;
        Hide();
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (canvasGroup.alpha > 0f && currentProvider != null)
        {
            Refresh();
        };
    }

    public void Refresh()
    {
        if (currentProvider == null) return;

        TooltipData data = currentProvider.GetTooltipData(currentCaster);
        ApplyData(data);

    }

    public void Show(
    ITooltipProvider provider,
    RectTransform target,
    CharacterStats caster,
    TooltipAnchorMode mode = TooltipAnchorMode.TopRight
)
    {
        if (provider == null || target == null) return;

        currentProvider = provider;

        currentTarget = target;
        currentCaster = caster;
        currentAnchorMode = mode;

        TooltipData data = provider.GetTooltipData(caster);
        ApplyData(data);

        SetPosition(target, mode);
    }

    public void ShowFixedBottomRight(
    ITooltipProvider provider,
    CharacterStats viewer)
    {
        if (provider == null)
            return;

        currentProvider =
            provider;

        currentTarget =
            null;

        currentCaster =
            viewer;

        currentAnchorMode =
            TooltipAnchorMode
                .FixedBottomRightScreen;

        TooltipData data =
            provider.GetTooltipData(
                viewer
            );

        ApplyData(
            data
        );

        SetFixedBottomRightPosition();
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

        LayoutRebuilder
            .ForceRebuildLayoutImmediate(
                rectTransform
            );

        const float horizontalMargin = 24f;
        const float verticalMargin = 24f;

        /*
         * Positioneringen beräknas i canvasens lokala
         * koordinatsystem och fungerar oberoende av tooltipens pivot.
         */

        Rect canvasBounds =
            canvasRect.rect;

        Vector2 tooltipSize =
            rectTransform.rect.size;

        Vector2 pivot =
            rectTransform.pivot;

        float rightEdge =
            canvasBounds.xMax -
            horizontalMargin;

        float bottomEdge =
            canvasBounds.yMin +
            verticalMargin;

        rectTransform.anchoredPosition =
            new Vector2(
                rightEdge -
                (1f - pivot.x) *
                tooltipSize.x,

                bottomEdge +
                pivot.y *
                tooltipSize.y
            );

        ClampToScreen();
    }

    public void ShowTalent(TalentData data, int currentPoints, RectTransform target)
    {

        canvasGroup.blocksRaycasts = false;
        var player = PlayerReference.Player;
        currentProvider = null;

        currentTarget = null;
        currentCaster = null;

        descriptionText.gameObject.SetActive(true);

        TooltipData tooltip = data.GetTooltipData(player, currentPoints);

        nameText.text = tooltip.title;
        nameText.color = tooltip.titleColor;

        typeText.text = "";

        descriptionText.gameObject.SetActive(
            !string.IsNullOrEmpty(tooltip.description)
        );
        descriptionText.text = $"<color=#ffc70f>{tooltip.description}</color>";

        statsText.text = "";
        foreach (var line in tooltip.stats)
        {
            statsText.text += line + "\n";
        }

        statsText.gameObject.SetActive(tooltip.stats.Count > 0);
        sellPriceText.gameObject.SetActive(false);

        if (coinIcon != null)
            coinIcon.SetActive(false);

        canvasGroup.alpha = 1f;
        SetPosition(target, TooltipAnchorMode.TopRight);
    }

    void ApplyData(TooltipData data)
    {
        // RESET
        nameText.color = Color.white;

        typeText.gameObject.SetActive(false);
        descriptionText.gameObject.SetActive(false);
        statsText.gameObject.SetActive(false);
        sellPriceText.gameObject.SetActive(false);

        if (coinIcon != null)
            coinIcon.SetActive(false);

        // TITLE
        nameText.text = data.title;
        nameText.color = data.titleColor;

        // ITEM FAVOUR SOURCE + SUBTITLE
        bool hasItemFavourSource =
            !string.IsNullOrWhiteSpace(
                data.itemFavourSource
            );

        bool hasItemFavourRequirement =
            !string.IsNullOrWhiteSpace(
                data.itemFavourRequirement
            );

        bool hasSubtitle =
            !string.IsNullOrEmpty(
                data.subtitle
            );

        if (hasItemFavourSource ||
            hasItemFavourRequirement ||
            hasSubtitle)
        {
            typeText.gameObject.SetActive(
                true
            );

            typeText.text =
                "";

            if (hasItemFavourSource)
            {
                typeText.text +=
                    data.itemFavourSource;
            }

            if (hasItemFavourRequirement)
            {
                if (!string.IsNullOrEmpty(
                        typeText.text))
                {
                    typeText.text +=
                        "\n";
                }

                typeText.text +=
                    data.itemFavourRequirement;
            }

            if (hasSubtitle)
            {
                if (!string.IsNullOrEmpty(
                        typeText.text))
                {
                    typeText.text +=
                        "\n";
                }

                typeText.text +=
                    data.subtitle;
            }
        }

        // DESCRIPTION
        if (!string.IsNullOrEmpty(data.description))
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.text =
            $"<color=#ffc70f>{data.description}</color>";
        }

        // STATS + REQUIREMENTS + FAVOUR CONTEXT
        bool hasStats =
            data.stats.Count > 0;

        bool hasRequirements =
            data.requirements.Count > 0;

        bool hasFavourContext =
            data.favourContext.Count > 0;

        if (hasStats ||
            hasRequirements ||
            hasFavourContext)
        {
            statsText.gameObject.SetActive(
                true
            );

            statsText.text =
                "";

            foreach (string line
                     in data.stats)
            {
                statsText.text +=
                    line + "\n";
            }

            if (hasRequirements)
            {
                if (hasStats)
                {
                    statsText.text +=
                        "\n";
                }

                foreach (string requirement
                         in data.requirements)
                {
                    statsText.text +=
                        requirement + "\n";
                }
            }

            if (hasFavourContext)
            {
                if (hasStats ||
                    hasRequirements)
                {
                    statsText.text +=
                        "\n";
                }

                for (int i = 0;
                     i < data.favourContext.Count;
                     i++)
                {
                    statsText.text +=
                        data.favourContext[i];

                    if (i <
                        data.favourContext.Count - 1)
                    {
                        statsText.text +=
                            "\n\n";
                    }
                }
            }
        }

        // FOOTER
        bool showFooter =
            data.showFooter &&
            !string.IsNullOrEmpty(data.footer);

        if (showFooter)
        {
            sellPriceText.gameObject.SetActive(true);

            if (coinIcon != null)
                coinIcon.SetActive(true);

            sellPriceText.text = data.footer;
        }

        canvasGroup.alpha = 1f;

        //
        if (currentAnchorMode ==TooltipAnchorMode
        .FixedBottomRightScreen)
        {
            SetFixedBottomRightPosition();
        }
        else if (currentTarget != null)
        {
            SetPosition(
                currentTarget,
                currentAnchorMode
            );
        }
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


    public void SetPosition(RectTransform target, TooltipAnchorMode mode)
    {
        if (target == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // Force layout update så rectTransform size är uppdaterad
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // Hämta target world-corners och konvertera till canvas-lokal punkt
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // corners: 0 = bottom-left, 1 = top-left, 2 = top-right, 3 = bottom-right (Unity)
        Vector2 targetTopRightScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        Vector2 targetBottomLeftScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);

        Vector2 targetTopRightLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetTopRightScreen, cam, out targetTopRightLocal);

        Vector2 targetBottomLeftLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetBottomLeftScreen, cam, out targetBottomLeftLocal);

        // Tooltip size i canvas-lokala enheter (efter layout rebuild)
        Vector2 tooltipSize = rectTransform.rect.size;

        // Pivot/ankare: vi vill anpassa tooltip så att dess nedre-vänstra hörn ligger i targetTopRightLocal
        // Nedre-vänstra hörnets offset från rectTransform.anchoredPosition beror på pivot.
        // anchoredPosition = center-pos beroende på anchors; enklast: räkna corner-offset = (pivot - 0.0f) * size
        Vector2 pivot = rectTransform.pivot; // (0,0) = bottom-left, (1,1) = top-right
        Vector2 bottomLeftOffset = new Vector2(-pivot.x * tooltipSize.x, -pivot.y * tooltipSize.y);
        Vector2 topRightOffset = new Vector2((1f - pivot.x) * tooltipSize.x, (1f - pivot.y) * tooltipSize.y);
        Vector2 desiredAnchoredPos;

        float padding = 6f; // justera efter behov (canvas-lokala enheter)

        switch (mode)
        {
            case TooltipAnchorMode.TopRight:
                // placera tooltip så att dess bottom-left = target top-right + padding
                desiredAnchoredPos = targetTopRightLocal - bottomLeftOffset + new Vector2(padding, padding);
                break;

            case TooltipAnchorMode.BottomLeft:
                // placera tooltip så att dess top-right = target bottom-left - padding
                // top-right offset från anchoredPosition = (1 - pivot) * size
                desiredAnchoredPos = targetBottomLeftLocal - topRightOffset + new Vector2(-padding, -padding);
                break;

            case TooltipAnchorMode.BottomRight:
                // bottom-left of tooltip = bottom-right of target
                Vector2 targetBottomRightScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[3]);
                Vector2 targetBottomRightLocal;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetBottomRightScreen, cam, out targetBottomRightLocal);
                desiredAnchoredPos = targetBottomRightLocal - bottomLeftOffset + new Vector2(padding, -padding);
                break;

            case TooltipAnchorMode.TopLeft:
                // bottom-left of tooltip = top-left of target
                Vector2 targetTopLeftScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
                Vector2 targetTopLeftLocal;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetTopLeftScreen, cam, out targetTopLeftLocal);
                desiredAnchoredPos = targetTopLeftLocal - bottomLeftOffset + new Vector2(-padding, padding);
                break;

            case TooltipAnchorMode.FixedBottomLeft:
                // Hämta target bottom-left (world -> canvas-local)
                Vector2 targetBLScreen = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); // corners[0] = bottom-left
                Vector2 targetBLLocal;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetBLScreen, cam, out targetBLLocal);

                // tooltip storlek & pivot (redan beräknade före switch)
                // topRightOffset = vektor från anchoredPosition till tooltipens top-right
                // (använd topRightOffset som du deklarerat ovan)
                // Vi vill att tooltip.topRight = target.bottomLeft + small negative padding

                // Om du vill lägga ett avstånd utanför target, använd padding positivt/negativt som nedan
                Vector2 pad = new Vector2(-padding, -padding);

                // Sätt anchoredPosition så att tooltipens top-right hamnar på target bottom-left
                desiredAnchoredPos = targetBLLocal - topRightOffset + pad;
                break;






            default:
                desiredAnchoredPos = targetTopRightLocal - bottomLeftOffset + new Vector2(padding, padding);
                break;
        }

        rectTransform.anchoredPosition = desiredAnchoredPos;

        //Debug.Log($"Tooltip SetPosition -> anchoredPos {rectTransform.anchoredPosition}, mode {mode}, pivot {pivot}, tooltipSize {tooltipSize}");

        ClampToScreen();
    }

    private void ClampToScreen()
    {
        RectTransform canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;

        // tooltip anchored position och storlek
        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 size = rectTransform.rect.size;
        Vector2 pivot = rectTransform.pivot;

        // beräkna min/max i canvas-lokala koordinater för tooltipens anchoredPosition
        // vi behandlar anchoredPosition som tooltipens lokal-centrum beroende på anchors; enklast: räkna tooltipens min och max hörn i canvas-local:
        Vector2 tooltipMin = pos + (new Vector2(-pivot.x * size.x, -pivot.y * size.y));
        Vector2 tooltipMax = tooltipMin + size;

        Rect canvasR = canvasRect.rect;

        Vector2 correction = Vector2.zero;

        if (tooltipMin.x < canvasR.xMin) correction.x = canvasR.xMin - tooltipMin.x;
        if (tooltipMax.x > canvasR.xMax) correction.x = canvasR.xMax - tooltipMax.x;
        if (tooltipMin.y < canvasR.yMin) correction.y = canvasR.yMin - tooltipMin.y;
        if (tooltipMax.y > canvasR.yMax) correction.y = canvasR.yMax - tooltipMax.y;

        rectTransform.anchoredPosition = pos + correction;

        //Debug.Log($"ClampToScreen -> correction {correction}, newAnchored {rectTransform.anchoredPosition}");
    }


    public void ShowSimple(string text, RectTransform target)
    {
        currentProvider = null;
        currentTarget = target;
        currentCaster = null;
        currentAnchorMode = TooltipAnchorMode.TopRight;

        // RESET
        nameText.color = Color.white;

        typeText.gameObject.SetActive(false);
        descriptionText.gameObject.SetActive(false);
        statsText.gameObject.SetActive(false);
        sellPriceText.gameObject.SetActive(false);

        if (coinIcon != null)
            coinIcon.SetActive(false);

        // CONTENT
        nameText.text = text;

        // SHOW
        canvasGroup.alpha = 1f;

        SetPosition(target, currentAnchorMode);
    }

}

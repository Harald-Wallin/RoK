using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class FavourRewardEntryUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Visuals")]

    [SerializeField]
    private Image selectionFrame;

    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text stackText;

    [Header("Interaction")]

    [SerializeField]
    private Button button;

    [SerializeField]
    private RectTransform tooltipTarget;

    [SerializeField]
    private ItemTooltip.TooltipAnchorMode
        tooltipAnchorMode =
            ItemTooltip.TooltipAnchorMode.TopRight;

    [Header("Selection Frame")]

    [SerializeField]
    [Range(0f, 1f)]
    private float unselectedFrameAlpha = 0.1f;

    [SerializeField]
    [Range(0f, 1f)]
    private float selectedFrameAlpha = 1f;

    private FavourRewardChoiceOptionRuntime
        choiceOption;

    private ITooltipProvider tooltipProvider;

    private RectTransform CachedTooltipTarget =>
        tooltipTarget != null
            ? tooltipTarget
            : transform as RectTransform;

    // =========================================================
    // FIXED REWARDS
    // =========================================================

    public void BindFixedItem(
        FavourItemReward reward)
    {
        ClearBindings();

        if (reward == null ||
            reward.Item == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        tooltipProvider =
            reward.Item as ITooltipProvider;

        SetIcon(
            reward.Item.icon
        );

        SetStackAmount(
            reward.Amount
        );

        /*
         * Fixed rewards är inte valbara.
         * Selection frame ska därför inte bara vara transparent,
         * utan helt avstängd.
         */
        SetSelectionFrameVisible(
            false
        );

        SetFixedRewardInteraction();
    }

    public void BindFixedAbility(
        FavourAbilityReward reward)
    {
        ClearBindings();

        if (reward == null ||
            reward.Ability == null)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        tooltipProvider =
            reward.Ability as ITooltipProvider;

        SetIcon(
            reward.Ability.icon
        );

        /*
         * Abilities har ingen stackstorlek.
         */
        SetStackAmount(
            1
        );

        SetSelectionFrameVisible(
            false
        );

        SetFixedRewardInteraction();
    }

    public void BindFixedCurrency(
    CurrencyData currency,
    int amount)
    {
        ClearBindings();

        if (currency == null ||
            amount <= 0)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        tooltipProvider =
            currency;

        SetIcon(
            currency.Icon
        );

        SetStackAmount(
            amount
        );

        SetSelectionFrameVisible(
            false
        );

        SetFixedRewardInteraction();
    }

    // =========================================================
    // CHOICE REWARDS
    // =========================================================

    public void BindChoice(
        FavourRewardChoiceOptionRuntime option)
    {
        ClearBindings();

        choiceOption = option;

        if (choiceOption == null ||
            !choiceOption.IsValid)
        {
            gameObject.SetActive(
                false
            );

            return;
        }

        gameObject.SetActive(
            true
        );

        tooltipProvider =
            ResolveChoiceTooltipProvider(
                choiceOption
            );

        SetIcon(
            choiceOption.Icon
        );

        if (choiceOption.Type ==
            FavourRewardChoiceType.Item)
        {
            SetStackAmount(
                choiceOption.ItemAmount
            );
        }
        else
        {
            SetStackAmount(
                1
            );
        }

        SetSelectionFrameVisible(
            true
        );

        if (button != null)
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                HandleChoiceClicked
            );
        }

        RefreshChoiceVisual();
    }

    private static ITooltipProvider
        ResolveChoiceTooltipProvider(
            FavourRewardChoiceOptionRuntime option)
    {
        if (option == null)
            return null;

        switch (option.Type)
        {
            case FavourRewardChoiceType.Item:
                return option.Item as
                    ITooltipProvider;

            case FavourRewardChoiceType.Ability:
                return option.Ability as
                    ITooltipProvider;

            default:
                return null;
        }
    }

    public void RefreshChoiceVisual()
    {
        if (choiceOption == null)
            return;

        SetSelectionFrameVisible(
            true
        );

        SetSelectionFrameSelected(
            choiceOption.IsSelected
        );

        if (button != null)
        {
            /*
             * Ett valt alternativ måste kunna klickas för
             * att avmarkeras.
             *
             * Ett annat alternativ i en "Choose 1"-grupp
             * förblir klickbart eftersom runtime nu ersätter
             * det gamla valet automatiskt.
             */
            button.interactable =
                choiceOption.IsSelected
                    ? choiceOption.CanDeselect
                    : choiceOption.CanSelect;
        }
    }

    private void HandleChoiceClicked()
    {
        if (choiceOption == null)
            return;

        choiceOption.Toggle();

        /*
         * Runtime-eventet uppdaterar samtliga entries i gruppen.
         * Den lokala uppdateringen ger dessutom omedelbar respons.
         */
        RefreshChoiceVisual();
    }

    // =========================================================
    // TOOLTIP
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (tooltipProvider == null ||
            ItemTooltip.Instance == null)
        {
            return;
        }

        RectTransform target =
            CachedTooltipTarget;

        if (target == null)
            return;

        ItemTooltip.Instance.Show(
            tooltipProvider,
            target,
            PlayerReference.Player,
            tooltipAnchorMode
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (ItemTooltip.Instance != null)
        {
            ItemTooltip.Instance.Hide();
        }
    }

    // =========================================================
    // VISUAL HELPERS
    // =========================================================

    private void SetIcon(
        Sprite sprite)
    {
        if (iconImage == null)
            return;

        iconImage.sprite =
            sprite;

        iconImage.enabled =
            sprite != null;
    }

    private void SetStackAmount(
        int amount)
    {
        if (stackText == null)
            return;

        bool showStack =
            amount > 1;

        stackText.gameObject.SetActive(
            showStack
        );

        stackText.text =
            showStack
                ? amount.ToString()
                : string.Empty;
    }

    private void SetSelectionFrameVisible(
        bool visible)
    {
        if (selectionFrame == null)
            return;

        selectionFrame.gameObject.SetActive(
            visible
        );
    }

    private void SetSelectionFrameSelected(
        bool selected)
    {
        if (selectionFrame == null)
            return;

        Color color =
            selectionFrame.color;

        color.a =
            selected
                ? selectedFrameAlpha
                : unselectedFrameAlpha;

        selectionFrame.color =
            color;
    }

    // =========================================================
    // INTERACTION HELPERS
    // =========================================================

    private void SetFixedRewardInteraction()
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();

        /*
         * Button-komponenten behålls för raycasting/hover,
         * men fixed rewards går inte att klicka.
         */
        button.interactable =
            false;
    }

    private void ClearBindings()
    {
        choiceOption = null;
        tooltipProvider = null;

        HideTooltip();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
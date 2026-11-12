using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class WeaponTrainingRowUI : MonoBehaviour,
    ITooltipProvider, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image abilityIcon;
    [SerializeField] private TMP_Text abilityName;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject unavailableTint;

    private Button button;
    private PlayerStats player;
    private Action<AbilityData> onTrain;
    private WeaponTrainer.TrainingState state;
    private bool hovering;

    public WeaponTrainer.TrainingOffer Offer { get; private set; }

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    public void Bind(
        WeaponTrainer.TrainingOffer offer,
        PlayerStats viewer,
        Action<AbilityData> train)
    {
        Offer = offer;
        player = viewer;
        onTrain = train;

        abilityIcon.sprite = offer.Ability.icon;
        abilityIcon.enabled = abilityIcon.sprite != null;
        abilityName.text = offer.Ability.DisplayName;
        priceText.text = offer.Price == 0 ? "Free" : $"{offer.Price}";
    }

    public void SetState(WeaponTrainer.TrainingState newState)
    {
        state = newState;

        bool available = state == WeaponTrainer.TrainingState.Available;
        button.interactable = available;
        unavailableTint.SetActive(!available);

        if (hovering)
            ItemTooltip.Instance?.Refresh();
    }

    public TooltipData GetTooltipData(CharacterStats viewer = null)
    {
        if (Offer == null)
            return new TooltipData();

        TooltipData tooltip = Offer.Ability.GetTooltipData(viewer ?? player);

        string reason = state switch
        {
            WeaponTrainer.TrainingState.RequiresDeed => "Complete the required Deed.",
            WeaponTrainer.TrainingState.NotEnoughCoins => "Not enough coins.",
            WeaponTrainer.TrainingState.Learned => "Already learned.",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(reason))
            tooltip.requirements.Add(reason);

        return tooltip;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Offer == null || ItemTooltip.Instance == null)
            return;

        hovering = true;
        ItemTooltip.Instance.Show(this, abilityIcon.rectTransform, player);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void HandleClick()
    {
        if (Offer != null)
            onTrain?.Invoke(Offer.Ability);
    }

    private void HideTooltip()
    {
        if (!hovering)
            return;

        hovering = false;
        ItemTooltip.Instance?.Hide();
    }
}

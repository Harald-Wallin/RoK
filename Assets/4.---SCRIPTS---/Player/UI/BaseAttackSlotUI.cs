using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BaseAttackSlotUI :
    MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    ICombatSlot
{
    [Header("Slot")]

    [SerializeField]
    [Range(
        0,
        PlayerAbilityCollection
            .BaseAttackSlotCount - 1)]
    private int slotIndex;

    [Header("Visuals")]

    [SerializeField]
    private Image icon;

    [SerializeField]
    private Image cooldownOverlay;

    [SerializeField]
    private TMP_Text cooldownText;

    [SerializeField]
    private Image activeIndicator;

    private PlayerAbilityCollection
        collection;

    private CharacterActionController
        actionController;

    private AbilityData attack;

    public int SlotIndex =>
        slotIndex;

    public AbilityData Attack =>
        attack;

    public void Initialize(
        PlayerAbilityCollection abilityCollection,
        CharacterActionController controller,
        int index)
    {
        Unsubscribe();

        collection =
            abilityCollection;

        actionController =
            controller;

        slotIndex =
            Mathf.Clamp(
                index,
                0,
                PlayerAbilityCollection
                    .BaseAttackSlotCount - 1
            );

        Subscribe();
        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();

        ItemTooltip.Instance?.Hide();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        UpdateCooldown();
    }

    private void Subscribe()
    {
        if (collection == null)
            return;

        collection.OnBaseAttackSlotChanged -=
            HandleBaseAttackSlotChanged;

        collection.OnActiveBaseAttackChanged -=
            HandleActiveBaseAttackChanged;

        collection.OnBaseAttackSlotChanged +=
            HandleBaseAttackSlotChanged;

        collection.OnActiveBaseAttackChanged +=
            HandleActiveBaseAttackChanged;
    }

    private void Unsubscribe()
    {
        if (collection == null)
            return;

        collection.OnBaseAttackSlotChanged -=
            HandleBaseAttackSlotChanged;

        collection.OnActiveBaseAttackChanged -=
            HandleActiveBaseAttackChanged;
    }

    private void HandleBaseAttackSlotChanged(
        int changedSlotIndex,
        AbilityData changedAttack)
    {
        if (changedSlotIndex !=
            slotIndex)
        {
            return;
        }

        Refresh();
    }

    private void HandleActiveBaseAttackChanged(
        AbilityData activeAttack)
    {
        RefreshActiveIndicator();
    }

    private void UpdateCooldown()
    {
        if (attack == null ||
            actionController == null)
        {
            ClearCooldown();

            return;
        }

        float remaining =
            actionController
                .GetCooldownRemaining(
                    attack
                );

        float maximum =
            actionController
                .GetMaxCooldown(
                    attack
                );

        if (remaining <= 0f ||
            maximum <= 0f)
        {
            ClearCooldown();

            return;
        }

        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount =
                Mathf.Clamp01(
                    remaining /
                    maximum
                );
        }

        if (cooldownText != null)
        {
            cooldownText.text =
                Mathf.CeilToInt(
                    remaining
                ).ToString();
        }
    }

    private void ClearCooldown()
    {
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount =
                0f;
        }

        if (cooldownText != null)
        {
            cooldownText.text =
                string.Empty;
        }
    }

    public void OnDrop(
        PointerEventData eventData)
    {
        if (collection == null)
            return;

        DraggableAbility dragged =
            eventData.pointerDrag
                ?.GetComponent<
                    DraggableAbility>();

        if (dragged == null ||
            dragged.ability == null ||
            !dragged.ability.IsBaseAttack)
        {
            return;
        }

        bool equipped =
            collection
                .EquipBaseAttack(
                    slotIndex,
                    dragged.ability
                );

        if (!equipped)
            return;

        dragged.wasDroppedOnSlot =
            true;
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (eventData.button !=
            PointerEventData
                .InputButton.Left)
        {
            return;
        }

        if (collection == null)
            return;

        if (slotIndex ==
            PlayerAbilityCollection
                .PrimaryBaseAttackSlotIndex)
        {
            return;
        }

        collection
            .SwapBaseAttacks();
    }

    private void Refresh()
    {
        attack =
            collection != null
                ? collection
                    .GetBaseAttack(
                        slotIndex
                    )
                : null;

        RefreshIcon();
        RefreshActiveIndicator();
        ClearCooldown();
    }

    private void RefreshIcon()
    {
        if (icon == null)
            return;

        DraggableAbility draggable =
            icon.GetComponent<
                DraggableAbility>();

        if (attack == null)
        {
            icon.sprite =
                null;

            icon.enabled =
                false;

            if (draggable != null)
            {
                draggable.ability =
                    null;
            }

            return;
        }

        icon.sprite =
            attack.icon;

        icon.enabled =
            true;

        icon.color =
            Color.white;

        if (draggable != null)
        {
            draggable.ability =
                attack;
        }
    }

    private void RefreshActiveIndicator()
    {
        if (activeIndicator == null)
            return;

        activeIndicator.enabled =
            slotIndex ==
            PlayerAbilityCollection
                .PrimaryBaseAttackSlotIndex;
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (attack == null ||
            DraggableAbility.dragged ||
            ItemTooltip.Instance == null ||
            icon == null)
        {
            return;
        }

        ItemTooltip.Instance.Show(
            attack,
            icon.rectTransform,
            PlayerReference.Player,
            ItemTooltip
                .TooltipAnchorMode
                .TopRight
        );
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        ItemTooltip.Instance?.Hide();
    }

    public void ClearSlot()
    {
        collection?.ClearBaseAttack(
            slotIndex
        );
    }
}
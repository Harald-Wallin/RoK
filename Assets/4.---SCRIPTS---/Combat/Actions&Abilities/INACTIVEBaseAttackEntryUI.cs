using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BaseAttackEntryUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField]
    private Image icon;

    [SerializeField]
    private TMP_Text nameText;

    private AbilityData attack;

    public void Setup(
        AbilityData attack)
    {
        if (attack == null)
        {
            Clear();
            return;
        }

        if (!attack.IsBaseAttack)
        {
            Debug.LogWarning(
                $"Ability '{attack.abilityName}' skickades till " +
                $"{nameof(BaseAttackEntryUI)}, men dess Usage " +
                $"Type är inte BaseAttack.",
                this
            );

            Clear();
            return;
        }

        this.attack =
            attack;

        if (icon != null)
        {
            icon.sprite =
                attack.icon;

            icon.enabled =
                true;
        }

        if (nameText != null)
        {
            nameText.text =
                attack.abilityName;
        }

        DraggableAbility drag =
            icon != null
                ? icon.GetComponent<
                    DraggableAbility
                >()
                : null;

        if (drag != null)
        {
            drag.ability =
                attack;
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (attack == null ||
            icon == null)
        {
            return;
        }

        ItemTooltip.Instance?.Show(
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

    private void Clear()
    {
        attack = null;

        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;

            DraggableAbility drag =
                icon.GetComponent<
                    DraggableAbility
                >();

            if (drag != null)
            {
                drag.ability = null;
            }
        }

        if (nameText != null)
        {
            nameText.text = "";
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class AbilityEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;

    private AbilityData ability;

    public void Setup(AbilityData ability)
    {
        this.ability = ability;

        icon.sprite = ability.icon;
        nameText.text = ability.abilityName;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ability == null) return;

        // Don't show tooltip while dragging an ability
        if (DraggableAbility.dragged) return;

        var player = PlayerReference.Player;

        // Previous call (kept for reference):
        // ItemTooltip.Instance.Show(ability, icon.rectTransform, player);

        // New call: include explicit anchor mode so abilities use TopRight anchoring
        ItemTooltip.Instance.Show(
            (ITooltipProvider)ability,
            icon.rectTransform,
            player,
            ItemTooltip.TooltipAnchorMode.TopRight
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }
}

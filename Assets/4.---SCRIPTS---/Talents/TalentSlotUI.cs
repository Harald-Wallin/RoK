using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TalentSlotUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField] Image icon;
    [SerializeField] TMP_Text pointsText;
    [SerializeField] private Image lockOverlay;

    private TalentRuntime talent;

    public void Setup(TalentRuntime talent)
    {
        this.talent = talent;

        if (talent == null || talent.data == null)
        {
            Debug.LogError("Talent runtime is NULL");
            return;
        }

        icon.sprite = talent.data.icon;

        Refresh();
    }

    public void ForceRefresh()
    {
        Refresh();
    }

    void Refresh()
    {
        pointsText.text =
            $"{talent.currentPoints}/{talent.data.maxPoints}";

        bool canLearn =
            TalentManager.Instance.CanLearnTalent(talent);

        if (!canLearn && talent.currentPoints <= 0)
        {
            icon.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        }
        else
        {
            icon.color = Color.white;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (TalentManager.Instance.TrySpendPoint(talent))
        {
            TalentWindowUI window =GetComponentInParent<TalentWindowUI>();

            if (window != null)
            {
                window.RefreshAllSlots();
            }

            ItemTooltip.Instance.ShowTalent(talent.data,talent.currentPoints,icon.rectTransform);
        }

    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        ItemTooltip.Instance.ShowTalent(
            talent.data,
            talent.currentPoints,
            icon.rectTransform
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ItemTooltip.Instance.Hide();
    }
}

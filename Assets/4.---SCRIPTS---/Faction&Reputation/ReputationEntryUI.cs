using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReputationEntryUI : MonoBehaviour
{
    public TextMeshProUGUI factionNameText;
    public TextMeshProUGUI tierText;
    public Slider reputationSlider;
    public TextMeshProUGUI progressText;

    private FactionReputationData rep;
    private ReputationLevelDefinition levelDefinition;
    private ReputationDetailsPanelUI detailsPanel;

    public void Setup(
        FactionReputationData reputation,
        ReputationLevelDefinition levelDef,
        ReputationDetailsPanelUI panel)
    {
        rep = reputation;
        levelDefinition = levelDef;
        detailsPanel = panel;

        Refresh();

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClicked);


    }

    public void Refresh()
    {
        if (rep == null) return;

        factionNameText.text = rep.faction.factionName;
        tierText.text = levelDefinition.GetTierName(rep.level);

        int requiredXP = levelDefinition.GetXPRequired(rep.level);
        reputationSlider.maxValue = requiredXP;
        reputationSlider.value = rep.currentXP;

        Color color = ReputationColorUtility.GetColor(rep.level);
        reputationSlider.fillRect.GetComponent<Image>().color = color;
        progressText.text = $"{rep.currentXP}/{requiredXP}";

    }

    void OnClicked()
    {
        if (detailsPanel == null) return;

        // Om panelen redan visar denna faction → stäng den
        if (detailsPanel.IsShowing(rep.faction))
        {
            detailsPanel.gameObject.SetActive(false);
            return;
        }

        detailsPanel.ShowFaction(rep.faction);
        detailsPanel.gameObject.SetActive(true);
    }
}


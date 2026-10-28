using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FactionListItemUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    private Faction faction;
    private ReputationDetailsPanelUI detailsPanel;

    public void Initialize(Faction data, ReputationDetailsPanelUI panel)
    {
        faction = data;
        detailsPanel = panel;
        nameText.text = data.factionName;

        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        detailsPanel.ShowFaction(faction);
    }
}


using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ReputationDetailsPanelUI : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI factionNameText;
    public TextMeshProUGUI reputationStateText;
    public TextMeshProUGUI loreText;
    public TMP_Text reputationDetailText;

    [Header("Toggles")]
    public Toggle trackToggle;
    public Toggle murderToggle;

    private Faction currentFaction;
    private PlayerReputationManager reputationManager;
    FactionReputationData currentRep;

    void Awake()
    { 
        reputationManager = FindFirstObjectByType<PlayerReputationManager>();
        trackToggle.onValueChanged.AddListener(OnTrackChanged);
        murderToggle.onValueChanged.AddListener(OnMurderChanged);
    }

    public void ShowFaction(Faction faction)
    {
        currentFaction = faction;

        factionNameText.text = faction.factionName;

        var state = reputationManager.GetReputationState(currentFaction);
        reputationStateText.text = state.ToString();

        loreText.text = faction.loreDescription;

        trackToggle.SetIsOnWithoutNotify(
            reputationManager.IsTracked(faction));

        murderToggle.SetIsOnWithoutNotify(
            reputationManager.IsMurderEnabled(faction));

        // Update detail text showing current XP / required XP for next level
        if (reputationDetailText != null)
        {
            var repData = reputationManager.GetReputation(currentFaction);
            if (repData == null)
            {
                reputationDetailText.text = "0/0";
            }
            else
            {
                int required = reputationManager.levelDefinition.GetXPRequired(repData.level);
                reputationDetailText.text = $"{repData.currentXP}/{required}";
            }
        }
    }

    public bool IsShowing(Faction faction)
    {
        return currentFaction == faction && gameObject.activeSelf;
    }

    void OnTrackChanged(bool value)
    {
        if (currentFaction == null) return;

        reputationManager.SetTracked(currentFaction, value);
    }

    void OnMurderChanged(bool value)
    {
        if (currentFaction == null) return;

        reputationManager.SetMurderEnabled(currentFaction, value);
    }

}



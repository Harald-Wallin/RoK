using UnityEngine;
using System.Collections.Generic;

public class ReputationWindowUI : MonoBehaviour
{
    public PlayerReputationManager reputationManager;
    public ReputationEntryUI entryPrefab;
    public Transform contentParent;
    public ReputationDetailsPanelUI detailsPanel;


    private Dictionary<FactionReputationData, ReputationEntryUI> entries
        = new Dictionary<FactionReputationData, ReputationEntryUI>();


    void OnEnable()
    {
        reputationManager = FindFirstObjectByType<PlayerReputationManager>();

        if (reputationManager == null)
        {
            Debug.LogError("No PlayerReputationManager found!");
            return;
        }

        reputationManager.OnReputationChanged += RefreshEntry;

        BuildWindow();

        FactionNotificationManager.Instance
            ?.ClearUnread();
    }

    void OnDisable()
    {
        reputationManager.OnReputationChanged -= RefreshEntry;
    }

    void BuildWindow()
    {
        if (reputationManager == null)
        {
            //Debug.LogError("ReputationManager is NULL");
            return;
        }

        if (reputationManager.reputations == null)
        {
            //Debug.LogError("Reputations list is NULL");
            return;
        }

        //Debug.Log("Reputation count: " + reputationManager.reputations.Count);

        if (detailsPanel != null)
            detailsPanel.gameObject.SetActive(false);

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        entries.Clear();

        foreach (var rep in reputationManager.reputations)
        {
            if (!rep.discovered || rep.faction == null)
                continue;

            if (!rep.faction.showInReputationWindow)
                continue;

            var entry = Instantiate(entryPrefab, contentParent);
            entry.Setup(rep, reputationManager.levelDefinition, detailsPanel);


            entries.Add(rep, entry);
        }
    }

    void RefreshEntry(FactionReputationData rep)
    {
        if (entries.TryGetValue(rep, out var entry))
        {
            entry.Refresh();
        }
    }
}



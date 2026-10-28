using UnityEngine;

public sealed class PlayerStatsUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerStats playerStats;

    [Header("Stat Panels")]

    [SerializeField]
    private PlayerStatListPanelUI leftStatPanel;

    [SerializeField]
    private PlayerStatListPanelUI rightStatPanel;

    private void Awake()
    {
        ResolvePlayer();
    }

    private void OnEnable()
    {
        ResolvePlayer();
        Subscribe();
        BindPanels();
        RefreshUI();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolvePlayer()
    {
        if (playerStats != null)
            return;

        playerStats =
            PlayerReference.Player;

        if (playerStats == null)
        {
            playerStats =
                FindFirstObjectByType<
                    PlayerStats>();
        }
    }

    private void Subscribe()
    {
        Unsubscribe();

        if (playerStats == null)
            return;

        playerStats.OnStatsChanged +=
            RefreshUI;

        playerStats.OnHealthChanged +=
            RefreshUI;
    }

    private void Unsubscribe()
    {
        if (playerStats == null)
            return;

        playerStats.OnStatsChanged -=
            RefreshUI;

        playerStats.OnHealthChanged -=
            RefreshUI;
    }

    private void BindPanels()
    {
        if (playerStats == null)
            return;

        leftStatPanel?.Bind(
            playerStats);

        rightStatPanel?.Bind(
            playerStats);
    }

    private void RefreshUI()
    {
        leftStatPanel?.Refresh();
        rightStatPanel?.Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(
            false);
    }
}
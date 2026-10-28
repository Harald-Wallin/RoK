using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    public PlayerStats playerStats;
    public Slider expBar;
    public TMP_Text levelText;

    void Start()
    {
        if (playerStats == null)
            playerStats = PlayerReference.Player;

        UpdateUI();

        playerStats.OnExpChanged += UpdateUI;
        playerStats.OnLevelChanged += UpdateUI;
    }

    void UpdateUI()
    {
        expBar.maxValue = playerStats.expToNextLevel;
        expBar.value = playerStats.currentExp;
        levelText.text = "Lv " + playerStats.level;
    }

    void OnDestroy()
    {
        if (playerStats == null) return;

        playerStats.OnExpChanged -= UpdateUI;
        playerStats.OnLevelChanged -= UpdateUI;
    }
}


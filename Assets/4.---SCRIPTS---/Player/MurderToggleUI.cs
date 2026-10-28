using UnityEngine;
using UnityEngine.UI;

public class MurderToggleUI : MonoBehaviour
{
    private Toggle toggle;
    private PlayerStats playerStats;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        playerStats = PlayerReference.Player;
    }

    void Start()
    {
        // Sync UI med aktuell status
        toggle.isOn = playerStats.murderMode;

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void OnToggleChanged(bool value)
    {
        playerStats.murderMode = value;
        Debug.Log("Murder Mode set to: " + value);
    }
}

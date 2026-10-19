using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHealthUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerStats player;

    [SerializeField]
    private HealthBarVisual healthBarVisual;

    [SerializeField]
    private TMP_Text hpText;


    private void Start()
    {
        if (player == null)
        {
            player =
                PlayerReference.Player;
        }

        if (player == null)
        {
            Debug.LogWarning(
                $"{nameof(PlayerHealthUI)} kunde inte hitta spelaren.",
                this
            );

            return;
        }

        player.OnHealthChanged -=
            HandleHealthChanged;

        player.OnHealthChanged +=
            HandleHealthChanged;

        Refresh(
            true
        );
    }


    private void OnDestroy()
    {
        if (player == null)
            return;

        player.OnHealthChanged -=
            HandleHealthChanged;
    }


    private void HandleHealthChanged()
    {
        Refresh(
            false
        );
    }


    private void Refresh(
        bool immediate)
    {
        if (player == null)
            return;

        float maximumHealth =
            Mathf.Max(
                1f,
                player.GetStat(
                    StatType.MaxHP
                )
            );


        healthBarVisual?.SetHealth(
            player.currentHP,
            maximumHealth,
            immediate
        );


        if (hpText != null)
        {
            hpText.text =
                $"{Mathf.CeilToInt(player.currentHP)} / " +
                $"{Mathf.CeilToInt(maximumHealth)}";
        }
    }
}
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ExpBarUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private PlayerStats player;

    [SerializeField]
    private Image expFill;

    [SerializeField]
    private TMP_Text levelText;

    [SerializeField]
    private TMP_Text expText;


    private void Awake()
    {
        ConfigureFillImage(
            expFill
        );
    }


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
                $"{nameof(ExpBarUI)} kunde inte hitta spelaren.",
                this
            );

            return;
        }

        player.OnExpChanged -=
            UpdateUI;

        player.OnLevelChanged -=
            UpdateUI;

        player.OnExpChanged +=
            UpdateUI;

        player.OnLevelChanged +=
            UpdateUI;

        UpdateUI();
    }


    private void OnDestroy()
    {
        if (player == null)
            return;

        player.OnExpChanged -=
            UpdateUI;

        player.OnLevelChanged -=
            UpdateUI;
    }


    private void UpdateUI()
    {
        if (player == null)
            return;

        if (expFill != null)
        {
            float maximum =
                Mathf.Max(
                    0f,
                    player.expToNextLevel
                );

            expFill.fillAmount =
                maximum > 0f
                    ? Mathf.Clamp01(
                        player.currentExp /
                        maximum
                    )
                    : 0f;
        }

        if (levelText != null)
        {
            levelText.text =
                $"Lv {player.level}";
        }

        if (expText != null)
        {
            expText.text =
                $"{player.currentExp}/{player.expToNextLevel}";
        }
    }


    private static void ConfigureFillImage(
        Image image)
    {
        if (image == null)
            return;

        image.type =
            Image.Type.Filled;

        image.fillMethod =
            Image.FillMethod.Horizontal;

        image.fillOrigin =
            (int)Image.OriginHorizontal.Left;
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DeedRewardRowUI :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private TMP_Text typeText;

    [SerializeField]
    private TMP_Text nameText;

    [Header("Default Icons")]
    [SerializeField]
    private Sprite titleIcon;

    [SerializeField]
    private Sprite coinIcon;

    // =====================================================
    // PUBLIC API
    // =====================================================

    public void Bind(DeedRewardPreview reward)
    {
        if (reward == null)
        {
            Hide();
            return;
        }

        string typeLabel = string.Empty;
        string valueLabel = string.Empty;

        Sprite icon = reward.Icon;

        switch (reward.Kind)
        {
            case DeedRewardKind.Title:

                typeLabel = "Title:";
                valueLabel = reward.DisplayName;

                if (icon == null)
                    icon = titleIcon;

                break;

            case DeedRewardKind.Currency:

                typeLabel = string.Empty;
                valueLabel = reward.Amount.ToString();

                if (icon == null)
                    icon = coinIcon;

                break;

            case DeedRewardKind.Experience:

                typeLabel = "EXP:";
                valueLabel = reward.Amount.ToString();

                // Experience has no icon.
                icon = null;

                break;

            case DeedRewardKind.TrainableAbility:

                typeLabel = "Ability:";
                valueLabel = reward.DisplayName;

                if (icon == null &&
                    reward.Ability != null)
                {
                    icon = reward.Ability.icon;
                }

                break;

            case DeedRewardKind.Item:

                typeLabel = "Item:";
                valueLabel = reward.DisplayName;

                if (icon == null &&
                    reward.Item != null)
                {
                    icon = reward.Item.icon;
                }

                break;

            case DeedRewardKind.Cosmetic:

                typeLabel = "Cosmetic:";
                valueLabel = reward.DisplayName;

                break;

            case DeedRewardKind.GeneralTrait:
                typeLabel = "Passive:";
                valueLabel = reward.DisplayName;
                break;
        }

        // =================================================
        // TEXT
        // =================================================

        if (typeText != null)
        {
            typeText.text = typeLabel;

            typeText.gameObject.SetActive(
                !string.IsNullOrEmpty(typeLabel)
            );
        }

        if (nameText != null)
        {
            nameText.text = valueLabel;
        }

        // =================================================
        // ICON
        // =================================================

        if (iconImage != null)
        {
            iconImage.sprite = icon;

            iconImage.gameObject.SetActive(
                icon != null
            );
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}

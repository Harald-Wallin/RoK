using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visuell knapp för exakt ett IInteractionOption.
///
/// Komponenten känner inte till Vendor, Favour,
/// Tribute eller andra gameplay-system.
/// Den visar endast optionens presentation och
/// rapporterar klicket tillbaka till sitt fönster.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionOptionButton :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Button button;

    [SerializeField]
    private TMP_Text categoryText;

    [SerializeField]
    private TMP_Text mainText;

    [SerializeField]
    private TMP_Text statusText;

    private IInteractionOption option;

    private Action<IInteractionOption>
        clickedCallback;

    private InteractionCategory
    boundCategory;

    public IInteractionOption Option =>
        option;

    [Header("Favour Status Colors")]

    [SerializeField]
    private Color favourAvailableColor =
    new Color32(
        205,
        127,
        50,
        255
    );

    [SerializeField]
    private Color favourActiveColor =
        new Color32(
            192,
            192,
            192,
            255
        );

    [SerializeField]
    private Color favourCompleteColor =
        new Color32(
            212,
            175,
            55,
            255
        );

    [SerializeField]
    private Color favourFailedColor =
        new Color32(
            200,
            70,
            70,
            255
        );

    [SerializeField]
    private Color defaultStatusColor =
        Color.white;

    private void Awake()
    {
        if (button == null)
        {
            button =
                GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(
                HandleClicked
            );
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClicked
            );
        }
    }

    /// <summary>
    /// Binder knappen till ett konkret interaction-alternativ.
    /// </summary>
    public void Bind(
    IInteractionOption interactionOption,
    Action<IInteractionOption> onClicked,
    string textOverride = null)
    {
        option =
            interactionOption;

        clickedCallback =
            onClicked;

        if (option == null)
        {
            ClearPresentation();

            if (button != null)
            {
                button.interactable =
                    false;
            }

            return;
        }

        /*
         * Detta är vår FROZEN presentation.
         *
         * GetPresentation() anropas exakt när knappen binds.
         * Huvudtexten kommer därför inte förändras när
         * dynamisk status refreshas.
         */
        InteractionPresentation presentation =
            option.GetPresentation();

        if (!string.IsNullOrWhiteSpace(
        textOverride))
        {
            presentation =
                new InteractionPresentation(
                    presentation.Category,
                    textOverride
                );
        }

        boundCategory =
            presentation.Category;

        ApplyFrozenPresentation(
            presentation
        );

        RefreshDynamicStatus();

        if (button != null)
        {
            button.interactable =
                true;
        }
    }

    /// <summary>
    /// Hämtar presentationen på nytt.
    ///
    /// Detta låter dynamisk status, exempelvis en
    /// Favour-cooldown, uppdateras utan att knappen
    /// behöver skapas om.
    /// </summary>
    private void ApplyFrozenPresentation(
    InteractionPresentation presentation)
    {
        if (categoryText != null)
        {
            string category =
                GetCategoryLabel(
                    presentation.Category
                );

            bool hasCategory =
                !string.IsNullOrWhiteSpace(
                    category
                );

            categoryText.text =
                hasCategory
                    ? $"{category}:"
                    : string.Empty;

            categoryText.gameObject.SetActive(
                hasCategory
            );
        }

        if (mainText != null)
        {
            mainText.text =
                presentation.Text;
        }
    }

    public void RefreshDynamicStatus()
    {
        if (statusText == null)
            return;

        if (option == null)
        {
            statusText.text =
                string.Empty;

            statusText.gameObject.SetActive(
                false
            );

            return;
        }

        string status =
            option.GetStatusText();

        bool hasStatus =
            !string.IsNullOrWhiteSpace(
                status
            );

        statusText.text =
            hasStatus
                ? status
                : string.Empty;

        statusText.gameObject.SetActive(
            hasStatus
        );

        if (hasStatus)
        {
            ApplyStatusColor(
                status
            );
        }
    }

    private void ApplyStatusColor(
    string status)
    {
        if (statusText == null)
            return;

        /*
         * Statusfärgerna gäller endast Favour.
         *
         * Andra interaction-typer får defaultfärgen även
         * om de i framtiden får någon status.
         */
        if (boundCategory !=
            InteractionCategory.Favour)
        {
            statusText.color =
                defaultStatusColor;

            return;
        }

        if (status == "Available")
        {
            statusText.color =
                favourAvailableColor;

            return;
        }

        if (status == "Active")
        {
            statusText.color =
                favourActiveColor;

            return;
        }

        if (status == "Complete")
        {
            statusText.color =
                favourCompleteColor;

            return;
        }

        if (status == "Failed")
        {
            statusText.color =
                favourFailedColor;

            return;
        }

        /*
         * Cooldown ("Retry in: ...") och alla okända
         * framtida states förblir vita/default.
         */
        statusText.color =
            defaultStatusColor;
    }

    private static string GetCategoryLabel(
        InteractionCategory category)
    {
        switch (category)
        {
            case InteractionCategory.Favour:
                return "Favour";

            case InteractionCategory.Vendor:
                return "Vendor";

            case InteractionCategory.Tribute:
                return "Tribute";

            case InteractionCategory.Dialogue:
                return "Dialogue";

            case InteractionCategory.Trainer:
                return "Trainer";

            case InteractionCategory.Bank:
                return "Bank";

            case InteractionCategory.Crafting:
                return "Crafting";

            /*
             * Generiska world interactions behöver inte
             * visa en mekanisk "[Other]"-prefix.
             */
            case InteractionCategory.Other:
            case InteractionCategory.None:
            default:
                return string.Empty;
        }
    }

    private void HandleClicked()
    {
        if (option == null)
            return;

        clickedCallback?.Invoke(
            option
        );
    }

    private void ClearPresentation()
    {
        if (categoryText != null)
        {
            categoryText.text =
                string.Empty;

            categoryText.gameObject.SetActive(
                false
            );
        }

        if (mainText != null)
        {
            mainText.text =
                string.Empty;
        }

        if (statusText != null)
        {
            statusText.text =
                string.Empty;

            statusText.gameObject.SetActive(
                false
            );
        }
    }
}

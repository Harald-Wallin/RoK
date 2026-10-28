using UnityEngine;

[CreateAssetMenu(
    menuName = "Economy/Currency",
    fileName = "Currency_"
)]
public sealed class CurrencyData :
    ScriptableObject,
    ITooltipProvider
{
    [Header("Identity")]

    [SerializeField]
    private string displayName =
        "Coins";

    [TextArea(2, 4)]
    [SerializeField]
    private string description =
        "Currency accepted by merchants.";

    [Header("Visuals")]

    [SerializeField]
    private Sprite icon;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            displayName
        )
            ? name
            : displayName;

    public string Description =>
        description;

    public Sprite Icon =>
        icon;

    public TooltipData GetTooltipData(
        CharacterStats viewer)
    {
        TooltipData data =
            new TooltipData();

        data.title =
            DisplayName;

        data.titleColor =
            Color.white;

        data.subtitle =
            "Currency";

        data.description =
            description;

        PlayerCurrency playerCurrency =
            PlayerCurrency.Instance;

        if (playerCurrency != null &&
            playerCurrency.CurrencyDefinition ==
            this)
        {
            data.footer =
                $"Owned: {playerCurrency.Coins}";

            data.showFooter =
                true;
        }

        return data;
    }
}

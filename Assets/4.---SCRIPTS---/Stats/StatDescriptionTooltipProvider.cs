using UnityEngine;

public sealed class StatDescriptionTooltipProvider :
    ITooltipProvider
{
    private readonly StatDefinition definition;

    public StatDescriptionTooltipProvider(
        StatDefinition definition)
    {
        this.definition =
            definition;
    }

    public TooltipData GetTooltipData(
        CharacterStats viewer = null)
    {
        TooltipData data =
            new TooltipData();

        if (definition == null)
            return data;

        data.title =
            definition.DisplayName;

        data.description =
            definition.Description;

        data.showFooter =
            false;

        return data;
    }
}

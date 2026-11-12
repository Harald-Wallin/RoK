using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "New General Trait",
    menuName = "RöK/Deeds/General Trait")]
public sealed class GeneralTraitDefinition : ScriptableObject, ITooltipProvider
{
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField]
    private EffectDefinition[] effects =
        Array.Empty<EffectDefinition>();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName) ? name : displayName;

    public Sprite Icon => icon;

    public IReadOnlyList<EffectDefinition> Effects =>
        effects ?? Array.Empty<EffectDefinition>();

    public TooltipData GetTooltipData(CharacterStats viewer = null)
    {
        TooltipData tooltip = new()
        {
            title = DisplayName,
            subtitle = "General · Passive",
            description = description
        };

        foreach (EffectDefinition effect in Effects)
        {
            if (effect != null)
                tooltip.stats.Add(effect.GetDescription());
        }

        return tooltip;
    }
}

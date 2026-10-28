using System.Collections.Generic;
using UnityEngine;

public interface ITooltipProvider
{
    TooltipData GetTooltipData(
        CharacterStats viewer = null
    );
}

public class TooltipData
{
    public string title;

    public Color titleColor =
        Color.white;

    public string itemFavourSource;

    public string itemFavourRequirement;

    public string subtitle;

    public string description;

    public List<string> stats =
        new();

    public List<string> requirements =
        new();

    public List<string> favourContext =
        new();

    public string footer;

    public bool showFooter =
        false;
}
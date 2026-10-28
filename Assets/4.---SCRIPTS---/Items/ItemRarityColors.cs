using UnityEngine;

public static class ItemRarityColors
{
    public static Color GetColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Trash:
                return new Color(0.6f, 0.6f, 0.6f);

            case ItemRarity.Common:
                return Color.white;

            case ItemRarity.Uncommon:
                return new Color(0.2f, 1f, 0.2f);

            case ItemRarity.Strange:
                return new Color(0.3f, 0.6f, 1f);

            case ItemRarity.Extraordinary:
                return new Color(0.7f, 0.3f, 1f);

            case ItemRarity.FromTheSagas:
                return new Color(1f, 0.251f, 0f);

                default:
                return Color.white;
        }
    }
}
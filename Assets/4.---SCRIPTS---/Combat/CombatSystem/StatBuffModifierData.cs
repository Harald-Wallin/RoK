using System;
using UnityEngine;

[Serializable]
public sealed class StatBuffModifierData
{
    [SerializeField]
    private StatType stat;

    [SerializeField]
    private float value;

    [SerializeField]
    private ModifierType modifierType =
        ModifierType.Flat;

    public StatType Stat =>
        stat;

    public float Value =>
        value;

    public ModifierType ModifierType =>
        modifierType;

    public string GetTooltipText()
    {
        if (modifierType ==
            ModifierType.Percent)
        {
            float percentage =
                value * 100f;

            string sign =
                percentage >= 0f
                    ? "+"
                    : string.Empty;

            return
                $"{stat} {sign}{percentage:0.#}%";
        }

        string flatSign =
            value >= 0f
                ? "+"
                : string.Empty;

        return
            $"{stat} {flatSign}{value:0.#}";
    }
}
using UnityEngine;

public enum ModifierType
{
    Flat,
    Percent
}

public enum ModifierSourceType
{
    Talent,
    Equipment,
    Buff,
    Oath
}

public class StatModifier
{
    public StatType stat;
    public float value;
    public ModifierType type;
    public object source;
    public ModifierSourceType sourceType;

    public StatModifier(StatType stat, float value, ModifierType type, object source, ModifierSourceType sourceType)
    {
        this.stat = stat;
        this.value = value;
        this.type = type;
        this.source = source;
        this.sourceType = sourceType;
    }
}
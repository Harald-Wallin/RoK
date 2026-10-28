using System;
using UnityEngine;

[Serializable]
public class ItemStatModifier
{
    public StatType stat;
    public ModifierType modifierType = ModifierType.Flat;
    public float value;
}

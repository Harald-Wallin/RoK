using System;
using UnityEngine;

[Serializable]
public class DerivedStat
{
    [Tooltip("Derived stat generated from the Scaling Profile.")]
    public StatType stat;

    [Tooltip("Current calculated value. This field is read-only and rebuilt automatically.")]
    public float value;
}
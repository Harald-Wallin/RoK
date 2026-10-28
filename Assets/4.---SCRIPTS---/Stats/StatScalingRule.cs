using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatScalingRule
{
    public StatType source;

    public List<StatScalingOutput> outputs = new();
}
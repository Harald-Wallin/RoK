using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Stats/Scaling Profile")]
public class StatScalingProfile : ScriptableObject
{
    public List<StatScalingRule> rules = new();
}

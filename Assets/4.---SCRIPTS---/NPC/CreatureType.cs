
using System;
using UnityEngine;

[Flags]
[InspectorOrder(
    InspectorSort.ByName,
    InspectorSortDirection.Ascending
)]
public enum CreatureType
{
    None = 0,

    // Specific creature families
    Wolf = 1 << 0,
    Bear = 1 << 1,
    Boar = 1 << 2,

    // Broader classifications
    Beast = 1 << 3,
    Humanoid = 1 << 4,
    Undead = 1 << 5,
    Spirit = 1 << 6,
    Construct = 1 << 7

    // Add new values using:
    // NewCreature = 1 << 8,
}



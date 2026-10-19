using System;
using UnityEngine;

[Serializable]
public readonly struct BuildingEdgeRun
{
    public readonly int Level;
    public readonly bool Horizontal;
    public readonly float FixedCoordinate;
    public readonly float StartCoordinate;
    public readonly float EndCoordinate;

    public BuildingEdgeRun(
        int level,
        bool horizontal,
        float fixedCoordinate,
        float startCoordinate,
        float endCoordinate)
    {
        Level = level;
        Horizontal = horizontal;
        FixedCoordinate = fixedCoordinate;
        StartCoordinate = Mathf.Min(startCoordinate, endCoordinate);
        EndCoordinate = Mathf.Max(startCoordinate, endCoordinate);
    }

    public float Length =>
        EndCoordinate - StartCoordinate;
}

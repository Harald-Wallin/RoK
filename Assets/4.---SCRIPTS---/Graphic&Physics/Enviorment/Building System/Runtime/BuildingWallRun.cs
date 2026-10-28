using System;
using UnityEngine;

[Serializable]
public readonly struct BuildingWallRun
{
    public readonly int Level;
    public readonly BuildingEdgeDirection Direction;
    public readonly float FixedCoordinate;
    public readonly float StartCoordinate;
    public readonly float EndCoordinate;

    public BuildingWallRun(
        int level,
        BuildingEdgeDirection direction,
        float fixedCoordinate,
        float startCoordinate,
        float endCoordinate)
    {
        Level = level;
        Direction = direction;
        FixedCoordinate = fixedCoordinate;
        StartCoordinate = Mathf.Min(startCoordinate, endCoordinate);
        EndCoordinate = Mathf.Max(startCoordinate, endCoordinate);
    }

    public bool Horizontal =>
        Direction == BuildingEdgeDirection.North ||
        Direction == BuildingEdgeDirection.South;

    public float Length =>
        EndCoordinate - StartCoordinate;
}

using System;
using UnityEngine;

public enum BuildingEdgeDirection
{
    North,
    East,
    South,
    West
}

[Serializable]
public readonly struct BuildingEdge : IEquatable<BuildingEdge>
{
    public readonly BuildingCell Cell;
    public readonly BuildingEdgeDirection Direction;

    public BuildingEdge(
        BuildingCell cell,
        BuildingEdgeDirection direction)
    {
        Cell = cell;
        Direction = direction;
    }

    public Vector2 LocalStart
    {
        get
        {
            float s = BuildingGrid.StructuralStep;
            float x = Cell.X * s;
            float y = Cell.Y * s;

            switch (Direction)
            {
                case BuildingEdgeDirection.North:
                    return new Vector2(x, y + s);
                case BuildingEdgeDirection.East:
                    return new Vector2(x + s, y + s);
                case BuildingEdgeDirection.South:
                    return new Vector2(x + s, y);
                default:
                    return new Vector2(x, y);
            }
        }
    }

    public Vector2 LocalEnd
    {
        get
        {
            float s = BuildingGrid.StructuralStep;
            float x = Cell.X * s;
            float y = Cell.Y * s;

            switch (Direction)
            {
                case BuildingEdgeDirection.North:
                    return new Vector2(x + s, y + s);
                case BuildingEdgeDirection.East:
                    return new Vector2(x + s, y);
                case BuildingEdgeDirection.South:
                    return new Vector2(x, y);
                default:
                    return new Vector2(x, y + s);
            }
        }
    }

    public bool Equals(BuildingEdge other)
    {
        return Cell.Equals(other.Cell) &&
               Direction == other.Direction;
    }

    public override bool Equals(object obj)
    {
        return obj is BuildingEdge other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return Cell.GetHashCode() * 397 ^ (int)Direction;
        }
    }
}

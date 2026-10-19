using System;

[Serializable]
public readonly struct BuildingCell : IEquatable<BuildingCell>
{
    public readonly int Level;
    public readonly int X;
    public readonly int Y;

    public BuildingCell(int level, int x, int y)
    {
        Level = level;
        X = x;
        Y = y;
    }

    public BuildingCell Offset(int x, int y)
    {
        return new BuildingCell(Level, X + x, Y + y);
    }

    public bool Equals(BuildingCell other)
    {
        return Level == other.Level &&
               X == other.X &&
               Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is BuildingCell other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Level;
            hash = hash * 397 ^ X;
            hash = hash * 397 ^ Y;
            return hash;
        }
    }
}

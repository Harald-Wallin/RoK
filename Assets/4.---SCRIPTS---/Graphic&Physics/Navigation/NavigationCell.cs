using UnityEngine;

/// <summary>
/// Lätt runtime-data för en cell i ett NavigationRegion-grid.
///
/// Ingen MonoBehaviour.
/// Ingen GameObject.
/// Ingen Update().
/// </summary>
public readonly struct NavigationCell
{
    public readonly int X;
    public readonly int Y;

    public readonly Vector2 WorldPosition;

    public readonly bool Walkable;

    public NavigationCell(
        int x,
        int y,
        Vector2 worldPosition,
        bool walkable)
    {
        X = x;
        Y = y;

        WorldPosition =
            worldPosition;

        Walkable =
            walkable;
    }
}

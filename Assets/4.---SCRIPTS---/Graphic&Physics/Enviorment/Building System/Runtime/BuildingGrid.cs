using UnityEngine;

public static class BuildingGrid
{
    public const int PixelsPerUnit = 32;
    public const int StructuralPixels = 32;
    public const int FinePixels = 8;

    public const float StructuralStep =
        StructuralPixels / (float)PixelsPerUnit;

    public const float FineStep =
        FinePixels / (float)PixelsPerUnit;

    public static Vector2 GridToLocal(Vector2Int gridPosition)
    {
        return new Vector2(
            gridPosition.x * StructuralStep,
            gridPosition.y * StructuralStep
        );
    }

    public static Vector2Int LocalToGrid(Vector2 localPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt(localPosition.x / StructuralStep),
            Mathf.RoundToInt(localPosition.y / StructuralStep)
        );
    }

    public static Vector2 SnapStructural(Vector2 localPosition)
    {
        return GridToLocal(LocalToGrid(localPosition));
    }

    public static Vector2 SnapFine(Vector2 localPosition)
    {
        return new Vector2(
            Mathf.Round(localPosition.x / FineStep) * FineStep,
            Mathf.Round(localPosition.y / FineStep) * FineStep
        );
    }
}

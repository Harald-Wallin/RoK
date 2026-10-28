
using UnityEngine;

/// <summary>
/// Central authority for converting world Y-depth
/// into Unity sorting orders.
///
/// This class owns the calculation only.
/// Renderers and SortingGroups are controlled
/// by their respective presentation systems.
/// </summary>
public static class WorldSorting
{
    // Existing RöK precision.
    // Kept unchanged during foundation migration.
    public const float Precision = 100f;

    public const int MinSortingOrder = -32768;
    public const int MaxSortingOrder = 32767;

    public static int CalculateOrder(
        float worldY,
        float yOffset = 0f,
        int sortingOrderOffset = 0)
    {
        int depthOrder =
            Mathf.RoundToInt(
                (-worldY - yOffset) *
                Precision
            );

        return depthOrder +
            sortingOrderOffset;
    }

    public static bool IsWithinSortingRange(
        int sortingOrder)
    {
        return
            sortingOrder >= MinSortingOrder &&
            sortingOrder <= MaxSortingOrder;
    }
}


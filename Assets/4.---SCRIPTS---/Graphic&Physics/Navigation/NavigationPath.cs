using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resultatet av en navigation-sökning.
///
/// RawPoints:
/// Grid-path direkt från A*.
///
/// Points:
/// Den färdiga smoothade pathen som NPC:n faktiskt bör följa.
/// </summary>
public sealed class NavigationPath
{
    private readonly List<Vector2>
        rawPoints =
            new();

    private readonly List<Vector2>
        points =
            new();

    public IReadOnlyList<Vector2>
        RawPoints =>
            rawPoints;

    public IReadOnlyList<Vector2>
        Points =>
            points;

    /*
     * En användbar path måste innehålla minst:
     *
     * 0 = start
     * 1 = destination
     *
     * En ensam punkt är INTE en faktisk route.
     */
    public bool IsValid =>
        points.Count >= 2;

    public int PointCount =>
        points.Count;

    public void SetRawPoints(
        IEnumerable<Vector2> newPoints)
    {
        rawPoints.Clear();

        if (newPoints == null)
            return;

        rawPoints.AddRange(
            newPoints
        );
    }

    public void SetPoints(
        IEnumerable<Vector2> newPoints)
    {
        points.Clear();

        if (newPoints == null)
            return;

        points.AddRange(
            newPoints
        );
    }

    public Vector2 GetPoint(
        int index)
    {
        if (points.Count == 0)
            return Vector2.zero;

        index =
            Mathf.Clamp(
                index,
                0,
                points.Count - 1
            );

        return points[index];
    }

    public void Clear()
    {
        rawPoints.Clear();
        points.Clear();
    }
}
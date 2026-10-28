using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debugverktyg för NavigationRegion.
///
/// Visar:
/// - startpunkt
/// - targetpunkt
/// - rå A*-path
/// - smoothad slutpath
///
/// Pathen kan testas i både Play Mode och Edit Mode.
/// </summary>
[ExecuteAlways]
public sealed class NavigationDebugTester :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private NavigationRegion region;

    [SerializeField]
    private Transform start;

    [SerializeField]
    private Transform target;

    [Header("Refresh")]

    [SerializeField]
    private bool continuouslyRefresh =
        true;

    [Header("Path Debug")]

    [SerializeField]
    private bool drawRawPath =
        true;

    [SerializeField]
    private bool drawSmoothedPath =
        true;

    [SerializeField]
    [Min(0.01f)]
    private float rawPointSize =
        0.05f;

    [SerializeField]
    [Min(0.01f)]
    private float smoothedPointSize =
        0.12f;

    [Header("Endpoints")]

    [SerializeField]
    private bool drawEndpoints =
        true;

    [SerializeField]
    [Min(0.01f)]
    private float endpointSize =
        0.2f;

    private NavigationPath currentPath;

    private Vector2 lastStartPosition;
    private Vector2 lastTargetPosition;

    private void Update()
    {
        if (!continuouslyRefresh)
            return;

        RefreshIfNeeded();
    }

    private void OnValidate()
    {
        rawPointSize =
            Mathf.Max(
                0.01f,
                rawPointSize
            );

        smoothedPointSize =
            Mathf.Max(
                0.01f,
                smoothedPointSize
            );

        endpointSize =
            Mathf.Max(
                0.01f,
                endpointSize
            );

        if (!Application.isPlaying)
        {
            CalculatePath();
        }
    }

    private void RefreshIfNeeded()
    {
        if (region == null ||
            start == null ||
            target == null)
        {
            currentPath =
                null;

            return;
        }

        Vector2 startPosition =
            start.position;

        Vector2 targetPosition =
            target.position;

        bool startChanged =
            (
                startPosition -
                lastStartPosition
            ).sqrMagnitude >
            0.0001f;

        bool targetChanged =
            (
                targetPosition -
                lastTargetPosition
            ).sqrMagnitude >
            0.0001f;

        if (!startChanged &&
            !targetChanged &&
            currentPath != null)
        {
            return;
        }

        CalculatePath();

        lastStartPosition =
            startPosition;

        lastTargetPosition =
            targetPosition;
    }

    [ContextMenu("Calculate Path")]
    public void CalculatePath()
    {
        if (region == null ||
            start == null ||
            target == null)
        {
            currentPath =
                null;

            return;
        }

        /*
         * I Edit Mode behöver regionens walkability-data
         * finnas innan A* kan köras.
         */
        region.Bake();

        currentPath =
            AStarPathfinder.FindPath(
                region,
                start.position,
                target.position
            );
    }

    [ContextMenu("Bake Region")]
    public void BakeRegion()
    {
        if (region == null)
            return;

        region.Bake();

        CalculatePath();
    }

    private void OnDrawGizmos()
    {
        DrawEndpoints();

        if (currentPath == null ||
            !currentPath.IsValid)
        {
            return;
        }

        if (drawRawPath)
        {
            DrawPath(
                currentPath.RawPoints,
                rawPointSize
            );
        }

        if (drawSmoothedPath)
        {
            DrawPath(
                currentPath.Points,
                smoothedPointSize
            );
        }
    }

    private void DrawEndpoints()
    {
        if (!drawEndpoints)
            return;

        if (start != null)
        {
            Gizmos.DrawWireSphere(
                start.position,
                endpointSize
            );
        }

        if (target != null)
        {
            Gizmos.DrawWireSphere(
                target.position,
                endpointSize
            );
        }
    }

    private static void DrawPath(
        IReadOnlyList<Vector2> points,
        float markerSize)
    {
        if (points == null ||
            points.Count == 0)
        {
            return;
        }

        for (int i = 0;
             i < points.Count;
             i++)
        {
            Vector2 point =
                points[i];

            Gizmos.DrawSphere(
                point,
                markerSize
            );

            if (i >=
                points.Count - 1)
            {
                continue;
            }

            Gizmos.DrawLine(
                point,
                points[i + 1]
            );
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// En authored väg i världen.
///
/// RoadPath äger endast vägens authoring-data:
/// - profil
/// - kontrollpunkter
/// - width override
/// - appearance seed
///
/// All genererad presentation kan alltid byggas om.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RoadPath :
    MonoBehaviour
{
    // =========================================================
    // ACTIVE ROAD REGISTRY
    // =========================================================

    /*
     * Lättviktsregister över aktiva RoadPaths.
     *
     * PlayerMovement behöver då inte köra FindObjectsByType
     * varje physics-frame.
     */
    private static readonly List<RoadPath>
        activeRoads =
            new();


    // =========================================================
    // PROFILE
    // =========================================================

    [Header("Road")]

    [SerializeField]
    private RoadProfile profile;


    // =========================================================
    // WIDTH
    // =========================================================

    [Header("Width Override")]

    [SerializeField]
    private bool overrideWidth;

    [SerializeField]
    [Min(0.05f)]
    private float widthOverride =
        3.5f;


    // =========================================================
    // APPEARANCE
    // =========================================================

    [Header("Appearance")]

    [SerializeField]
    private int appearanceSeed =
        12345;


    // =========================================================
    // PATH
    // =========================================================

    [Header("Path")]

    [SerializeField]
    private List<Vector2> controlPoints =
        new()
        {
            new Vector2(
                -2f,
                0f
            ),

            new Vector2(
                2f,
                0f
            )
        };


    // =========================================================
    // PUBLIC
    // =========================================================

    public RoadProfile Profile =>
        profile;

    public bool OverrideWidth =>
        overrideWidth;

    public float WidthOverride =>
        widthOverride;

    public int AppearanceSeed =>
        appearanceSeed;

    public int ControlPointCount =>
        controlPoints != null
            ? controlPoints.Count
            : 0;

    public IReadOnlyList<Vector2>
        ControlPoints =>
            controlPoints;


    public static IReadOnlyList<RoadPath>
        ActiveRoads =>
            activeRoads;


    public float EffectiveBaseWidth
    {
        get
        {
            if (overrideWidth)
            {
                return Mathf.Max(
                    0.05f,
                    widthOverride
                );
            }

            if (profile != null)
            {
                return Mathf.Max(
                    0.05f,
                    profile.BaseWidth
                );
            }

            return 1f;
        }
    }


    // =========================================================
    // UNITY
    // =========================================================

    private void OnEnable()
    {
        RegisterActiveRoad();


        /*
         * Bara den egna presentationen här.
         *
         * RoadNetwork gör ett gemensamt network-rebuild
         * från sitt eget OnEnable.
         */
        RoadMeshBuilder.Rebuild(
            this
        );
    }


    private void OnDisable()
    {
        activeRoads.Remove(
            this
        );
    }


    // =========================================================
    // REBUILD
    // =========================================================

    public void RebuildVisual()
    {
        RoadNetwork network =
            GetComponentInParent<
                RoadNetwork
            >();


        if (network != null)
        {
            network.NotifyRoadChanged(
                this
            );

            return;
        }


        RoadMeshBuilder.Rebuild(
            this
        );
    }


    // =========================================================
    // GAMEPLAY SURFACE QUERY
    // =========================================================

    /// <summary>
    /// Hittar den aktiva RoadPath vars redan genererade Surface-mesh
    /// innehåller worldPoint.
    ///
    /// Om flera vägar överlappar:
    /// 1. högre player movement multiplier vinner
    /// 2. vid lika multiplier vinner närmaste centerline
    ///
    /// Därmed staplas aldrig road-bonusen i junctions.
    /// </summary>
    public static bool TryGetRoadAtWorldPoint(
        Vector2 worldPoint,
        out RoadPath road,
        out float playerMovementSpeedMultiplier)
    {
        road =
            null;

        playerMovementSpeedMultiplier =
            1f;


        float bestMultiplier =
            float.NegativeInfinity;

        float bestCenterLineDistanceSqr =
            float.PositiveInfinity;


        for (int i =
                 activeRoads.Count - 1;
             i >= 0;
             i--)
        {
            RoadPath candidate =
                activeRoads[i];


            if (candidate == null)
            {
                activeRoads.RemoveAt(
                    i
                );

                continue;
            }


            if (!candidate.isActiveAndEnabled ||
                candidate.Profile == null)
            {
                continue;
            }


            if (!RoadMeshBuilder
                    .ContainsWorldPoint(
                        candidate,
                        worldPoint,
                        out float centerLineDistanceSqr))
            {
                continue;
            }


            float candidateMultiplier =
                Mathf.Max(
                    0f,
                    candidate.Profile
                        .PlayerMovementSpeedMultiplier
                );


            bool hasNoSelection =
                road == null;


            bool hasHigherMultiplier =
                candidateMultiplier >
                bestMultiplier +
                0.0001f;


            bool sameMultiplierButCloser =
                Mathf.Abs(
                    candidateMultiplier -
                    bestMultiplier
                ) <=
                0.0001f &&
                centerLineDistanceSqr <
                bestCenterLineDistanceSqr;


            if (!hasNoSelection &&
                !hasHigherMultiplier &&
                !sameMultiplierButCloser)
            {
                continue;
            }


            road =
                candidate;

            bestMultiplier =
                candidateMultiplier;

            bestCenterLineDistanceSqr =
                centerLineDistanceSqr;
        }


        if (road == null)
        {
            return false;
        }


        playerMovementSpeedMultiplier =
            bestMultiplier;

        return true;
    }


    // =========================================================
    // PROFILE
    // =========================================================

    public void SetProfile(
        RoadProfile newProfile)
    {
        profile =
            newProfile;

        RebuildVisual();
    }


    public void SetWidthOverride(
        bool enabled,
        float width)
    {
        overrideWidth =
            enabled;

        widthOverride =
            Mathf.Max(
                0.05f,
                width
            );

        RebuildVisual();
    }


    // =========================================================
    // POINT ACCESS
    // =========================================================

    public Vector2 GetLocalPoint(
        int index)
    {
        return
            controlPoints[index];
    }


    public Vector2 GetWorldPoint(
        int index)
    {
        Vector3 worldPoint =
            transform.TransformPoint(
                controlPoints[index]
            );

        return
            new Vector2(
                worldPoint.x,
                worldPoint.y
            );
    }


    public void SetLocalPoint(
        int index,
        Vector2 localPoint)
    {
        if (!IsValidPointIndex(
                index))
        {
            return;
        }

        controlPoints[index] =
            localPoint;

        RebuildVisual();
    }


    public void SetWorldPoint(
        int index,
        Vector2 worldPoint)
    {
        if (!IsValidPointIndex(
                index))
        {
            return;
        }

        Vector3 localPoint =
            transform.InverseTransformPoint(
                new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    transform.position.z
                )
            );

        controlPoints[index] =
            new Vector2(
                localPoint.x,
                localPoint.y
            );

        RebuildVisual();
    }


    public void AddLocalPoint(
        Vector2 localPoint)
    {
        EnsurePointList();

        controlPoints.Add(
            localPoint
        );

        RebuildVisual();
    }


    public void AddWorldPoint(
        Vector2 worldPoint)
    {
        EnsurePointList();

        Vector3 localPoint =
            transform.InverseTransformPoint(
                new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    transform.position.z
                )
            );

        controlPoints.Add(
            new Vector2(
                localPoint.x,
                localPoint.y
            )
        );

        RebuildVisual();
    }


    public void InsertWorldPoint(
        int index,
        Vector2 worldPoint)
    {
        EnsurePointList();

        Vector3 localPoint =
            transform.InverseTransformPoint(
                new Vector3(
                    worldPoint.x,
                    worldPoint.y,
                    transform.position.z
                )
            );

        int safeIndex =
            Mathf.Clamp(
                index,
                0,
                controlPoints.Count
            );

        controlPoints.Insert(
            safeIndex,
            new Vector2(
                localPoint.x,
                localPoint.y
            )
        );

        RebuildVisual();
    }


    public void RemovePoint(
        int index)
    {
        if (!IsValidPointIndex(
                index))
        {
            return;
        }

        controlPoints.RemoveAt(
            index
        );

        RebuildVisual();
    }


    public void ClearPoints()
    {
        EnsurePointList();

        controlPoints.Clear();

        RebuildVisual();
    }


    public void ReplaceWorldPoints(
        IEnumerable<Vector2> worldPoints)
    {
        EnsurePointList();

        controlPoints.Clear();

        if (worldPoints != null)
        {
            foreach (
                Vector2 worldPoint
                in worldPoints)
            {
                Vector3 localPoint =
                    transform
                        .InverseTransformPoint(
                            new Vector3(
                                worldPoint.x,
                                worldPoint.y,
                                transform.position.z
                            )
                        );

                controlPoints.Add(
                    new Vector2(
                        localPoint.x,
                        localPoint.y
                    )
                );
            }
        }

        RebuildVisual();
    }


    // =========================================================
    // WIDTH
    // =========================================================

    /// <summary>
    /// Lodrätt:
    /// 100 % av grundbredd.
    ///
    /// Horisontellt:
    /// HorizontalWidthScale.
    ///
    /// Mellanliggande vinklar interpoleras mjukt.
    /// </summary>
    public float GetWidthForDirection(
        Vector2 worldDirection)
    {
        float baseWidth =
            EffectiveBaseWidth;

        if (worldDirection.sqrMagnitude <=
            0.000001f)
        {
            return baseWidth;
        }

        worldDirection.Normalize();

        float verticalness =
            Mathf.Abs(
                Vector2.Dot(
                    worldDirection,
                    Vector2.up
                )
            );

        float horizontalScale =
            profile != null
                ? profile
                    .HorizontalWidthScale
                : 1f;

        float directionScale =
            Mathf.Lerp(
                horizontalScale,
                1f,
                verticalness
            );

        return
            baseWidth *
            directionScale;
    }


    // =========================================================
    // APPEARANCE
    // =========================================================

    public void RandomizeAppearanceSeed()
    {
        appearanceSeed =
            Guid.NewGuid()
                .GetHashCode();

        if (appearanceSeed == 0)
        {
            appearanceSeed =
                1;
        }

        RebuildVisual();
    }


    // =========================================================
    // INTERNAL
    // =========================================================

    private void RegisterActiveRoad()
    {
        if (activeRoads.Contains(
                this))
        {
            return;
        }


        activeRoads.Add(
            this
        );
    }


    private bool IsValidPointIndex(
        int index)
    {
        return
            controlPoints != null &&
            index >= 0 &&
            index < controlPoints.Count;
    }


    private void EnsurePointList()
    {
        controlPoints ??=
            new List<Vector2>();
    }


    private Vector2
        GetApproximateWorldTangent(
            int index)
    {
        if (ControlPointCount < 2)
        {
            return
                Vector2.right;
        }

        Vector2 previous;
        Vector2 next;

        if (index <= 0)
        {
            previous =
                GetWorldPoint(0);

            next =
                GetWorldPoint(1);
        }
        else if (
            index >=
            ControlPointCount - 1)
        {
            previous =
                GetWorldPoint(
                    ControlPointCount - 2
                );

            next =
                GetWorldPoint(
                    ControlPointCount - 1
                );
        }
        else
        {
            previous =
                GetWorldPoint(
                    index - 1
                );

            next =
                GetWorldPoint(
                    index + 1
                );
        }

        Vector2 direction =
            next -
            previous;

        if (direction.sqrMagnitude <=
            0.000001f)
        {
            return
                Vector2.right;
        }

        return
            direction.normalized;
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        widthOverride =
            Mathf.Max(
                0.05f,
                widthOverride
            );

        EnsurePointList();
    }


    // =========================================================
    // DEBUG PREVIEW
    // =========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (controlPoints == null ||
            controlPoints.Count == 0)
        {
            return;
        }

        DrawCenterLine();
        DrawWidthPreview();
        DrawControlPoints();
    }


    private void DrawCenterLine()
    {
        Gizmos.color =
            new Color(
                1f,
                0.75f,
                0.15f,
                1f
            );

        for (int i = 0;
             i <
             controlPoints.Count - 1;
             i++)
        {
            Gizmos.DrawLine(
                GetWorldPoint(i),
                GetWorldPoint(i + 1)
            );
        }
    }


    private void DrawWidthPreview()
    {
        if (controlPoints.Count < 2)
            return;

        Vector2[] leftEdge =
            new Vector2[
                controlPoints.Count
            ];

        Vector2[] rightEdge =
            new Vector2[
                controlPoints.Count
            ];


        for (int i = 0;
             i < controlPoints.Count;
             i++)
        {
            Vector2 point =
                GetWorldPoint(i);

            Vector2 tangent =
                GetApproximateWorldTangent(
                    i
                );

            Vector2 normal =
                new Vector2(
                    -tangent.y,
                    tangent.x
                );

            float halfWidth =
                GetWidthForDirection(
                    tangent
                ) *
                0.5f;

            leftEdge[i] =
                point +
                normal *
                halfWidth;

            rightEdge[i] =
                point -
                normal *
                halfWidth;
        }


        Gizmos.color =
            new Color(
                0.95f,
                0.55f,
                0.15f,
                0.75f
            );


        for (int i = 0;
             i <
             controlPoints.Count - 1;
             i++)
        {
            Gizmos.DrawLine(
                leftEdge[i],
                leftEdge[i + 1]
            );

            Gizmos.DrawLine(
                rightEdge[i],
                rightEdge[i + 1]
            );
        }
    }


    private void DrawControlPoints()
    {
        Gizmos.color =
            Color.white;

        for (int i = 0;
             i < controlPoints.Count;
             i++)
        {
            Gizmos.DrawSphere(
                GetWorldPoint(i),
                0.07f
            );
        }
    }

#endif
}
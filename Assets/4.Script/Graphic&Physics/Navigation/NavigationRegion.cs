using UnityEngine;

/// <summary>
/// Ett bakat navigation-grid för ett avgränsat område.
///
/// Regionen känner endast till:
/// - vilka celler som är walkable
/// - grid/world-konvertering
/// - navigation-clearance mot World-geometri
///
/// Regionen utför INTE själv A*.
/// </summary>
public sealed class NavigationRegion :
    MonoBehaviour
{
    [Header("Region")]

    [SerializeField]
    [Min(1f)]
    private float width =
        32f;

    [SerializeField]
    [Min(1f)]
    private float height =
        32f;

    [SerializeField]
    [Min(0.1f)]
    private float cellSize =
        0.5f;

    [Header("Agent")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Standardradie runt varje navigation-cell som måste " +
        "vara fri från blockerande World-geometri."
    )]
    private float agentRadius =
        0.3f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
    "Extra säkerhetsmarginal mellan navigation-pathen " +
    "och World-geometri. " +
    "Påverkar navigation men inte NPC:ns fysiska collider."
)]
    private float obstacleClearancePadding =
    0.2f;

    public float NavigationRadius =>
        agentRadius +
        obstacleClearancePadding;

    [Header("Obstacles")]

    [SerializeField]
    [Tooltip(
        "Lager som blockerar navigation. " +
        "Normalt endast World."
    )]
    private LayerMask obstacleLayers;

    [Header("Movement")]

    [SerializeField]
    private bool allowDiagonalMovement =
        true;

    [SerializeField]
    [Tooltip(
        "Förhindrar diagonal rörelse genom hörn där två " +
        "blockerade celler möts."
    )]
    private bool preventCornerCutting =
        true;

    [Header("Bake")]

    [SerializeField]
    [Tooltip(
        "Om regionen automatiskt ska bakas i Awake om ingen " +
        "runtime-data finns."
    )]
    private bool bakeOnAwake =
        true;

    [Header("Debug")]

    [SerializeField]
    private bool drawGrid;

    [SerializeField]
    private bool drawBlockedCells =
        true;

    [SerializeField]
    private bool drawRegionBounds =
        true;

    private bool[] walkableCells;

    private int columns;
    private int rows;


    private readonly Collider2D[] obstacleOverlapResults =
        new Collider2D[1];

    private readonly RaycastHit2D[] obstacleCastResults =
        new RaycastHit2D[1];


    public float CellSize =>
        cellSize;

    public float AgentRadius =>
        agentRadius;

    public float Width =>
    width;

    public float Height =>
        height;

    public int Columns =>
        columns;

    public int Rows =>
        rows;

    public bool AllowDiagonalMovement =>
        allowDiagonalMovement;

    public bool PreventCornerCutting =>
        preventCornerCutting;

    public LayerMask ObstacleLayers =>
        obstacleLayers;

    public Vector2 BottomLeft
    {
        get
        {
            return
                (Vector2)transform.position -
                new Vector2(
                    width * 0.5f,
                    height * 0.5f
                );
        }
    }

    public void SetDimensions(
    float newWidth,
    float newHeight)
    {
        width =
            Mathf.Max(
                1f,
                newWidth
            );

        height =
            Mathf.Max(
                1f,
                newHeight
            );

        RecalculateDimensions();
    }

    public Bounds RegionBounds
    {
        get
        {
            return new Bounds(
                transform.position,
                new Vector3(
                    width,
                    height,
                    0f
                )
            );
        }
    }

    private void Awake()
    {
        if (bakeOnAwake)
        {
            Bake();
        }
    }

    private void OnEnable()
    {
        NavigationWorld
            .Instance
            .RegisterRegion(
                this
            );
    }

    private void OnDisable()
    {
        if (!NavigationWorld.HasInstance)
            return;

        NavigationWorld
            .Instance
            .UnregisterRegion(
                this
            );
    }

    private void OnValidate()
    {
        width =
            Mathf.Max(
                1f,
                width
            );

        height =
            Mathf.Max(
                1f,
                height
            );

        cellSize =
            Mathf.Max(
                0.1f,
                cellSize
            );

        agentRadius =
            Mathf.Max(
                0f,
                agentRadius
            );

        obstacleClearancePadding =
            Mathf.Max(
                0f,
                obstacleClearancePadding
            );

        RecalculateDimensions();
    }

    public void Bake()
    {
        RecalculateDimensions();

        int totalCells =
            columns *
            rows;

        walkableCells =
            new bool[
                totalCells
            ];

        for (int y = 0;
             y < rows;
             y++)
        {
            for (int x = 0;
                 x < columns;
                 x++)
            {
                Vector2 worldPosition =
                    GridToWorld(
                        x,
                        y
                    );

                bool blocked =
                    IsWorldPositionBlocked(
                        worldPosition
                    );

                SetWalkable(
                    x,
                    y,
                    !blocked
                );
            }
        }
    }

    private void RecalculateDimensions()
    {
        columns =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    width /
                    cellSize
                )
            );

        rows =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    height /
                    cellSize
                )
            );
    }


    private ContactFilter2D GetNavigationObstacleFilter()
    {
        ContactFilter2D filter =
            new ContactFilter2D();

        filter.SetLayerMask(
            obstacleLayers
        );

        // Navigation only considers solid obstacles.
        // IndoorZones and other triggers are ignored.
        filter.useTriggers = false;

        return filter;
    }



    private bool IsWorldPositionBlocked(
        Vector2 worldPosition)
    {
        ContactFilter2D filter =
            GetNavigationObstacleFilter();

        float navigationRadius =
            NavigationRadius;

        if (navigationRadius <= 0f)
        {
            return Physics2D.OverlapPoint(
                worldPosition,
                filter,
                obstacleOverlapResults
            ) > 0;
        }

        return Physics2D.OverlapCircle(
            worldPosition,
            navigationRadius,
            filter,
            obstacleOverlapResults
        ) > 0;
    }


    public bool ContainsWorldPosition(
        Vector2 worldPosition)
    {
        Vector2 min =
            BottomLeft;

        Vector2 max =
            min +
            new Vector2(
                width,
                height
            );

        return
            worldPosition.x >= min.x &&
            worldPosition.x <= max.x &&
            worldPosition.y >= min.y &&
            worldPosition.y <= max.y;
    }

    public bool TryWorldToGrid(
        Vector2 worldPosition,
        out int x,
        out int y)
    {
        Vector2 local =
            worldPosition -
            BottomLeft;

        x =
            Mathf.FloorToInt(
                local.x /
                cellSize
            );

        y =
            Mathf.FloorToInt(
                local.y /
                cellSize
            );

        return IsValidCell(
            x,
            y
        );
    }

    public Vector2 GridToWorld(
        int x,
        int y)
    {
        Vector2 bottomLeft =
            BottomLeft;

        return
            bottomLeft +
            new Vector2(
                (
                    x +
                    0.5f
                ) *
                cellSize,

                (
                    y +
                    0.5f
                ) *
                cellSize
            );
    }

    public bool IsValidCell(
        int x,
        int y)
    {
        return
            x >= 0 &&
            x < columns &&
            y >= 0 &&
            y < rows;
    }

    public bool IsWalkable(
        int x,
        int y)
    {
        if (!IsValidCell(
                x,
                y))
        {
            return false;
        }

        EnsureGridExists();

        return walkableCells[
            ToIndex(
                x,
                y
            )
        ];
    }

    public NavigationCell GetCell(
        int x,
        int y)
    {
        return new NavigationCell(
            x,
            y,
            GridToWorld(
                x,
                y
            ),
            IsWalkable(
                x,
                y
            )
        );
    }

    public bool TryGetClosestWalkableCell(
        Vector2 worldPosition,
        out NavigationCell result,
        int maximumSearchRadius = 8)
    {
        result =
            default;

        if (!TryWorldToGrid(
                worldPosition,
                out int centerX,
                out int centerY))
        {
            return false;
        }

        if (IsWalkable(
                centerX,
                centerY))
        {
            result =
                GetCell(
                    centerX,
                    centerY
                );

            return true;
        }

        for (int radius = 1;
             radius <= maximumSearchRadius;
             radius++)
        {
            float bestDistance =
                float.MaxValue;

            bool found =
                false;

            NavigationCell best =
                default;

            for (int y =
                     centerY - radius;
                 y <=
                     centerY + radius;
                 y++)
            {
                for (int x =
                         centerX - radius;
                     x <=
                         centerX + radius;
                     x++)
                {
                    if (!IsValidCell(
                            x,
                            y))
                    {
                        continue;
                    }

                    bool onEdge =
                        x ==
                        centerX - radius ||
                        x ==
                        centerX + radius ||
                        y ==
                        centerY - radius ||
                        y ==
                        centerY + radius;

                    if (!onEdge)
                        continue;

                    if (!IsWalkable(
                            x,
                            y))
                    {
                        continue;
                    }

                    NavigationCell candidate =
                        GetCell(
                            x,
                            y
                        );

                    float distance =
                        (
                            candidate.WorldPosition -
                            worldPosition
                        ).sqrMagnitude;

                    if (distance >=
                        bestDistance)
                    {
                        continue;
                    }

                    bestDistance =
                        distance;

                    best =
                        candidate;

                    found =
                        true;
                }
            }

            if (found)
            {
                result =
                    best;

                return true;
            }
        }

        return false;
    }


    public bool IsDirectPathClear(
        Vector2 from,
        Vector2 to,
        float radiusOverride = -1f)
    {
        float radius =
            radiusOverride >= 0f
                ? radiusOverride
                : NavigationRadius;

        Vector2 direction =
            to - from;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction.Normalize();

        ContactFilter2D filter =
            GetNavigationObstacleFilter();

        if (radius <= 0f)
        {
            int hitCount =
                Physics2D.Raycast(
                    from,
                    direction,
                    filter,
                    obstacleCastResults,
                    distance
                );

            return hitCount == 0;
        }

        int circleHitCount =
            Physics2D.CircleCast(
                from,
                radius,
                direction,
                filter,
                obstacleCastResults,
                distance
            );

        return circleHitCount == 0;
    }


    private void EnsureGridExists()
    {
        int expectedSize =
            columns *
            rows;

        if (walkableCells != null &&
            walkableCells.Length ==
            expectedSize)
        {
            return;
        }

        Bake();
    }

    private void SetWalkable(
        int x,
        int y,
        bool walkable)
    {
        if (!IsValidCell(
                x,
                y))
        {
            return;
        }

        walkableCells[
            ToIndex(
                x,
                y
            )
        ] =
            walkable;
    }

    private int ToIndex(
        int x,
        int y)
    {
        return
            y *
            columns +
            x;
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        RecalculateDimensions();

        if (drawRegionBounds)
        {
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(
                    width,
                    height,
                    0f
                )
            );
        }

        if (!drawGrid &&
            !drawBlockedCells)
        {
            return;
        }

        bool hasRuntimeGrid =
            walkableCells != null &&
            walkableCells.Length ==
            columns * rows;

        for (int y = 0;
             y < rows;
             y++)
        {
            for (int x = 0;
                 x < columns;
                 x++)
            {
                Vector2 position =
                    GridToWorld(
                        x,
                        y
                    );

                if (drawGrid)
                {
                    Gizmos.DrawWireCube(
                        position,
                        new Vector3(
                            cellSize,
                            cellSize,
                            0f
                        )
                    );
                }

                if (!drawBlockedCells)
                    continue;

                bool walkable;

                if (hasRuntimeGrid)
                {
                    walkable =
                        IsWalkable(
                            x,
                            y
                        );
                }
                else
                {
                    walkable =
                        !IsWorldPositionBlocked(
                            position
                        );
                }

                if (walkable)
                    continue;

                Gizmos.DrawCube(
                    position,
                    new Vector3(
                        cellSize *
                        0.75f,

                        cellSize *
                        0.75f,

                        0.01f
                    )
                );
            }
        }
    }

#endif
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry och spatial navigation-world.
///
/// NavigationWorld delar upp världen i regelbundna
/// NavigationRegions och känner till hur regionerna sitter ihop.
///
/// Ansvar:
/// - registrera aktiva NavigationRegions
/// - spatial region-lookup
/// - world/grid region-koordinater
/// - hitta grannregions
/// - automatiskt upptäcka walkable portals mellan regions
///
/// NavigationWorld ansvarar INTE för:
/// - lokal A*
/// - Rigidbody movement
/// - NPC AI
/// - path scheduling
/// - grid baking
///
/// Senare används portal-grafen av en high-level world pathfinder.
/// </summary>
public sealed class NavigationWorld :
    MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    private static NavigationWorld instance;

    public static NavigationWorld Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance =
                FindFirstObjectByType<
                    NavigationWorld>();

            if (instance != null)
                return instance;

            GameObject runtimeObject =
                new GameObject(
                    "Navigation World Runtime"
                );

            instance =
                runtimeObject.AddComponent<
                    NavigationWorld>();

            return instance;
        }
    }

    public static bool HasInstance =>
        instance != null;

    // =========================================================
    // WORLD GRID
    // =========================================================

    [Header("World Grid")]

    [SerializeField]
    [Tooltip(
    "Om NavigationWorld automatiskt ska använda den första " +
    "registrerade NavigationRegion som källa för Region Size " +
    "och World Origin. Rekommenderas."
)]
    private bool autoConfigureFromRegions =
    true;

    [SerializeField]
    [Min(1f)]
    [Tooltip(
        "Standardstorleken på en navigation-region i world units."
    )]
    private float regionSize =
        32f;

    [SerializeField]
    [Tooltip(
        "World-positionen för centrum av region-coordinate (0,0)."
    )]
    private Vector2 worldOrigin =
        Vector2.zero;

    public float RegionSize =>
        regionSize;

    public Vector2 WorldOrigin =>
        worldOrigin;

    // =========================================================
    // PORTAL SETTINGS
    // =========================================================

    [Header("Region Portals")]

    [SerializeField]
    [Min(0.0001f)]
    [Tooltip(
        "Maximal tillåten skillnad mellan två grannregions " +
        "Cell Size för att automatiska portals ska byggas."
    )]
    private float cellSizeTolerance =
        0.001f;

    [SerializeField]
    [Min(1)]
    [Tooltip(
        "Minsta antal sammanhängande walkable cellpar som krävs " +
        "för att en passage ska räknas som en portal."
    )]
    private int minimumPortalCells =
        1;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool drawWorldOrigin =
        true;

    [SerializeField]
    private bool drawRegionCenters =
        true;

    [SerializeField]
    private bool drawPortals =
        true;

    [SerializeField]
    [Min(0.01f)]
    private float portalMarkerRadius =
        0.2f;

    // =========================================================
    // REGION DATA
    // =========================================================

    private readonly List<
        NavigationRegion>
        regions =
            new();

    private readonly Dictionary<
        Vector2Int,
        NavigationRegion>
        regionsByCoordinate =
            new();

    private readonly Dictionary<
        NavigationRegion,
        Vector2Int>
        coordinatesByRegion =
            new();

    public IReadOnlyList<
        NavigationRegion>
        Regions =>
            regions;

    public int RegionCount =>
        regions.Count;

    // =========================================================
    // PORTAL DATA
    // =========================================================

    private readonly List<
        NavigationRegionPortal>
        portals =
            new();

    private readonly Dictionary<
        NavigationRegion,
        List<NavigationRegionPortal>>
        portalsByRegion =
            new();

    public IReadOnlyList<
        NavigationRegionPortal>
        Portals =>
            portals;

    public int PortalCount =>
        portals.Count;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(
                gameObject
            );

            return;
        }

        instance =
            this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance =
                null;
        }
    }

    private void OnValidate()
    {
        regionSize =
            Mathf.Max(
                1f,
                regionSize
            );

        cellSizeTolerance =
            Mathf.Max(
                0.0001f,
                cellSizeTolerance
            );

        minimumPortalCells =
            Mathf.Max(
                1,
                minimumPortalCells
            );

        portalMarkerRadius =
            Mathf.Max(
                0.01f,
                portalMarkerRadius
            );
    }

    // =========================================================
    // REGISTRATION
    // =========================================================
    private void TryAutoConfigureWorldGrid(
    NavigationRegion region)
    {
        if (!autoConfigureFromRegions ||
            region == null)
        {
            return;
        }

        /*
         * Bara den FÖRSTA regionen definierar gridet.
         *
         * Därefter är World Origin och Region Size stabila,
         * oavsett i vilken ordning resten av regionerna registreras.
         */
        if (regions.Count > 0 ||
            regionsByCoordinate.Count > 0)
        {
            return;
        }

        float resolvedRegionSize =
            region.Width;

        if (resolvedRegionSize <= 0.001f)
        {
            Debug.LogError(
                $"NavigationWorld kunde inte konfigurera sitt grid från " +
                $"'{region.name}' eftersom regionens Width är ogiltig.",
                region
            );

            return;
        }

        regionSize =
            resolvedRegionSize;

        /*
         * Den första registrerade regionens centrum blir coordinate
         * (0,0).
         *
         * Det eliminerar behovet av att manuellt räkna ut ett
         * World Origin för zoner med jämnt/udda antal regions.
         */
        worldOrigin =
            region.transform.position;

        if (Application.isPlaying)
        {
            Debug.Log(
                $"NavigationWorld auto-configured | " +
                $"Region Size={regionSize} | " +
                $"World Origin={worldOrigin} | " +
                $"Source={region.name}",
                this
            );
        }
    }
    public void RegisterRegion(
    NavigationRegion region)
    {
        if (region == null)
            return;

        /*
         * Första aktiva NavigationRegion får definiera det globala
         * region-gridet automatiskt.
         *
         * Därmed behöver NavigationWorld.RegionSize och WorldOrigin
         * inte manuellt hållas synkade med NavigationZone.
         */
        TryAutoConfigureWorldGrid(
            region
        );

        /*
         * Regionen kan ha flyttats sedan föregående
         * registrering.
         */
        RemoveRegionCoordinateMapping(
            region
        );

        Vector2Int coordinate =
            WorldToRegionCoordinate(
                region.transform.position
            );

        if (regionsByCoordinate
                .TryGetValue(
                    coordinate,
                    out NavigationRegion existingRegion) &&
            existingRegion != null &&
            existingRegion != region)
        {
            Debug.LogError(
                $"NavigationWorld: Region '{region.name}' försöker " +
                $"registrera coordinate {coordinate}, men den ägs " +
                $"redan av '{existingRegion.name}'. " +
                $"Kontrollera att NavigationRegions ligger på samma " +
                $"region-grid.",
                region
            );

            return;
        }

        regionsByCoordinate[
            coordinate
        ] =
            region;

        coordinatesByRegion[
            region
        ] =
            coordinate;

        if (!regions.Contains(
                region))
        {
            regions.Add(
                region
            );
        }

        if (!portalsByRegion.ContainsKey(
                region))
        {
            portalsByRegion[
                region
            ] =
                new List<
                    NavigationRegionPortal>();
        }

        /*
         * När en ny region dyker upp kan nya grannar nu
         * existera.
         *
         * Bygg om just denna regions portals.
         */
        RebuildPortalsForRegion(
            region
        );
    }

    public void UnregisterRegion(
        NavigationRegion region)
    {
        if (region == null)
            return;

        RemovePortalsForRegion(
            region
        );

        RemoveRegionCoordinateMapping(
            region
        );

        portalsByRegion.Remove(
            region
        );

        regions.Remove(
            region
        );
    }

    private void RemoveRegionCoordinateMapping(
        NavigationRegion region)
    {
        if (region == null)
            return;

        if (!coordinatesByRegion
                .TryGetValue(
                    region,
                    out Vector2Int coordinate))
        {
            return;
        }

        coordinatesByRegion.Remove(
            region
        );

        if (regionsByCoordinate
                .TryGetValue(
                    coordinate,
                    out NavigationRegion mappedRegion) &&
            mappedRegion == region)
        {
            regionsByCoordinate.Remove(
                coordinate
            );
        }
    }

    // =========================================================
    // WORLD / REGION COORDINATES
    // =========================================================

    public Vector2Int WorldToRegionCoordinate(
        Vector2 worldPosition)
    {
        Vector2 relative =
            worldPosition -
            worldOrigin;

        float halfRegion =
            regionSize *
            0.5f;

        int x =
            Mathf.FloorToInt(
                (
                    relative.x +
                    halfRegion
                ) /
                regionSize
            );

        int y =
            Mathf.FloorToInt(
                (
                    relative.y +
                    halfRegion
                ) /
                regionSize
            );

        return new Vector2Int(
            x,
            y
        );
    }

    public Vector2 RegionCoordinateToWorldCenter(
        Vector2Int coordinate)
    {
        return
            worldOrigin +
            new Vector2(
                coordinate.x *
                regionSize,

                coordinate.y *
                regionSize
            );
    }

    // =========================================================
    // REGION LOOKUP
    // =========================================================

    public bool TryGetRegion(
        Vector2Int coordinate,
        out NavigationRegion region)
    {
        if (regionsByCoordinate
                .TryGetValue(
                    coordinate,
                    out region) &&
            region != null &&
            region.isActiveAndEnabled)
        {
            return true;
        }

        region =
            null;

        return false;
    }

    public NavigationRegion GetRegion(
        Vector2Int coordinate)
    {
        TryGetRegion(
            coordinate,
            out NavigationRegion region
        );

        return region;
    }

    public bool TryGetRegionAt(
        Vector2 worldPosition,
        out NavigationRegion region)
    {
        Vector2Int coordinate =
            WorldToRegionCoordinate(
                worldPosition
            );

        if (!TryGetRegion(
                coordinate,
                out region))
        {
            return false;
        }

        if (!region
                .ContainsWorldPosition(
                    worldPosition))
        {
            region =
                null;

            return false;
        }

        return true;
    }

    public NavigationRegion GetRegionAt(
        Vector2 worldPosition)
    {
        TryGetRegionAt(
            worldPosition,
            out NavigationRegion region
        );

        return region;
    }

    public bool TryGetRegionCoordinate(
        NavigationRegion region,
        out Vector2Int coordinate)
    {
        if (region == null)
        {
            coordinate =
                default;

            return false;
        }

        return coordinatesByRegion
            .TryGetValue(
                region,
                out coordinate
            );
    }

    public Vector2Int GetRegionCoordinate(
        NavigationRegion region)
    {
        if (TryGetRegionCoordinate(
                region,
                out Vector2Int coordinate))
        {
            return coordinate;
        }

        if (region != null)
        {
            return WorldToRegionCoordinate(
                region.transform.position
            );
        }

        return Vector2Int.zero;
    }

    // =========================================================
    // NEIGHBOURS
    // =========================================================

    public bool TryGetNeighbour(
        NavigationRegion region,
        Vector2Int direction,
        out NavigationRegion neighbour)
    {
        neighbour =
            null;

        if (!TryGetRegionCoordinate(
                region,
                out Vector2Int coordinate))
        {
            return false;
        }

        return TryGetRegion(
            coordinate +
            direction,
            out neighbour
        );
    }

    public NavigationRegion GetNeighbour(
        NavigationRegion region,
        Vector2Int direction)
    {
        TryGetNeighbour(
            region,
            direction,
            out NavigationRegion neighbour
        );

        return neighbour;
    }

    // =========================================================
    // SEAMLESS WORLD DIRECT-PATH QUERY
    // =========================================================

    /// <summary>
    /// Kontrollerar att en rak väg:
    ///
    /// 1. är fri från blockerande World-geometri med samma
    ///    navigation-clearance som en NavigationRegion använder,
    ///
    /// OCH
    ///
    /// 2. hela sträckan ligger inne i registrerade NavigationRegions.
    ///
    /// Det gör att direct movement och path smoothing får vara
    /// sömlösa över region-borders utan att kunna gena genom ett
    /// hål där ingen NavigationRegion finns.
    /// </summary>
    public bool IsDirectPathClear(
        Vector2 from,
        Vector2 to,
        NavigationRegion clearanceRegion = null)
    {
        if (clearanceRegion == null)
        {
            if (!TryGetRegionAt(
                    from,
                    out clearanceRegion))
            {
                return false;
            }
        }


        if (!clearanceRegion
                .IsDirectPathClear(
                    from,
                    to))
        {
            return false;
        }


        Vector2 delta =
            to -
            from;

        float distance =
            delta.magnitude;


        if (distance <=
            0.001f)
        {
            return
                TryGetRegionAt(
                    from,
                    out _
                );
        }


        /*
         * Halv cellstorlek gör samplet tätare än själva navigation-gridet.
         * Då kan vi inte råka hoppa över ett smalt gap mellan regions.
         */
        float sampleSpacing =
            Mathf.Max(
                0.05f,
                clearanceRegion.CellSize *
                0.5f
            );


        int steps =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    distance /
                    sampleSpacing
                )
            );


        for (int i = 0;
             i <= steps;
             i++)
        {
            float t =
                i /
                (float)steps;


            Vector2 sample =
                Vector2.Lerp(
                    from,
                    to,
                    t
                );


            if (!TryGetRegionAt(
                    sample,
                    out _))
            {
                return false;
            }
        }


        return true;
    }

    // =========================================================
    // PORTAL QUERY
    // =========================================================

    /// <summary>
    /// Returnerar alla portals som lämnar den angivna regionen.
    /// </summary>
    public IReadOnlyList<
        NavigationRegionPortal>
        GetPortals(
            NavigationRegion region)
    {
        if (region != null &&
            portalsByRegion
                .TryGetValue(
                    region,
                    out List<
                        NavigationRegionPortal>
                        regionPortals))
        {
            return regionPortals;
        }

        return
            System.Array.Empty<
                NavigationRegionPortal>();
    }

    /// <summary>
    /// Returnerar alla portals direkt mellan två specifika regions.
    ///
    /// Det kan finnas flera separata passager längs samma kant.
    /// </summary>
    public void GetPortalsBetween(
        NavigationRegion first,
        NavigationRegion second,
        List<NavigationRegionPortal> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (first == null ||
            second == null)
        {
            return;
        }

        if (!portalsByRegion
                .TryGetValue(
                    first,
                    out List<
                        NavigationRegionPortal>
                        firstPortals))
        {
            return;
        }

        for (int i = 0;
             i < firstPortals.Count;
             i++)
        {
            NavigationRegionPortal portal =
                firstPortals[i];

            if (portal == null)
                continue;

            if (!portal.Connects(
                    first,
                    second))
            {
                continue;
            }

            results.Add(
                portal
            );
        }
    }

    // =========================================================
    // PORTAL REBUILD
    // =========================================================

    public void RebuildAllPortals()
    {
        portals.Clear();

        portalsByRegion.Clear();

        for (int i = 0;
             i < regions.Count;
             i++)
        {
            NavigationRegion region =
                regions[i];

            if (region == null)
                continue;

            portalsByRegion[
                region
            ] =
                new List<
                    NavigationRegionPortal>();
        }

        /*
         * Bara east + north.
         *
         * Då behandlas varje regionpar exakt en gång.
         */
        for (int i = 0;
             i < regions.Count;
             i++)
        {
            NavigationRegion region =
                regions[i];

            if (region == null)
                continue;

            TryBuildPortalsWithNeighbour(
                region,
                Vector2Int.right
            );

            TryBuildPortalsWithNeighbour(
                region,
                Vector2Int.up
            );
        }
    }

    private void RebuildPortalsForRegion(
        NavigationRegion region)
    {
        if (region == null)
            return;

        RemovePortalsForRegion(
            region
        );

        EnsurePortalList(
            region
        );

        TryBuildPortalsWithNeighbour(
            region,
            Vector2Int.right
        );

        TryBuildPortalsWithNeighbour(
            region,
            Vector2Int.left
        );

        TryBuildPortalsWithNeighbour(
            region,
            Vector2Int.up
        );

        TryBuildPortalsWithNeighbour(
            region,
            Vector2Int.down
        );
    }

    private void TryBuildPortalsWithNeighbour(
        NavigationRegion region,
        Vector2Int direction)
    {
        if (!TryGetNeighbour(
                region,
                direction,
                out NavigationRegion neighbour))
        {
            return;
        }

        /*
         * Denna connection kan redan ha byggts från
         * grannens registrering.
         */
        if (HasAnyPortalBetween(
                region,
                neighbour))
        {
            return;
        }

        BuildPortalsBetween(
            region,
            neighbour,
            direction
        );
    }

    private bool HasAnyPortalBetween(
        NavigationRegion first,
        NavigationRegion second)
    {
        if (!portalsByRegion
                .TryGetValue(
                    first,
                    out List<
                        NavigationRegionPortal>
                        firstPortals))
        {
            return false;
        }

        for (int i = 0;
             i < firstPortals.Count;
             i++)
        {
            NavigationRegionPortal portal =
                firstPortals[i];

            if (portal != null &&
                portal.Connects(
                    first,
                    second))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // PORTAL GENERATION
    // =========================================================

    private void BuildPortalsBetween(
        NavigationRegion regionA,
        NavigationRegion regionB,
        Vector2Int directionFromA)
    {
        if (regionA == null ||
            regionB == null)
        {
            return;
        }

        float sizeDifference =
            Mathf.Abs(
                regionA.CellSize -
                regionB.CellSize
            );

        if (sizeDifference >
            cellSizeTolerance)
        {
            Debug.LogWarning(
                $"NavigationWorld: Kan inte automatiskt skapa portals " +
                $"mellan '{regionA.name}' och '{regionB.name}' eftersom " +
                $"deras Cell Size skiljer sig " +
                $"({regionA.CellSize} / {regionB.CellSize}).",
                this
            );

            return;
        }

        if (directionFromA ==
                Vector2Int.right ||
            directionFromA ==
                Vector2Int.left)
        {
            BuildVerticalEdgePortals(
                regionA,
                regionB,
                directionFromA
            );

            return;
        }

        if (directionFromA ==
                Vector2Int.up ||
            directionFromA ==
                Vector2Int.down)
        {
            BuildHorizontalEdgePortals(
                regionA,
                regionB,
                directionFromA
            );
        }
    }

    // =========================================================
    // EAST / WEST EDGE
    // =========================================================

    private void BuildVerticalEdgePortals(
        NavigationRegion regionA,
        NavigationRegion regionB,
        Vector2Int directionFromA)
    {
        int cellCount =
            Mathf.Min(
                regionA.Rows,
                regionB.Rows
            );

        int runStart =
            -1;

        for (int index = 0;
             index <= cellCount;
             index++)
        {
            bool open =
                index <
                cellCount &&
                IsVerticalEdgePairOpen(
                    regionA,
                    regionB,
                    directionFromA,
                    index
                );

            if (open)
            {
                if (runStart < 0)
                {
                    runStart =
                        index;
                }

                continue;
            }

            if (runStart < 0)
                continue;

            int runEnd =
                index -
                1;

            CreateVerticalPortalRun(
                regionA,
                regionB,
                directionFromA,
                runStart,
                runEnd
            );

            runStart =
                -1;
        }
    }

    private bool IsVerticalEdgePairOpen(
        NavigationRegion regionA,
        NavigationRegion regionB,
        Vector2Int directionFromA,
        int y)
    {
        int xA =
            directionFromA ==
            Vector2Int.right
                ? regionA.Columns - 1
                : 0;

        int xB =
            directionFromA ==
            Vector2Int.right
                ? 0
                : regionB.Columns - 1;

        if (!regionA.IsWalkable(
                xA,
                y) ||
            !regionB.IsWalkable(
                xB,
                y))
        {
            return false;
        }

        Vector2 pointA =
            regionA.GridToWorld(
                xA,
                y
            );

        Vector2 pointB =
            regionB.GridToWorld(
                xB,
                y
            );

        /*
         * Border-cellerna kan båda vara walkable men ett mycket
         * smalt hinder skulle tekniskt kunna ligga mellan dem.
         *
         * Kontrollera därför även själva övergången.
         */
        return
            regionA.IsDirectPathClear(
                pointA,
                pointB
            ) &&
            regionB.IsDirectPathClear(
                pointA,
                pointB
            );
    }

    private void CreateVerticalPortalRun(
    NavigationRegion regionA,
    NavigationRegion regionB,
    Vector2Int directionFromA,
    int startY,
    int endY)
    {
        int runLength =
            endY -
            startY +
            1;


        if (runLength <
            minimumPortalCells)
        {
            return;
        }


        int xA =
            directionFromA ==
            Vector2Int.right
                ? regionA.Columns - 1
                : 0;


        int xB =
            directionFromA ==
            Vector2Int.right
                ? 0
                : regionB.Columns - 1;


        /*
         * Behåll HELA det sammanhängande öppna spanet.
         *
         * Varje position mellan startY och endY motsvarar
         * ett cellpar som redan verifierats av
         * IsVerticalEdgePairOpen().
         */
        Vector2 pointAStart =
            regionA.GridToWorld(
                xA,
                startY
            );


        Vector2 pointAEnd =
            regionA.GridToWorld(
                xA,
                endY
            );


        Vector2 pointBStart =
            regionB.GridToWorld(
                xB,
                startY
            );


        Vector2 pointBEnd =
            regionB.GridToWorld(
                xB,
                endY
            );


        float width =
            runLength *
            Mathf.Min(
                regionA.CellSize,
                regionB.CellSize
            );


        AddPortal(
            new NavigationRegionPortal(
                regionA,
                regionB,

                pointAStart,
                pointAEnd,

                pointBStart,
                pointBEnd,

                runLength,
                width,

                directionFromA
            )
        );
    }

    // =========================================================
    // NORTH / SOUTH EDGE
    // =========================================================

    private void BuildHorizontalEdgePortals(
        NavigationRegion regionA,
        NavigationRegion regionB,
        Vector2Int directionFromA)
    {
        int cellCount =
            Mathf.Min(
                regionA.Columns,
                regionB.Columns
            );

        int runStart =
            -1;

        for (int index = 0;
             index <= cellCount;
             index++)
        {
            bool open =
                index <
                cellCount &&
                IsHorizontalEdgePairOpen(
                    regionA,
                    regionB,
                    directionFromA,
                    index
                );

            if (open)
            {
                if (runStart < 0)
                {
                    runStart =
                        index;
                }

                continue;
            }

            if (runStart < 0)
                continue;

            int runEnd =
                index -
                1;

            CreateHorizontalPortalRun(
                regionA,
                regionB,
                directionFromA,
                runStart,
                runEnd
            );

            runStart =
                -1;
        }
    }

    private bool IsHorizontalEdgePairOpen(
        NavigationRegion regionA,
        NavigationRegion regionB,
        Vector2Int directionFromA,
        int x)
    {
        int yA =
            directionFromA ==
            Vector2Int.up
                ? regionA.Rows - 1
                : 0;

        int yB =
            directionFromA ==
            Vector2Int.up
                ? 0
                : regionB.Rows - 1;

        if (!regionA.IsWalkable(
                x,
                yA) ||
            !regionB.IsWalkable(
                x,
                yB))
        {
            return false;
        }

        Vector2 pointA =
            regionA.GridToWorld(
                x,
                yA
            );

        Vector2 pointB =
            regionB.GridToWorld(
                x,
                yB
            );

        return
            regionA.IsDirectPathClear(
                pointA,
                pointB
            ) &&
            regionB.IsDirectPathClear(
                pointA,
                pointB
            );
    }

    private void CreateHorizontalPortalRun(
    NavigationRegion regionA,
    NavigationRegion regionB,
    Vector2Int directionFromA,
    int startX,
    int endX)
    {
        int runLength =
            endX -
            startX +
            1;


        if (runLength <
            minimumPortalCells)
        {
            return;
        }


        int yA =
            directionFromA ==
            Vector2Int.up
                ? regionA.Rows - 1
                : 0;


        int yB =
            directionFromA ==
            Vector2Int.up
                ? 0
                : regionB.Rows - 1;


        Vector2 pointAStart =
            regionA.GridToWorld(
                startX,
                yA
            );


        Vector2 pointAEnd =
            regionA.GridToWorld(
                endX,
                yA
            );


        Vector2 pointBStart =
            regionB.GridToWorld(
                startX,
                yB
            );


        Vector2 pointBEnd =
            regionB.GridToWorld(
                endX,
                yB
            );


        float width =
            runLength *
            Mathf.Min(
                regionA.CellSize,
                regionB.CellSize
            );


        AddPortal(
            new NavigationRegionPortal(
                regionA,
                regionB,

                pointAStart,
                pointAEnd,

                pointBStart,
                pointBEnd,

                runLength,
                width,

                directionFromA
            )
        );
    }

    // =========================================================
    // PORTAL STORAGE
    // =========================================================

    private void AddPortal(
        NavigationRegionPortal portal)
    {
        if (portal == null ||
            portal.RegionA == null ||
            portal.RegionB == null)
        {
            return;
        }

        portals.Add(
            portal
        );

        EnsurePortalList(
            portal.RegionA
        );

        EnsurePortalList(
            portal.RegionB
        );

        portalsByRegion[
            portal.RegionA
        ].Add(
            portal
        );

        portalsByRegion[
            portal.RegionB
        ].Add(
            portal
        );
    }

    private void EnsurePortalList(
        NavigationRegion region)
    {
        if (region == null)
            return;

        if (portalsByRegion.ContainsKey(
                region))
        {
            return;
        }

        portalsByRegion[
            region
        ] =
            new List<
                NavigationRegionPortal>();
    }

    private void RemovePortalsForRegion(
        NavigationRegion region)
    {
        if (region == null)
            return;

        for (int i =
                 portals.Count - 1;
             i >= 0;
             i--)
        {
            NavigationRegionPortal portal =
                portals[i];

            if (portal == null ||
                !portal.Connects(
                    region))
            {
                continue;
            }

            portals.RemoveAt(
                i
            );

            if (portal.RegionA != null &&
                portalsByRegion
                    .TryGetValue(
                        portal.RegionA,
                        out List<
                            NavigationRegionPortal>
                            portalsA))
            {
                portalsA.Remove(
                    portal
                );
            }

            if (portal.RegionB != null &&
                portalsByRegion
                    .TryGetValue(
                        portal.RegionB,
                        out List<
                            NavigationRegionPortal>
                            portalsB))
            {
                portalsB.Remove(
                    portal
                );
            }
        }
    }

    // =========================================================
    // ALIGNMENT
    // =========================================================

    public Vector2 GetRegionAlignmentError(
        NavigationRegion region)
    {
        if (region == null)
            return Vector2.zero;

        Vector2Int coordinate =
            WorldToRegionCoordinate(
                region.transform.position
            );

        Vector2 expectedPosition =
            RegionCoordinateToWorldCenter(
                coordinate
            );

        return
            (Vector2)region.transform.position -
            expectedPosition;
    }

    public bool IsRegionAligned(
        NavigationRegion region,
        float tolerance = 0.01f)
    {
        Vector2 error =
            GetRegionAlignmentError(
                region
            );

        return
            error.sqrMagnitude <=
            tolerance *
            tolerance;
    }

#if UNITY_EDITOR

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        float safeRegionSize =
            Mathf.Max(
                1f,
                regionSize
            );

        if (drawWorldOrigin)
        {
            Gizmos.DrawWireCube(
                worldOrigin,
                new Vector3(
                    safeRegionSize,
                    safeRegionSize,
                    0f
                )
            );

            float markerSize =
                Mathf.Max(
                    0.25f,
                    safeRegionSize *
                    0.025f
                );

            Gizmos.DrawLine(
                worldOrigin +
                Vector2.left *
                markerSize,
                worldOrigin +
                Vector2.right *
                markerSize
            );

            Gizmos.DrawLine(
                worldOrigin +
                Vector2.down *
                markerSize,
                worldOrigin +
                Vector2.up *
                markerSize
            );
        }

        if (drawRegionCenters)
        {
            for (int i = 0;
                 i < regions.Count;
                 i++)
            {
                NavigationRegion region =
                    regions[i];

                if (region == null)
                    continue;

                Gizmos.DrawWireSphere(
                    region.transform.position,
                    portalMarkerRadius *
                    0.75f
                );
            }
        }

        if (!drawPortals)
            return;

        for (int i = 0;
             i < portals.Count;
             i++)
        {
            NavigationRegionPortal portal =
                portals[i];

            if (portal == null)
                continue;

            /*
             * Visa hela portal-spanet på båda regionsidorna.
             */

            Gizmos.DrawLine(
                portal.PointAStart,
                portal.PointAEnd
            );


            Gizmos.DrawLine(
                portal.PointBStart,
                portal.PointBEnd
            );


            /*
             * Visa även början/slutet av den traverserbara
             * regionövergången.
             */
            Gizmos.DrawLine(
                portal.PointAStart,
                portal.PointBStart
            );


            Gizmos.DrawLine(
                portal.PointAEnd,
                portal.PointBEnd
            );

            /*
             * Visar de två säkra cellpunkterna.
             */
            Gizmos.DrawWireSphere(
                portal.PointA,
                portalMarkerRadius
            );

            Gizmos.DrawWireSphere(
                portal.PointB,
                portalMarkerRadius
            );

            /*
             * Linjen representerar själva övergången mellan
             * regionerna.
             */
            Gizmos.DrawLine(
                portal.PointA,
                portal.PointB
            );

            /*
             * Lite större markering i portalens centrum.
             */
            Gizmos.DrawWireSphere(
                portal.WorldCenter,
                portalMarkerRadius *
                1.35f
            );
        }
    }

#endif
}
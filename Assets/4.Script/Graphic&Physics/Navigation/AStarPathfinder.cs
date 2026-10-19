using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Seamless A* genom hela NavigationWorld.
///
/// NavigationRegions är endast chunks för bakad navigation-data.
/// En region-border är INTE en waypoint och INTE en portal.
/// Om en granncell ligger i nästa NavigationRegion fortsätter
/// samma A*-search helt enkelt in i den regionen.
///
/// Behåller även den gamla single-region-overloaden för kompatibilitet.
/// </summary>
public static class AStarPathfinder
{
    private const float DiagonalCost =
        1.41421356f;

    /*
     * Säkerhetsgräns så en trasig/omöjlig extrem request inte
     * kan söka obegränsat genom hela världen i samma frame.
     *
     * Med 80x80 cells per 32x32-region motsvarar 250 000 cells
     * väldigt många regioner och är gott om marginal för nuvarande RöK.
     */
    private const int MaximumExpandedNodes =
        250000;

    private const float CellSizeTolerance =
        0.001f;


    // =========================================================
    // WORLD CELL KEY
    // =========================================================

    private readonly struct WorldCellKey :
        IEquatable<WorldCellKey>
    {
        public readonly NavigationRegion Region;
        public readonly int X;
        public readonly int Y;

        public WorldCellKey(
            NavigationRegion region,
            int x,
            int y)
        {
            Region =
                region;

            X =
                x;

            Y =
                y;
        }

        public bool Equals(
            WorldCellKey other)
        {
            return
                Region == other.Region &&
                X == other.X &&
                Y == other.Y;
        }

        public override bool Equals(
            object obj)
        {
            return
                obj is WorldCellKey other &&
                Equals(
                    other
                );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash =
                    Region != null
                        ? Region.GetInstanceID()
                        : 0;

                hash =
                    hash *
                    397 ^
                    X;

                hash =
                    hash *
                    397 ^
                    Y;

                return hash;
            }
        }
    }


    // =========================================================
    // SEARCH DATA
    // =========================================================

    private struct SearchRecord
    {
        public float GCost;
        public float HCost;

        public int ParentIndex;

        public int HeapIndex;

        public bool Closed;

        public float FCost =>
            GCost +
            HCost;
    }


    /// <summary>
    /// Återanvändbar workspace så vi behåller fördelen från
    /// gamla pathfindern: arrays/heap återanvänds mellan searches.
    ///
    /// Skillnaden är att ett node-index nu representerar:
    ///
    /// NavigationRegion + local X + local Y
    ///
    /// istället för bara X/Y i en enda region.
    /// </summary>
    private sealed class SearchWorkspace
    {
        private readonly Dictionary<
            WorldCellKey,
            int>
            nodeIndices =
                new();

        private WorldCellKey[] keys =
            Array.Empty<WorldCellKey>();

        private Vector2[] positions =
            Array.Empty<Vector2>();

        private SearchRecord[] records =
            Array.Empty<SearchRecord>();

        private int[] heap =
            Array.Empty<int>();

        private int nodeCount;
        private int heapCount;


        public int NodeCount =>
            nodeCount;

        public int HeapCount =>
            heapCount;


        public void BeginSearch()
        {
            nodeIndices.Clear();

            nodeCount =
                0;

            heapCount =
                0;
        }


        public int GetOrCreateNode(
            WorldCellKey key,
            Vector2 worldPosition)
        {
            if (nodeIndices.TryGetValue(
                    key,
                    out int existingIndex))
            {
                return existingIndex;
            }

            EnsureNodeCapacity(
                nodeCount + 1
            );

            int index =
                nodeCount;

            nodeCount++;

            keys[index] =
                key;

            positions[index] =
                worldPosition;

            records[index] =
                new SearchRecord
                {
                    GCost =
                        float.PositiveInfinity,

                    HCost =
                        0f,

                    ParentIndex =
                        -1,

                    HeapIndex =
                        -1,

                    Closed =
                        false
                };

            nodeIndices.Add(
                key,
                index
            );

            return index;
        }


        public ref SearchRecord GetRecord(
            int index)
        {
            return
                ref records[index];
        }


        public WorldCellKey GetKey(
            int index)
        {
            return
                keys[index];
        }


        public Vector2 GetPosition(
            int index)
        {
            return
                positions[index];
        }


        // =====================================================
        // HEAP
        // =====================================================

        public void AddOrUpdateOpen(
            int nodeIndex)
        {
            ref SearchRecord record =
                ref records[nodeIndex];

            /*
             * Redan i heapen:
             * GCost har minskat, så node behöver bara bubbla upp.
             */
            if (record.HeapIndex >= 0)
            {
                BubbleUp(
                    record.HeapIndex
                );

                return;
            }

            EnsureHeapCapacity(
                heapCount + 1
            );

            int heapIndex =
                heapCount;

            heap[heapIndex] =
                nodeIndex;

            record.HeapIndex =
                heapIndex;

            heapCount++;

            BubbleUp(
                heapIndex
            );
        }


        public int PopBest()
        {
            if (heapCount <= 0)
                return -1;

            int bestNode =
                heap[0];

            records[bestNode]
                .HeapIndex =
                    -1;

            heapCount--;

            if (heapCount <= 0)
            {
                return bestNode;
            }

            int replacement =
                heap[heapCount];

            heap[0] =
                replacement;

            records[replacement]
                .HeapIndex =
                    0;

            BubbleDown(
                0
            );

            return bestNode;
        }


        private void BubbleUp(
            int heapIndex)
        {
            while (heapIndex > 0)
            {
                int parentIndex =
                    (
                        heapIndex -
                        1
                    ) /
                    2;

                int currentNode =
                    heap[heapIndex];

                int parentNode =
                    heap[parentIndex];

                if (!IsBetter(
                        currentNode,
                        parentNode))
                {
                    break;
                }

                SwapHeapEntries(
                    heapIndex,
                    parentIndex
                );

                heapIndex =
                    parentIndex;
            }
        }


        private void BubbleDown(
            int heapIndex)
        {
            while (true)
            {
                int leftChild =
                    heapIndex *
                    2 +
                    1;

                if (leftChild >=
                    heapCount)
                {
                    return;
                }

                int rightChild =
                    leftChild +
                    1;

                int bestChild =
                    leftChild;

                if (rightChild <
                        heapCount &&
                    IsBetter(
                        heap[rightChild],
                        heap[leftChild]))
                {
                    bestChild =
                        rightChild;
                }

                if (!IsBetter(
                        heap[bestChild],
                        heap[heapIndex]))
                {
                    return;
                }

                SwapHeapEntries(
                    heapIndex,
                    bestChild
                );

                heapIndex =
                    bestChild;
            }
        }


        private bool IsBetter(
            int firstNodeIndex,
            int secondNodeIndex)
        {
            SearchRecord first =
                records[firstNodeIndex];

            SearchRecord second =
                records[secondNodeIndex];

            float firstF =
                first.FCost;

            float secondF =
                second.FCost;

            if (firstF <
                secondF)
            {
                return true;
            }

            if (firstF >
                secondF)
            {
                return false;
            }

            /*
             * Samma FCost:
             * föredra lägre heuristic.
             *
             * Samma princip som gamla pathfindern.
             */
            return
                first.HCost <
                second.HCost;
        }


        private void SwapHeapEntries(
            int firstHeapIndex,
            int secondHeapIndex)
        {
            int firstNode =
                heap[firstHeapIndex];

            int secondNode =
                heap[secondHeapIndex];

            heap[firstHeapIndex] =
                secondNode;

            heap[secondHeapIndex] =
                firstNode;

            records[firstNode]
                .HeapIndex =
                    secondHeapIndex;

            records[secondNode]
                .HeapIndex =
                    firstHeapIndex;
        }


        // =====================================================
        // CAPACITY
        // =====================================================

        private void EnsureNodeCapacity(
            int requiredCapacity)
        {
            requiredCapacity =
                Mathf.Max(
                    1,
                    requiredCapacity
                );

            if (records.Length >=
                requiredCapacity)
            {
                return;
            }

            int newCapacity =
                GetExpandedCapacity(
                    records.Length,
                    requiredCapacity
                );

            Array.Resize(
                ref keys,
                newCapacity
            );

            Array.Resize(
                ref positions,
                newCapacity
            );

            Array.Resize(
                ref records,
                newCapacity
            );
        }


        private void EnsureHeapCapacity(
            int requiredCapacity)
        {
            if (heap.Length >=
                requiredCapacity)
            {
                return;
            }

            int newCapacity =
                GetExpandedCapacity(
                    heap.Length,
                    requiredCapacity
                );

            Array.Resize(
                ref heap,
                newCapacity
            );
        }


        private static int GetExpandedCapacity(
            int currentCapacity,
            int requiredCapacity)
        {
            int capacity =
                Mathf.Max(
                    64,
                    currentCapacity
                );

            while (capacity <
                   requiredCapacity)
            {
                capacity *=
                    2;
            }

            return capacity;
        }
    }


    private static readonly
        SearchWorkspace Workspace =
            new();


    // =========================================================
    // DIRECTIONS
    // =========================================================

    private static readonly Vector2Int[]
        CardinalDirections =
        {
            new Vector2Int(
                1,
                0
            ),

            new Vector2Int(
                -1,
                0
            ),

            new Vector2Int(
                0,
                1
            ),

            new Vector2Int(
                0,
                -1
            )
        };


    private static readonly Vector2Int[]
        DiagonalDirections =
        {
            new Vector2Int(
                1,
                1
            ),

            new Vector2Int(
                1,
                -1
            ),

            new Vector2Int(
                -1,
                1
            ),

            new Vector2Int(
                -1,
                -1
            )
        };


    // =========================================================
    // PUBLIC API - SEAMLESS WORLD
    // =========================================================

    public static NavigationPath FindPath(
        NavigationWorld world,
        Vector2 startWorldPosition,
        Vector2 targetWorldPosition,
        bool allowDirectFastPath = true)
    {
        NavigationPath emptyPath =
            new();

        if (world == null)
            return emptyPath;

        if (!world.TryGetRegionAt(
                startWorldPosition,
                out NavigationRegion startRegion))
        {
            return emptyPath;
        }

        if (!world.TryGetRegionAt(
                targetWorldPosition,
                out NavigationRegion targetRegion))
        {
            return emptyPath;
        }

        return FindPathInternal(
            world,
            startRegion,
            targetRegion,
            startWorldPosition,
            targetWorldPosition,
            allowDirectFastPath
        );
    }


    // =========================================================
    // PUBLIC API - BACKWARDS COMPATIBILITY
    // =========================================================

    /// <summary>
    /// Behåller gamla API:t för eventuell äldre kod.
    ///
    /// Denna overload söker endast i den angivna regionen.
    /// NPCNavigationAgent / NavigationPathScheduler använder
    /// den nya NavigationWorld-overloaden.
    /// </summary>
    public static NavigationPath FindPath(
        NavigationRegion region,
        Vector2 startWorldPosition,
        Vector2 targetWorldPosition,
        bool allowDirectFastPath = true)
    {
        NavigationPath emptyPath =
            new();

        if (region == null)
            return emptyPath;

        if (!region.ContainsWorldPosition(
                startWorldPosition) ||
            !region.ContainsWorldPosition(
                targetWorldPosition))
        {
            return emptyPath;
        }

        return FindPathInternal(
            null,
            region,
            region,
            startWorldPosition,
            targetWorldPosition,
            allowDirectFastPath
        );
    }


    // =========================================================
    // SEARCH
    // =========================================================

    private static NavigationPath FindPathInternal(
        NavigationWorld world,
        NavigationRegion startRegion,
        NavigationRegion targetRegion,
        Vector2 startWorldPosition,
        Vector2 targetWorldPosition,
        bool allowDirectFastPath)
    {
        NavigationPath path =
            new();

        if (startRegion == null ||
            targetRegion == null)
        {
            return path;
        }


        // -----------------------------------------------------
        // FAST PATH
        // -----------------------------------------------------

        bool directPathClear =
            world != null
                ? world.IsDirectPathClear(
                    startWorldPosition,
                    targetWorldPosition,
                    startRegion
                )
                : startRegion.IsDirectPathClear(
                    startWorldPosition,
                    targetWorldPosition
                );

        if (allowDirectFastPath &&
            directPathClear)
        {
            Vector2[] directPoints =
            {
                startWorldPosition,
                targetWorldPosition
            };

            path.SetRawPoints(
                directPoints
            );

            path.SetPoints(
                directPoints
            );

            return path;
        }


        // -----------------------------------------------------
        // START / TARGET CELLS
        // -----------------------------------------------------

        if (!startRegion
                .TryGetClosestWalkableCell(
                    startWorldPosition,
                    out NavigationCell startCell))
        {
            return path;
        }

        if (!targetRegion
                .TryGetClosestWalkableCell(
                    targetWorldPosition,
                    out NavigationCell targetCell))
        {
            return path;
        }


        WorldCellKey startKey =
            new WorldCellKey(
                startRegion,
                startCell.X,
                startCell.Y
            );

        WorldCellKey targetKey =
            new WorldCellKey(
                targetRegion,
                targetCell.X,
                targetCell.Y
            );


        Workspace.BeginSearch();


        int startIndex =
            Workspace.GetOrCreateNode(
                startKey,
                startCell.WorldPosition
            );

        int targetIndex =
            Workspace.GetOrCreateNode(
                targetKey,
                targetCell.WorldPosition
            );


        ref SearchRecord startRecord =
            ref Workspace.GetRecord(
                startIndex
            );

        startRecord.GCost =
            0f;

        startRecord.HCost =
            Heuristic(
                startCell.WorldPosition,
                targetCell.WorldPosition,
                startRegion.CellSize
            );

        startRecord.ParentIndex =
            -1;

        Workspace.AddOrUpdateOpen(
            startIndex
        );


        // -----------------------------------------------------
        // A*
        // -----------------------------------------------------

        bool foundDestination =
            false;

        int expandedNodes =
            0;

        while (Workspace.HeapCount > 0 &&
               expandedNodes <
               MaximumExpandedNodes)
        {
            int currentIndex =
                Workspace.PopBest();

            if (currentIndex < 0)
                break;

            ref SearchRecord currentRecord =
                ref Workspace.GetRecord(
                    currentIndex
                );

            if (currentRecord.Closed)
                continue;

            currentRecord.Closed =
                true;

            expandedNodes++;

            if (currentIndex ==
                targetIndex)
            {
                foundDestination =
                    true;

                break;
            }

            WorldCellKey currentKey =
                Workspace.GetKey(
                    currentIndex
                );

            EvaluateCardinalNeighbours(
                world,
                currentIndex,
                currentKey,
                targetCell.WorldPosition
            );

            if (currentKey.Region != null &&
                currentKey.Region.AllowDiagonalMovement)
            {
                EvaluateDiagonalNeighbours(
                    world,
                    currentIndex,
                    currentKey,
                    targetCell.WorldPosition
                );
            }
        }


        if (!foundDestination)
            return path;


        // -----------------------------------------------------
        // BUILD RAW PATH
        // -----------------------------------------------------

        List<Vector2> rawPoints =
            BuildWorldPath(
                world,
                startRegion,
                targetRegion,
                startIndex,
                targetIndex,
                startWorldPosition,
                targetWorldPosition
            );

        if (rawPoints.Count == 0)
            return path;

        path.SetRawPoints(
            rawPoints
        );


        // -----------------------------------------------------
        // SMOOTH
        // -----------------------------------------------------

        List<Vector2> smoothed =
            NavigationPathSmoother.Smooth(
                world,
                startRegion,
                rawPoints
            );

        /*
         * Precis som tidigare:
         * om smoothing av någon anledning inte kan skapa minst
         * start + nästa punkt, behåll raw A*-pathen.
         */
        if (smoothed == null ||
            smoothed.Count < 2)
        {
            path.SetPoints(
                rawPoints
            );
        }
        else
        {
            path.SetPoints(
                smoothed
            );
        }

        return path;
    }


    // =========================================================
    // CARDINAL NEIGHBOURS
    // =========================================================

    private static void EvaluateCardinalNeighbours(
        NavigationWorld world,
        int currentIndex,
        WorldCellKey currentKey,
        Vector2 targetWorldPosition)
    {
        for (int i = 0;
             i < CardinalDirections.Length;
             i++)
        {
            Vector2Int offset =
                CardinalDirections[i];

            if (!TryResolveNeighbourCell(
                    world,
                    currentKey,
                    offset,
                    out WorldCellKey neighbourKey,
                    out Vector2 neighbourWorldPosition))
            {
                continue;
            }

            TryEvaluateNeighbour(
                currentIndex,
                neighbourKey,
                neighbourWorldPosition,
                targetWorldPosition,
                1f
            );
        }
    }


    // =========================================================
    // DIAGONAL NEIGHBOURS
    // =========================================================

    private static void EvaluateDiagonalNeighbours(
        NavigationWorld world,
        int currentIndex,
        WorldCellKey currentKey,
        Vector2 targetWorldPosition)
    {
        for (int i = 0;
             i < DiagonalDirections.Length;
             i++)
        {
            Vector2Int offset =
                DiagonalDirections[i];

            if (!TryResolveNeighbourCell(
                    world,
                    currentKey,
                    offset,
                    out WorldCellKey neighbourKey,
                    out Vector2 neighbourWorldPosition))
            {
                continue;
            }

            /*
             * Om vi diagonalt går in i en annan region respekterar
             * vi även den regionens diagonal-setting.
             */
            if (neighbourKey.Region != null &&
                !neighbourKey.Region.AllowDiagonalMovement)
            {
                continue;
            }

            bool preventCornerCutting =
                currentKey.Region != null &&
                currentKey.Region.PreventCornerCutting;

            if (neighbourKey.Region != null &&
                neighbourKey.Region.PreventCornerCutting)
            {
                preventCornerCutting =
                    true;
            }

            if (preventCornerCutting)
            {
                Vector2Int horizontalOffset =
                    new Vector2Int(
                        offset.x,
                        0
                    );

                Vector2Int verticalOffset =
                    new Vector2Int(
                        0,
                        offset.y
                    );

                bool horizontalClear =
                    TryResolveNeighbourCell(
                        world,
                        currentKey,
                        horizontalOffset,
                        out _,
                        out _
                    );

                bool verticalClear =
                    TryResolveNeighbourCell(
                        world,
                        currentKey,
                        verticalOffset,
                        out _,
                        out _
                    );

                if (!horizontalClear ||
                    !verticalClear)
                {
                    continue;
                }
            }

            TryEvaluateNeighbour(
                currentIndex,
                neighbourKey,
                neighbourWorldPosition,
                targetWorldPosition,
                DiagonalCost
            );
        }
    }


    // =========================================================
    // NEIGHBOUR RESOLUTION
    // =========================================================

    /// <summary>
    /// Hittar granncellen även om offseten passerar en region-border.
    ///
    /// Exempel:
    ///
    /// Region A sista X-cell + right
    /// =>
    /// Region B första X-cell.
    ///
    /// Ingen portal behövs.
    /// </summary>
    private static bool TryResolveNeighbourCell(
        NavigationWorld world,
        WorldCellKey currentKey,
        Vector2Int offset,
        out WorldCellKey neighbourKey,
        out Vector2 neighbourWorldPosition)
    {
        neighbourKey =
            default;

        neighbourWorldPosition =
            Vector2.zero;

        NavigationRegion currentRegion =
            currentKey.Region;

        if (currentRegion == null)
            return false;


        int localX =
            currentKey.X +
            offset.x;

        int localY =
            currentKey.Y +
            offset.y;


        // -----------------------------------------------------
        // SAME REGION
        // -----------------------------------------------------

        if (currentRegion.IsValidCell(
                localX,
                localY))
        {
            if (!currentRegion.IsWalkable(
                    localX,
                    localY))
            {
                return false;
            }

            neighbourKey =
                new WorldCellKey(
                    currentRegion,
                    localX,
                    localY
                );

            neighbourWorldPosition =
                currentRegion.GridToWorld(
                    localX,
                    localY
                );

            return true;
        }


        // -----------------------------------------------------
        // CROSS REGION BORDER
        // -----------------------------------------------------

        if (world == null)
            return false;

        /*
         * GridToWorld fungerar även för x/y strax utanför
         * regionen eftersom den bara gör en matematisk konvertering.
         *
         * Därmed får vi centrumpositionen för den cell som borde
         * ligga precis på andra sidan bordern.
         */
        Vector2 expectedWorldPosition =
            currentRegion.GridToWorld(
                localX,
                localY
            );

        if (!world.TryGetRegionAt(
                expectedWorldPosition,
                out NavigationRegion neighbourRegion))
        {
            return false;
        }

        if (!AreRegionsCompatible(
                currentRegion,
                neighbourRegion))
        {
            return false;
        }

        if (!neighbourRegion.TryWorldToGrid(
                expectedWorldPosition,
                out int neighbourX,
                out int neighbourY))
        {
            return false;
        }

        if (!neighbourRegion.IsWalkable(
                neighbourX,
                neighbourY))
        {
            return false;
        }

        neighbourKey =
            new WorldCellKey(
                neighbourRegion,
                neighbourX,
                neighbourY
            );

        neighbourWorldPosition =
            neighbourRegion.GridToWorld(
                neighbourX,
                neighbourY
            );

        return true;
    }


    private static bool AreRegionsCompatible(
        NavigationRegion first,
        NavigationRegion second)
    {
        if (first == null ||
            second == null)
        {
            return false;
        }

        return
            Mathf.Abs(
                first.CellSize -
                second.CellSize
            ) <=
            CellSizeTolerance;
    }


    // =========================================================
    // NEIGHBOUR EVALUATION
    // =========================================================

    private static void TryEvaluateNeighbour(
        int currentIndex,
        WorldCellKey neighbourKey,
        Vector2 neighbourWorldPosition,
        Vector2 targetWorldPosition,
        float movementCost)
    {
        int neighbourIndex =
            Workspace.GetOrCreateNode(
                neighbourKey,
                neighbourWorldPosition
            );

        ref SearchRecord neighbour =
            ref Workspace.GetRecord(
                neighbourIndex
            );

        if (neighbour.Closed)
            return;

        ref SearchRecord current =
            ref Workspace.GetRecord(
                currentIndex
            );

        float tentativeG =
            current.GCost +
            movementCost;

        if (tentativeG >=
            neighbour.GCost)
        {
            return;
        }

        neighbour.ParentIndex =
            currentIndex;

        neighbour.GCost =
            tentativeG;

        neighbour.HCost =
            Heuristic(
                neighbourWorldPosition,
                targetWorldPosition,
                neighbourKey.Region != null
                    ? neighbourKey.Region.CellSize
                    : 1f
            );

        Workspace.AddOrUpdateOpen(
            neighbourIndex
        );
    }


    // =========================================================
    // HEURISTIC
    // =========================================================

    /// <summary>
    /// Euclidean heuristic.
    ///
    /// Den är admissible både med och utan diagonaler och fungerar
    /// därför säkert även om en framtida region skulle ha andra
    /// movement-settings än sin granne.
    /// </summary>
    private static float Heuristic(
        Vector2 fromWorld,
        Vector2 targetWorld,
        float cellSize)
    {
        float safeCellSize =
            Mathf.Max(
                0.001f,
                cellSize
            );

        return
            Vector2.Distance(
                fromWorld,
                targetWorld
            ) /
            safeCellSize;
    }


    // =========================================================
    // BUILD WORLD PATH
    // =========================================================

    private static List<Vector2> BuildWorldPath(
        NavigationWorld world,
        NavigationRegion startRegion,
        NavigationRegion targetRegion,
        int startIndex,
        int destinationIndex,
        Vector2 exactStart,
        Vector2 exactTarget)
    {
        List<Vector2> reversed =
            new();

        int currentIndex =
            destinationIndex;

        int maximumSteps =
            Mathf.Max(
                1,
                Workspace.NodeCount +
                1
            );

        int steps =
            0;

        while (currentIndex >= 0 &&
               steps <
               maximumSteps)
        {
            reversed.Add(
                Workspace.GetPosition(
                    currentIndex
                )
            );

            if (currentIndex ==
                startIndex)
            {
                break;
            }

            ref SearchRecord record =
                ref Workspace.GetRecord(
                    currentIndex
                );

            currentIndex =
                record.ParentIndex;

            steps++;
        }

        if (reversed.Count == 0 ||
            currentIndex < 0)
        {
            return new List<Vector2>();
        }

        reversed.Reverse();

        List<Vector2> result =
            new(
                reversed.Count +
                2
            );

        result.Add(
            exactStart
        );

        for (int i = 0;
             i < reversed.Count;
             i++)
        {
            Vector2 point =
                reversed[i];

            Vector2 previous =
                result[
                    result.Count -
                    1
                ];

            if ((point - previous)
                    .sqrMagnitude <=
                0.0001f)
            {
                continue;
            }

            result.Add(
                point
            );
        }

        /*
         * Försök avsluta på den exakta target-positionen,
         * precis som gamla implementationen.
         */
        Vector2 lastPoint =
            result[
                result.Count -
                1
            ];

        bool exactTargetClear =
            world != null
                ? world.IsDirectPathClear(
                    lastPoint,
                    exactTarget,
                    targetRegion
                )
                : targetRegion.IsDirectPathClear(
                    lastPoint,
                    exactTarget
                );

        if ((exactTarget - lastPoint)
                .sqrMagnitude >
            0.0001f &&
            exactTargetClear)
        {
            result.Add(
                exactTarget
            );
        }

        return result;
    }
}

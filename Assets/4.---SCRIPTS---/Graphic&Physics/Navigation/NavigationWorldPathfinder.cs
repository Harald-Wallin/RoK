using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// High-level A* genom NavigationWorld.
///
/// Söker endast bland NavigationRegions och deras portals.
///
/// Lokal movement inne i varje region hanteras fortfarande
/// av vanliga AStarPathfinder.
///
/// Detta ger två navigation-nivåer:
///
/// WORLD:
/// Region -> Portal -> Region -> Portal -> Region
///
/// LOCAL:
/// Cell -> Cell -> Cell
/// </summary>
public static class NavigationWorldPathfinder
{
    // =========================================================
    // SEARCH RECORD
    // =========================================================

    private sealed class SearchNode
    {
        public NavigationRegion Region;

        public float GCost =
            float.PositiveInfinity;

        public float HCost;

        public float FCost =>
            GCost +
            HCost;

        public SearchNode Parent;

        public NavigationRegionPortal
            ParentPortal;

        public bool Closed;
    }

    // =========================================================
    // FIND PATH
    // =========================================================

    public static NavigationWorldPath FindPath(
        NavigationWorld world,
        Vector2 startPosition,
        Vector2 targetPosition)
    {
        NavigationWorldPath result =
            new();

        if (world == null)
            return result;

        if (!world.TryGetRegionAt(
                startPosition,
                out NavigationRegion startRegion))
        {
            return result;
        }

        if (!world.TryGetRegionAt(
                targetPosition,
                out NavigationRegion targetRegion))
        {
            return result;
        }

        /*
         * Samma region:
         *
         * High-level navigation behövs inte.
         */
        if (startRegion ==
            targetRegion)
        {
            result.SetRoute(
                new[]
                {
                    startRegion
                },
                null
            );

            return result;
        }

        Dictionary<
            NavigationRegion,
            SearchNode>
            nodes =
                new();

        List<SearchNode>
            open =
                new();

        SearchNode startNode =
            GetOrCreateNode(
                nodes,
                startRegion
            );

        startNode.GCost =
            0f;

        startNode.HCost =
            GetHeuristic(
                world,
                startRegion,
                targetRegion
            );

        open.Add(
            startNode
        );

        SearchNode destinationNode =
            null;

        // =====================================================
        // A*
        // =====================================================

        while (open.Count > 0)
        {
            int bestIndex =
                FindBestOpenNode(
                    open
                );

            SearchNode current =
                open[
                    bestIndex
                ];

            open.RemoveAt(
                bestIndex
            );

            if (current.Closed)
                continue;

            current.Closed =
                true;

            if (current.Region ==
                targetRegion)
            {
                destinationNode =
                    current;

                break;
            }

            IReadOnlyList<
                NavigationRegionPortal>
                regionPortals =
                    world.GetPortals(
                        current.Region
                    );

            for (int i = 0;
                 i < regionPortals.Count;
                 i++)
            {
                NavigationRegionPortal portal =
                    regionPortals[i];

                if (portal == null)
                    continue;

                NavigationRegion neighbour =
                    portal.GetOtherRegion(
                        current.Region
                    );

                if (neighbour == null ||
                    !neighbour.isActiveAndEnabled)
                {
                    continue;
                }

                SearchNode neighbourNode =
                    GetOrCreateNode(
                        nodes,
                        neighbour
                    );

                if (neighbourNode.Closed)
                    continue;

                /*
                 * En region-transition kostar 1.
                 *
                 * Det är avsiktligt i första versionen.
                 *
                 * Senare kan kostnaden väga in:
                 * - portalens world-position
                 * - terrain cost
                 * - roads
                 * - danger
                 * - faction territory
                 */
                float tentativeG =
                    current.GCost +
                    1f;

                if (tentativeG >
                    neighbourNode.GCost)
                {
                    continue;
                }

                /*
                 * Vid samma regionkostnad föredrar vi portalen
                 * som ligger närmare slutmålet.
                 *
                 * Detta ger bättre val när samma två regions
                 * har flera separata öppningar.
                 */
                if (Mathf.Approximately(
                        tentativeG,
                        neighbourNode.GCost) &&
                    neighbourNode.ParentPortal != null)
                {
                    float existingDistance =
                        Vector2.Distance(
                            neighbourNode
                                .ParentPortal
                                .WorldCenter,
                            targetPosition
                        );

                    float candidateDistance =
                        Vector2.Distance(
                            portal.WorldCenter,
                            targetPosition
                        );

                    if (candidateDistance >=
                        existingDistance)
                    {
                        continue;
                    }
                }

                neighbourNode.Parent =
                    current;

                neighbourNode.ParentPortal =
                    portal;

                neighbourNode.GCost =
                    tentativeG;

                neighbourNode.HCost =
                    GetHeuristic(
                        world,
                        neighbour,
                        targetRegion
                    );

                if (!open.Contains(
                        neighbourNode))
                {
                    open.Add(
                        neighbourNode
                    );
                }
            }
        }

        if (destinationNode == null)
            return result;

        BuildResult(
            destinationNode,
            result
        );

        return result;
    }

    // =========================================================
    // BUILD RESULT
    // =========================================================

    private static void BuildResult(
        SearchNode destination,
        NavigationWorldPath result)
    {
        List<NavigationRegion>
            reversedRegions =
                new();

        List<NavigationRegionPortal>
            reversedPortals =
                new();

        SearchNode current =
            destination;

        while (current != null)
        {
            reversedRegions.Add(
                current.Region
            );

            if (current.ParentPortal != null)
            {
                reversedPortals.Add(
                    current.ParentPortal
                );
            }

            current =
                current.Parent;
        }

        reversedRegions.Reverse();
        reversedPortals.Reverse();

        result.SetRoute(
            reversedRegions,
            reversedPortals
        );
    }

    // =========================================================
    // NODE
    // =========================================================

    private static SearchNode GetOrCreateNode(
        Dictionary<
            NavigationRegion,
            SearchNode> nodes,
        NavigationRegion region)
    {
        if (nodes.TryGetValue(
                region,
                out SearchNode node))
        {
            return node;
        }

        node =
            new SearchNode
            {
                Region =
                    region
            };

        nodes.Add(
            region,
            node
        );

        return node;
    }

    // =========================================================
    // OPEN LIST
    // =========================================================

    private static int FindBestOpenNode(
        List<SearchNode> open)
    {
        int bestIndex =
            0;

        float bestF =
            open[0].FCost;

        float bestH =
            open[0].HCost;

        for (int i = 1;
             i < open.Count;
             i++)
        {
            SearchNode candidate =
                open[i];

            if (candidate.FCost <
                bestF)
            {
                bestIndex =
                    i;

                bestF =
                    candidate.FCost;

                bestH =
                    candidate.HCost;

                continue;
            }

            if (!Mathf.Approximately(
                    candidate.FCost,
                    bestF))
            {
                continue;
            }

            if (candidate.HCost <
                bestH)
            {
                bestIndex =
                    i;

                bestH =
                    candidate.HCost;
            }
        }

        return bestIndex;
    }

    // =========================================================
    // HEURISTIC
    // =========================================================

    private static float GetHeuristic(
        NavigationWorld world,
        NavigationRegion from,
        NavigationRegion target)
    {
        Vector2Int fromCoordinate =
            world.GetRegionCoordinate(
                from
            );

        Vector2Int targetCoordinate =
            world.GetRegionCoordinate(
                target
            );

        int dx =
            Mathf.Abs(
                targetCoordinate.x -
                fromCoordinate.x
            );

        int dy =
            Mathf.Abs(
                targetCoordinate.y -
                fromCoordinate.y
            );

        /*
         * Regioner ansluter just nu kardinalt.
         *
         * Manhattan distance är därför korrekt heuristic.
         */
        return
            dx +
            dy;
    }
}

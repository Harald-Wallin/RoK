using UnityEngine;

/// <summary>
/// Runtime-navigation för en NPC.
///
/// NavigationRegion är nu endast en chunk av samma sammanhängande
/// navigation-grid.
///
/// NPC:n navigerar med EN A*-path från sin riktiga startposition
/// till sin riktiga slutdestination, även när pathen korsar flera
/// NavigationRegions.
///
/// Region-borders är alltså inte waypoints och inga portals används.
///
/// Ansvar:
/// - hålla reda på aktuell NavigationRegion
/// - begära seamless A* genom NavigationWorld
/// - följa smoothade waypoints
/// - repatha när target flyttar sig
/// - recovera från fysisk blockage / stuck
///
/// Ansvarar INTE för:
/// - Rigidbody2D movement
/// - movement speed
/// - animation
/// - AI states
/// - combat decisions
/// </summary>
[RequireComponent(typeof(NPCMovement))]
public sealed class NPCNavigationAgent :
    MonoBehaviour
{
    // =========================================================
    // REGION
    // =========================================================

    [Header("Region")]

    [SerializeField]
    private NavigationRegion navigationRegion;


    // =========================================================
    // PATHFINDING
    // =========================================================

    [Header("Pathfinding")]

    [SerializeField]
    [Min(0.05f)]
    private float waypointReachDistance =
        0.35f;


    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
        "Minsta tid mellan A*-sökningar."
    )]
    private float repathInterval =
        0.25f;


    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur långt slutmålet måste ha flyttats innan " +
        "den befintliga pathen betraktas som gammal."
    )]
    private float targetMoveThreshold =
        0.75f;


    [SerializeField]
    [Tooltip(
        "Om agenten får använda direkt movement när ingen " +
        "A*-path redan är aktiv."
    )]
    private bool preferDirectMovement =
        true;


    // =========================================================
    // STUCK RECOVERY
    // =========================================================

    [Header("Stuck Recovery")]

    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
        "Hur länge NPC:n får försöka följa navigationen utan " +
        "tydlig progress innan en ny path begärs."
    )]
    private float stuckCheckDuration =
        0.75f;


    [SerializeField]
    [Min(0.001f)]
    [Tooltip(
        "Minsta positionsförändring som räknas som progress."
    )]
    private float minimumProgressDistance =
        0.12f;


    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
        "Hur länge Direct Movement stängs av efter att den " +
        "fysiska Rigidbody-rörelsen har blockerats av World."
    )]
    private float physicalBlockPathDuration =
        1.5f;


    private Vector2 progressCheckPosition;

    private float progressTimer;

    private float forcedPathTimer;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool drawCurrentPath =
        true;


    [SerializeField]
    private bool drawRawPath;


    [SerializeField]
    private bool drawCurrentWaypoint =
        true;


    [SerializeField]
    private bool navigationDebugLogs;


    // =========================================================
    // PATH RUNTIME
    // =========================================================

    private NavigationPath currentPath;

    private int waypointIndex;


    /*
     * AI:ns slutgiltiga destination.
     */
    private Vector2 requestedDestination;


    /*
     * Destinationen som nuvarande/pending A*-path beräknades för.
     *
     * Till skillnad från gamla portal-systemet är detta alltid
     * den riktiga slutdestinationen.
     */
    private Vector2 pathDestination;


    private float repathTimer;

    private bool hasDestination;

    private bool pathRequestPending;

    private int pathRequestVersion;


    /*
     * Sparas främst för Inspector/API/debug.
     * Den styr inte längre någon portal-route.
     */
    private NavigationRegion worldDestinationRegion;


    // =========================================================
    // PUBLIC
    // =========================================================

    public bool HasDestination =>
        hasDestination;


    public bool HasPath =>
        currentPath != null &&
        currentPath.IsValid;


    /*
     * Behålls för kompatibilitet med eventuell kod som redan
     * läser properties från den gamla agenten.
     *
     * "World path" betyder nu helt enkelt att den aktiva
     * seamless-pathen går mot en annan region.
     */
    public bool HasWorldPath =>
        HasPath &&
        IsCrossRegionNavigating;


    public bool IsCrossRegionNavigating =>
        navigationRegion != null &&
        worldDestinationRegion != null &&
        navigationRegion !=
            worldDestinationRegion;


    public NavigationRegion CurrentRegion =>
        navigationRegion;


    public NavigationRegion DestinationRegion =>
        worldDestinationRegion;


    public Vector2 Destination =>
        requestedDestination;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        progressCheckPosition =
            transform.position;

        ResolveNavigationRegion();
    }


    private void Update()
    {
        if (repathTimer > 0f)
        {
            repathTimer =
                Mathf.Max(
                    0f,
                    repathTimer -
                    Time.deltaTime
                );
        }

        if (forcedPathTimer > 0f)
        {
            forcedPathTimer =
                Mathf.Max(
                    0f,
                    forcedPathTimer -
                    Time.deltaTime
                );
        }
    }


    private void OnValidate()
    {
        waypointReachDistance =
            Mathf.Max(
                0.05f,
                waypointReachDistance
            );

        repathInterval =
            Mathf.Max(
                0.05f,
                repathInterval
            );

        targetMoveThreshold =
            Mathf.Max(
                0f,
                targetMoveThreshold
            );

        stuckCheckDuration =
            Mathf.Max(
                0.1f,
                stuckCheckDuration
            );

        minimumProgressDistance =
            Mathf.Max(
                0.001f,
                minimumProgressDistance
            );

        physicalBlockPathDuration =
            Mathf.Max(
                0.1f,
                physicalBlockPathDuration
            );
    }


    // =========================================================
    // MOVEMENT DIRECTION
    // =========================================================

    /// <summary>
    /// Huvudingången för NPCMovement.
    ///
    /// destination är alltid AI:ns riktiga slutdestination.
    ///
    /// Samma path kan korsa valfritt antal NavigationRegions.
    /// </summary>
    public bool TryGetMovementDirection(
        Vector2 destination,
        out Vector2 direction)
    {
        direction =
            Vector2.zero;

        requestedDestination =
            destination;

        hasDestination =
            true;

        Vector2 currentPosition =
            transform.position;


        /*
         * Utanför NavigationWorld behåller vi gamla säkra fallbacken.
         */
        if (!ResolveNavigationRegion())
        {
            worldDestinationRegion =
                null;

            return TryGetDirectDirection(
                destination,
                out direction
            );
        }


        NavigationWorld world =
            NavigationWorld.Instance;

        if (world == null)
            return false;


        if (!world.TryGetRegionAt(
                destination,
                out NavigationRegion destinationRegion))
        {
            worldDestinationRegion =
                null;

            return false;
        }


        worldDestinationRegion =
            destinationRegion;


        return TryGetPathMovementDirection(
            world,
            currentPosition,
            destination,
            out direction
        );
    }


    // =========================================================
    // PATH MOVEMENT
    // =========================================================

    private bool TryGetPathMovementDirection(
        NavigationWorld world,
        Vector2 currentPosition,
        Vector2 destination,
        out Vector2 direction)
    {
        direction =
            Vector2.zero;

        if (world == null ||
            navigationRegion == null)
        {
            return false;
        }


        // -----------------------------------------------------
        // ACTIVE PATH
        // -----------------------------------------------------

        /*
         * En fungerande path får företräde.
         *
         * Detta behåller "path commitment"-förbättringen:
         * NPC:n byter inte sida runt ett hinder bara för att en
         * ny direct line råkar bli möjlig en kort stund.
         */
        if (HasPath)
        {
            TryScheduleUpdatedPath(
                world,
                currentPosition,
                destination
            );


            AdvanceReachedWaypoints(
                currentPosition
            );


            if (waypointIndex <
                currentPath.PointCount)
            {
                AdvanceVisibleWaypoints(
                    world,
                    currentPosition
                );


                if (TryGetCurrentWaypointDirection(
                        currentPosition,
                        out direction))
                {
                    UpdateStuckDetection(
                        world,
                        currentPosition,
                        destination
                    );

                    return true;
                }
            }


            ClearPathOnly();
        }


        // -----------------------------------------------------
        // DIRECT MOVEMENT
        // -----------------------------------------------------

        bool directPathClear =
            world.IsDirectPathClear(
                currentPosition,
                destination,
                navigationRegion
            );


        bool mayUseDirectMovement =
            preferDirectMovement &&
            forcedPathTimer <= 0f &&
            directPathClear;


        if (mayUseDirectMovement)
        {
            /*
             * Om en gammal request väntar men direct path nu är
             * fullt säker behöver vi inte längre resultatet.
             */
            if (pathRequestPending)
            {
                InvalidatePath();
            }


            ResetProgressTracking(
                currentPosition
            );


            return TryGetDirectionToPoint(
                currentPosition,
                destination,
                out direction
            );
        }


        // -----------------------------------------------------
        // NEED A*
        // -----------------------------------------------------

        ResetProgressTracking(
            currentPosition
        );


        if (ShouldRequestPath(
                destination))
        {
            RequestPath(
                world,
                currentPosition,
                destination,
                forceGridPath:
                    true
            );
        }


        /*
         * Schedulern levererar resultatet senare.
         */
        return false;
    }


    // =========================================================
    // PHYSICAL BLOCK FEEDBACK
    // =========================================================

    public void NotifyPhysicalMovementBlocked(
        Vector2 destination)
    {
        if (navigationDebugLogs)
        {
            Debug.LogWarning(
                $"[NAV REPATH] {name} PHYSICAL BLOCK | " +
                $"position={transform.position} | " +
                $"destination={destination} | " +
                $"hadPath={HasPath}",
                this
            );
        }


        requestedDestination =
            destination;

        hasDestination =
            true;


        forcedPathTimer =
            Mathf.Max(
                forcedPathTimer,
                physicalBlockPathDuration
            );


        if (!ResolveNavigationRegion())
            return;


        NavigationWorld world =
            NavigationWorld.Instance;

        if (world == null)
            return;


        if (!world.TryGetRegionAt(
                destination,
                out NavigationRegion destinationRegion))
        {
            return;
        }


        worldDestinationRegion =
            destinationRegion;


        /*
         * Låt en redan väntande request bli klar.
         * Annars kan NPCMovement råka cancella sin egen request
         * varje FixedUpdate när den står blockerad.
         */
        if (pathRequestPending)
            return;


        repathTimer =
            0f;


        RequestPath(
            world,
            transform.position,
            destination,
            forceGridPath:
                true
        );
    }


    // =========================================================
    // CURRENT WAYPOINT
    // =========================================================

    private bool TryGetCurrentWaypointDirection(
        Vector2 currentPosition,
        out Vector2 direction)
    {
        direction =
            Vector2.zero;

        if (!HasPath ||
            waypointIndex < 0 ||
            waypointIndex >=
            currentPath.PointCount)
        {
            return false;
        }


        Vector2 waypoint =
            currentPath.GetPoint(
                waypointIndex
            );


        return TryGetDirectionToPoint(
            currentPosition,
            waypoint,
            out direction
        );
    }


    private static bool TryGetDirectionToPoint(
        Vector2 from,
        Vector2 to,
        out Vector2 direction)
    {
        Vector2 delta =
            to -
            from;

        if (delta.sqrMagnitude <=
            0.0001f)
        {
            direction =
                Vector2.zero;

            return false;
        }

        direction =
            delta.normalized;

        return true;
    }


    // =========================================================
    // PATH REQUEST
    // =========================================================

    private bool ShouldRequestPath(
        Vector2 destination)
    {
        if (pathRequestPending)
        {
            float destinationChange =
                Vector2.Distance(
                    destination,
                    pathDestination
                );

            return
                destinationChange >=
                targetMoveThreshold &&
                repathTimer <= 0f;
        }


        if (!HasPath)
        {
            return
                repathTimer <= 0f;
        }


        float targetMovement =
            Vector2.Distance(
                destination,
                pathDestination
            );


        if (targetMovement >=
            targetMoveThreshold)
        {
            return
                repathTimer <= 0f;
        }


        if (waypointIndex >=
            currentPath.PointCount)
        {
            return
                repathTimer <= 0f;
        }


        return false;
    }


    private void TryScheduleUpdatedPath(
        NavigationWorld world,
        Vector2 currentPosition,
        Vector2 destination)
    {
        float targetMovement =
            Vector2.Distance(
                destination,
                pathDestination
            );

        if (targetMovement <
            targetMoveThreshold)
        {
            return;
        }

        if (repathTimer > 0f)
            return;


        if (navigationDebugLogs)
        {
            Debug.LogWarning(
                $"[NAV REPATH] {name} TARGET MOVED | " +
                $"oldDestination={pathDestination} | " +
                $"newDestination={destination} | " +
                $"movement={targetMovement:F3}",
                this
            );
        }


        RequestPath(
            world,
            currentPosition,
            destination
        );
    }


    private void RequestPath(
        NavigationWorld world,
        Vector2 start,
        Vector2 destination,
        bool forceGridPath = false)
    {
        if (world == null)
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} REQUEST FAILED: NO WORLD",
                    this
                );
            }

            return;
        }


        if (!world.TryGetRegionAt(
                start,
                out NavigationRegion startRegion))
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} REQUEST FAILED: START OUTSIDE NAV WORLD | " +
                    $"start={start}",
                    this
                );
            }

            return;
        }


        if (!world.TryGetRegionAt(
                destination,
                out NavigationRegion destinationRegion))
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} REQUEST FAILED: DESTINATION OUTSIDE NAV WORLD | " +
                    $"destination={destination}",
                    this
                );
            }

            return;
        }


        navigationRegion =
            startRegion;

        worldDestinationRegion =
            destinationRegion;


        pathRequestVersion++;

        int version =
            pathRequestVersion;


        pathDestination =
            destination;

        pathRequestPending =
            true;

        repathTimer =
            repathInterval;


        if (navigationDebugLogs)
        {
            Debug.Log(
                $"[NAV DEBUG] {name} REQUEST PATH | " +
                $"version={version} | " +
                $"startRegion={startRegion.name} | " +
                $"targetRegion={destinationRegion.name} | " +
                $"start={start} | " +
                $"destination={destination} | " +
                $"forceGrid={forceGridPath} | " +
                $"hadPath={HasPath}",
                this
            );
        }


        NavigationPathScheduler
            .Instance
            .RequestPath(
                this,
                world,
                start,
                destination,
                version,
                forceGridPath
            );
    }


    // =========================================================
    // SCHEDULED RESULT
    // =========================================================

    public void ReceiveScheduledPath(
        NavigationPath path,
        Vector2 destination,
        int version)
    {
        if (version !=
            pathRequestVersion)
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} PATH REJECTED: OLD VERSION | " +
                    $"received={version} | " +
                    $"expected={pathRequestVersion}",
                    this
                );
            }

            return;
        }


        pathRequestPending =
            false;

        pathDestination =
            destination;


        if (path == null ||
            !path.IsValid)
        {
            if (navigationDebugLogs)
            {
                Debug.LogWarning(
                    $"[NAV DEBUG] {name} PATH INVALID | " +
                    $"currentRegion={navigationRegion?.name} | " +
                    $"destination={destination}",
                    this
                );
            }

            return;
        }


        currentPath =
            path;

        waypointIndex =
            0;


        /*
         * Punkt 0 är requestens exakta startposition.
         */
        if (currentPath.PointCount > 1)
        {
            waypointIndex =
                1;
        }


        Vector2 currentPosition =
            transform.position;


        AdvanceReachedWaypoints(
            currentPosition
        );


        NavigationWorld world =
            NavigationWorld.HasInstance
                ? NavigationWorld.Instance
                : null;

        if (world != null)
        {
            AdvanceVisibleWaypoints(
                world,
                currentPosition
            );
        }


        ResetProgressTracking(
            currentPosition
        );


        if (navigationDebugLogs)
        {
            Debug.Log(
                $"[NAV DEBUG] {name} PATH ACCEPTED | " +
                $"points={currentPath.PointCount} | " +
                $"waypoint={waypointIndex} | " +
                $"destination={destination}",
                this
            );
        }
    }


    // =========================================================
    // WAYPOINT ADVANCEMENT
    // =========================================================

    private void AdvanceReachedWaypoints(
        Vector2 currentPosition)
    {
        if (!HasPath)
            return;


        float reachDistanceSqr =
            waypointReachDistance *
            waypointReachDistance;


        while (waypointIndex <
               currentPath.PointCount)
        {
            Vector2 waypoint =
                currentPath.GetPoint(
                    waypointIndex
                );

            float distanceSqr =
                (
                    waypoint -
                    currentPosition
                ).sqrMagnitude;

            if (distanceSqr >
                reachDistanceSqr)
            {
                break;
            }

            waypointIndex++;
        }
    }


    private void AdvanceVisibleWaypoints(
        NavigationWorld world,
        Vector2 currentPosition)
    {
        if (!HasPath ||
            world == null ||
            navigationRegion == null)
        {
            return;
        }


        if (waypointIndex < 0 ||
            waypointIndex >=
            currentPath.PointCount - 1)
        {
            return;
        }


        for (int candidate =
                 currentPath.PointCount - 1;
             candidate >
                 waypointIndex;
             candidate--)
        {
            Vector2 candidatePoint =
                currentPath.GetPoint(
                    candidate
                );


            if (!world.IsDirectPathClear(
                    currentPosition,
                    candidatePoint,
                    navigationRegion))
            {
                continue;
            }


            waypointIndex =
                candidate;

            return;
        }
    }


    // =========================================================
    // STUCK RECOVERY
    // =========================================================

    private void UpdateStuckDetection(
        NavigationWorld world,
        Vector2 currentPosition,
        Vector2 destination)
    {
        float movedDistance =
            Vector2.Distance(
                currentPosition,
                progressCheckPosition
            );


        if (movedDistance >=
            minimumProgressDistance)
        {
            ResetProgressTracking(
                currentPosition
            );

            return;
        }


        progressTimer +=
            Time.fixedDeltaTime;


        if (progressTimer <
            stuckCheckDuration)
        {
            return;
        }


        progressTimer =
            0f;

        progressCheckPosition =
            currentPosition;


        if (repathTimer > 0f ||
            pathRequestPending)
        {
            return;
        }


        if (navigationDebugLogs)
        {
            Debug.LogWarning(
                $"[NAV REPATH] {name} STUCK RECOVERY | " +
                $"position={currentPosition} | " +
                $"destination={destination} | " +
                $"hadPath={HasPath}",
                this
            );
        }


        RequestPath(
            world,
            currentPosition,
            destination,
            forceGridPath:
                true
        );
    }


    private void ResetProgressTracking(
        Vector2 currentPosition)
    {
        progressCheckPosition =
            currentPosition;

        progressTimer =
            0f;
    }


    // =========================================================
    // DIRECT FALLBACK
    // =========================================================

    private bool TryGetDirectDirection(
        Vector2 destination,
        out Vector2 direction)
    {
        return TryGetDirectionToPoint(
            transform.position,
            destination,
            out direction
        );
    }


    // =========================================================
    // REGION
    // =========================================================

    private bool ResolveNavigationRegion()
    {
        Vector2 currentPosition =
            transform.position;


        if (navigationRegion != null &&
            navigationRegion.isActiveAndEnabled &&
            navigationRegion
                .ContainsWorldPosition(
                    currentPosition))
        {
            return true;
        }


        NavigationRegion previousRegion =
            navigationRegion;


        navigationRegion =
            null;


        NavigationWorld world =
            NavigationWorld.Instance;

        if (world == null)
            return false;


        if (!world.TryGetRegionAt(
                currentPosition,
                out NavigationRegion region))
        {
            return false;
        }


        navigationRegion =
            region;


        /*
         * VIKTIGT:
         *
         * En regionövergång invaliderar INTE längre pathen.
         * Samma path är skapad över hela NavigationWorld och ska
         * fortsätta sömlöst över bordern.
         */
        if (previousRegion !=
                navigationRegion &&
            previousRegion !=
                null &&
            navigationDebugLogs)
        {
            /*Debug.Log(
                $"[NAV REGION] {name} | " +
                $"{previousRegion.name} -> {navigationRegion.name}",
                this
            );*/
        }


        return true;
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ClearDestination()
    {
        hasDestination =
            false;

        requestedDestination =
            Vector2.zero;

        pathDestination =
            Vector2.zero;

        worldDestinationRegion =
            null;


        InvalidatePath();


        ResetProgressTracking(
            transform.position
        );
    }


    public void ForceRepath()
    {
        repathTimer =
            0f;


        InvalidatePath();


        ResetProgressTracking(
            transform.position
        );
    }


    private void InvalidatePath()
    {
        pathRequestVersion++;

        pathRequestPending =
            false;


        NavigationPathScheduler
            .Instance
            .CancelRequests(
                this
            );


        ClearPathOnly();
    }


    private void ClearPathOnly()
    {
        currentPath =
            null;

        waypointIndex =
            0;
    }


#if UNITY_EDITOR

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        DrawPathGizmos();
    }


    private void DrawPathGizmos()
    {
        if (currentPath == null)
            return;


        if (drawRawPath &&
            currentPath.RawPoints != null)
        {
            DrawPath(
                currentPath.RawPoints
            );
        }


        if (drawCurrentPath &&
            currentPath.Points != null)
        {
            DrawPath(
                currentPath.Points
            );
        }


        if (drawCurrentWaypoint &&
            currentPath.IsValid &&
            waypointIndex >= 0 &&
            waypointIndex <
            currentPath.PointCount)
        {
            Vector2 waypoint =
                currentPath.GetPoint(
                    waypointIndex
                );


            Gizmos.DrawWireSphere(
                waypoint,
                waypointReachDistance
            );


            Gizmos.DrawLine(
                transform.position,
                waypoint
            );
        }
    }


    private static void DrawPath(
        System.Collections.Generic
            .IReadOnlyList<Vector2> points)
    {
        if (points == null ||
            points.Count < 2)
        {
            return;
        }


        for (int i = 0;
             i < points.Count - 1;
             i++)
        {
            Gizmos.DrawLine(
                points[i],
                points[i + 1]
            );
        }
    }

#endif
}

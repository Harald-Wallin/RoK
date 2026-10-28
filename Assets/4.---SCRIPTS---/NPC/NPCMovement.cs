using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NPCNavigationAgent))]
public class NPCMovement : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    protected CharacterStats stats;
    protected HumanoidEquipment equipment;
    protected CharacterActionController actionController;

    private HumanoidVisualController visualController;

    private Rigidbody2D rb;
    private NPCNavigationAgent navigationAgent;

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "Hur nära destinationen NPC:n behöver komma innan " +
        "den betraktas som nådd."
    )]
    private float stopDistance =
        0.8f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Liten fysisk säkerhetsmarginal mot World-colliders."
    )]
    private float skinWidth =
        0.02f;

    public float DefaultStopDistance =>
        stopDistance;

    // =========================================================
    // CHARACTER STEERING
    // =========================================================

    [Header("Character Steering")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur långt framför NPC:n en annan karaktär kan börja " +
        "påverka lokal avoidance."
    )]
    private float avoidanceProbeDistance =
        1.4f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Halvbredd på korridoren framför NPC:n där en annan " +
        "karaktär betraktas som blockerande."
    )]
    private float avoidanceCorridorHalfWidth =
        0.65f;

    [SerializeField]
    [Range(0f, 1.5f)]
    [Tooltip(
        "Hur starkt NPC:n styr åt sidan runt en blockerande " +
        "karaktär."
    )]
    private float avoidanceStrength =
        0.8f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur länge samma vänster/höger-beslut behålls. " +
        "Förhindrar snabbt jitter mellan två steering-riktningar."
    )]
    private float avoidanceMemoryDuration =
        0.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Avstånd där två karaktärer betraktas som så nära " +
        "att en liten overlap-recovery får användas."
    )]
    private float overlapRecoveryDistance =
        0.25f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "Hur starkt overlap-recovery får påverka movement."
    )]
    private float overlapRecoveryStrength =
        0.35f;

    private static int CharacterLayers =>
        LayerMask.GetMask(
            "NPC",
            "Player"
        );

    private readonly Collider2D[]
        characterBuffer =
            new Collider2D[24];

    /*
     * Steering-memory.
     *
     * Vi låser sidovalet mot samma blockerare en kort stund.
     * Det förhindrar:
     *
     * frame 1 -> vänster
     * frame 2 -> höger
     * frame 3 -> vänster
     */
    private CharacterStats
        rememberedAvoidanceCharacter;

    /*
     * -1 = höger
     * +1 = vänster
     */
    private float rememberedAvoidanceSide;

    private float avoidanceMemoryTimer;

    // =========================================================
    // FLEE
    // =========================================================

    private bool isFleeing;

    private CharacterStats fleeSource;

    private float fleeDistance;
    private float safeDistance;

    private Vector2 fleeStartPosition;
    private Vector2 fleeTargetPosition;

    private float fleeSpeedMultiplier =
        1f;

    // =========================================================
    // WANDER
    // =========================================================

    [Header("Wander Settings")]

    [SerializeField]
    [Min(0f)]
    private float wanderRadius =
        3f;

    [SerializeField]
    [Min(0f)]
    private float wanderMoveTime =
        2f;

    [SerializeField]
    [Min(0f)]
    private float wanderPauseTime =
        2f;

    [SerializeField]
    [Min(0f)]
    private float wanderSpeedMultiplier =
        0.5f;

    private Vector2 wanderTarget;

    private float wanderTimer;

    private bool isWandering;
    private bool isPausing;

    // =========================================================
    // PATROL
    // =========================================================

    [Header("Patrol Settings")]

    [SerializeField]
    [Min(0f)]
    protected float patrolSpeedMultiplier =
        0.75f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip(
    "Hur nära en vanlig patrol-node NPC:n måste komma innan " +
    "noden betraktas som nådd. " +
    "Hålls separat från generell Stop Distance så patrol-pathen " +
    "inte byter mål för tidigt runt hinder."
)]
    private float patrolNodeReachDistance =
    0.35f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Om patrol-noden blockeras av en annan levande " +
        "karaktär får den betraktas som nådd när patrulleraren " +
        "redan befinner sig inom detta avstånd."
    )]
    private float blockedPatrolNodeAcceptanceDistance =
        2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Radie runt patrol-noden där en annan levande " +
        "karaktär räknas som att den blockerar noden."
    )]
    private float patrolNodeBlockRadius =
        0.9f;

    private int patrolIndex;

    private bool patrolForward =
        true;

    private float patrolWaitTimer;

    private bool waitingAtPatrolNode;

    // =========================================================
    // STATE
    // =========================================================

    public Vector2 CurrentFacingDirection
    {
        get;
        private set;
    } = Vector2.down;

    public Vector3 SpawnPosition
    {
        get;
        private set;
    }

    public enum NPCMovementMode
    {
        Default,
        Aggressive,
        Patrol,
        Wander,
        Flee
    }

    private NPCMovementMode movementMode =
        NPCMovementMode.Default;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        rb =
            GetComponent<Rigidbody2D>();

        stats =
            GetComponent<CharacterStats>();

        visualController =
            GetComponentInChildren<
                HumanoidVisualController>();

        equipment =
            GetComponent<HumanoidEquipment>();

        actionController =
            GetComponent<
                CharacterActionController>();

        navigationAgent =
            GetComponent<
                NPCNavigationAgent>();

        SpawnPosition =
            transform.position;
    }

    private void OnValidate()
    {
        stopDistance =
            Mathf.Max(
                0.01f,
                stopDistance
            );

        skinWidth =
            Mathf.Max(
                0f,
                skinWidth
            );

        avoidanceProbeDistance =
            Mathf.Max(
                0f,
                avoidanceProbeDistance
            );

        avoidanceCorridorHalfWidth =
            Mathf.Max(
                0f,
                avoidanceCorridorHalfWidth
            );

        avoidanceStrength =
            Mathf.Clamp(
                avoidanceStrength,
                0f,
                1.5f
            );

        avoidanceMemoryDuration =
            Mathf.Max(
                0f,
                avoidanceMemoryDuration
            );

        overlapRecoveryDistance =
            Mathf.Max(
                0f,
                overlapRecoveryDistance
            );

        overlapRecoveryStrength =
            Mathf.Clamp01(
                overlapRecoveryStrength
            );

        wanderRadius =
            Mathf.Max(
                0f,
                wanderRadius
            );

        wanderMoveTime =
            Mathf.Max(
                0f,
                wanderMoveTime
            );

        wanderPauseTime =
            Mathf.Max(
                0f,
                wanderPauseTime
            );

        wanderSpeedMultiplier =
            Mathf.Max(
                0f,
                wanderSpeedMultiplier
            );

        patrolSpeedMultiplier =
            Mathf.Max(
                0f,
                patrolSpeedMultiplier
            );

        patrolNodeReachDistance =
            Mathf.Max(
                0.05f,
                patrolNodeReachDistance
            );

        blockedPatrolNodeAcceptanceDistance =
            Mathf.Max(
                0f,
                blockedPatrolNodeAcceptanceDistance
            );

        patrolNodeBlockRadius =
            Mathf.Max(
                0f,
                patrolNodeBlockRadius
            );
    }

    // =========================================================
    // MODE
    // =========================================================

    public void SetMovementMode(
        NPCMovementMode mode)
    {
        if (movementMode == mode)
            return;

        movementMode =
            mode;

        ClearAvoidanceMemory();
    }

    // =========================================================
    // FACING
    // =========================================================

    public void SetFacing(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        CurrentFacingDirection =
            direction.normalized;

        visualController?.SetFacing(
            CurrentFacingDirection
        );

        equipment?.UpdateVisualDirection(
            CurrentFacingDirection
        );
    }

    // =========================================================
    // PUBLIC MOVEMENT
    // =========================================================

    /// <summary>
    /// Gemensam movement-ingång för generell NPC-rörelse.
    ///
    /// NavigationAgent väljer world/path-riktning.
    /// NPCMovement får endast göra lokal, begränsad steering.
    /// </summary>
    public bool MoveTowards(
        Vector3 target,
        float speedMultiplier = 1f,
        float customStopDistance = -1f)
    {
        return MoveTowardsInternal(
            target,
            speedMultiplier,
            customStopDistance,
            ignoredAvoidanceCharacter: null
        );
    }

    // =========================================================
    // CORE MOVEMENT PIPELINE
    // =========================================================

    private bool MoveTowardsInternal(
        Vector3 target,
        float speedMultiplier,
        float customStopDistance,
        CharacterStats ignoredAvoidanceCharacter)
    {
        if (!CanMove())
        {
            HoldPosition();

            return false;
        }

        float resolvedStopDistance =
            ResolveStopDistance(
                customStopDistance
            );

        Vector2 targetPosition =
            target;

        if (HasReachedDestination(
                targetPosition,
                resolvedStopDistance))
        {
            HoldPosition();

            return false;
        }

        float moveSpeed =
            ResolveMovementSpeed(
                speedMultiplier
            );

        if (moveSpeed <= 0.0001f)
        {
            HoldPosition();

            return false;
        }

        if (!TryResolveNavigationDirection(
                targetPosition,
                out Vector2 navigationDirection))
        {
            HoldPosition();

            return false;
        }

        Vector2 movementDirection =
            ResolveLocalSteering(
                navigationDirection,
                ignoredAvoidanceCharacter
            );

        if (movementDirection.sqrMagnitude <=
            0.0001f)
        {
            movementDirection =
                navigationDirection;
        }

        movementDirection.Normalize();

        /*
         * Local steering får aldrig styra oss genom World.
         *
         * Om steering-riktningen är geometriskt olämplig
         * används navigationens ursprungliga riktning.
         */
        if (!IsNavigationSteeringDirectionClear(
                movementDirection,
                moveSpeed))
        {
            movementDirection =
                navigationDirection.normalized;
        }

        return ApplyPhysicalMovement(
            movementDirection,
            moveSpeed,
            targetPosition
        );
    }

    // =========================================================
    // MOVEMENT VALIDATION
    // =========================================================

    private bool CanMove()
    {
        return
            rb != null &&
            stats != null &&
            stats.IsAlive &&
            !stats.IsStunned;
    }

    private float ResolveStopDistance(
        float customStopDistance)
    {
        if (customStopDistance >= 0f)
        {
            return Mathf.Max(
                0f,
                customStopDistance
            );
        }

        return stopDistance;
    }

    private bool HasReachedDestination(
        Vector2 destination,
        float resolvedStopDistance)
    {
        if (rb == null)
            return true;

        float distanceSqr =
            (
                destination -
                rb.position
            ).sqrMagnitude;

        float stopDistanceSqr =
            resolvedStopDistance *
            resolvedStopDistance;

        return
            distanceSqr <=
            stopDistanceSqr;
    }

    private float ResolveMovementSpeed(
        float speedMultiplier)
    {
        if (stats == null)
            return 0f;

        float actionMovementMultiplier =
            actionController != null
                ? actionController
                    .CurrentMovementMultiplier
                : 1f;

        if (actionMovementMultiplier <=
            0.0001f)
        {
            return 0f;
        }

        float moveSpeed =
            stats.GetStat(
                StatType.MovementSpeed
            );

        moveSpeed *=
            actionMovementMultiplier;

        moveSpeed *=
            Mathf.Max(
                0f,
                speedMultiplier
            );

        return Mathf.Max(
            0f,
            moveSpeed
        );
    }

    // =========================================================
    // NAVIGATION
    // =========================================================

    private bool TryResolveNavigationDirection(
        Vector2 destination,
        out Vector2 direction)
    {
        direction =
            Vector2.zero;

        if (navigationAgent != null)
        {
            bool hasDirection =
                navigationAgent
                    .TryGetMovementDirection(
                        destination,
                        out direction
                    );

            if (!hasDirection)
            {
                return false;
            }

            if (direction.sqrMagnitude <=
                0.0001f)
            {
                return false;
            }

            direction.Normalize();

            return true;
        }

        /*
         * Säker fallback om NavigationAgent av någon anledning
         * saknas trots RequireComponent.
         */
        Vector2 directDirection =
            destination -
            rb.position;

        if (directDirection.sqrMagnitude <=
            0.0001f)
        {
            return false;
        }

        direction =
            directDirection.normalized;

        return true;
    }

    // =========================================================
    // LOCAL STEERING
    // =========================================================

    /// <summary>
    /// ENDA platsen där dynamiska karaktärer får korrigera
    /// NavigationAgent-riktningen.
    ///
    /// Prioritet:
    ///
    /// 1. Forward avoidance
    /// 2. Minimal overlap recovery
    /// 3. Ursprunglig navigationDirection
    ///
    /// Karaktärer bakom NPC:n påverkar inte normal avoidance.
    /// </summary>
    private Vector2 ResolveLocalSteering(
        Vector2 navigationDirection,
        CharacterStats ignoredCharacter)
    {
        if (navigationDirection.sqrMagnitude <=
            0.0001f)
        {
            return Vector2.zero;
        }

        navigationDirection.Normalize();

        UpdateAvoidanceMemoryTimer();

        CharacterStats blocker =
            FindForwardBlockingCharacter(
                navigationDirection,
                ignoredCharacter,
                out Vector2 blockerPosition
            );

        if (blocker != null)
        {
            return ResolveAvoidanceDirection(
                navigationDirection,
                blocker,
                blockerPosition
            );
        }

        /*
         * Ingen relevant forward-blocker.
         *
         * Separation får nu endast användas som recovery om
         * vi faktiskt befinner oss nästan ovanpå någon.
         */
        Vector2 overlapRecovery =
            ResolveOverlapRecovery(
                navigationDirection,
                ignoredCharacter
            );

        if (overlapRecovery.sqrMagnitude >
            0.0001f)
        {
            return overlapRecovery;
        }

        return navigationDirection;
    }

    // =========================================================
    // FORWARD CHARACTER AVOIDANCE
    // =========================================================

    private CharacterStats
        FindForwardBlockingCharacter(
            Vector2 navigationDirection,
            CharacterStats ignoredCharacter,
            out Vector2 blockerPosition)
    {
        blockerPosition =
            Vector2.zero;

        if (rb == null ||
            avoidanceProbeDistance <= 0f ||
            avoidanceCorridorHalfWidth <= 0f ||
            avoidanceStrength <= 0f)
        {
            return null;
        }

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                rb.position,
                avoidanceProbeDistance,
                characterBuffer,
                CharacterLayers
            );

        if (hitCount <= 0)
            return null;

        CharacterStats bestBlocker =
            null;

        float bestForwardDistance =
            float.PositiveInfinity;

        Vector2 origin =
            rb.position;

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                characterBuffer[i];

            if (hit == null)
                continue;

            CharacterStats other =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (!IsValidAvoidanceCharacter(
                    other,
                    ignoredCharacter))
            {
                continue;
            }

            Vector2 point =
                hit.ClosestPoint(
                    origin
                );

            Vector2 toCharacter =
                point -
                origin;

            if (toCharacter.sqrMagnitude <=
                0.0001f)
            {
                toCharacter =
                    (Vector2)other
                        .transform
                        .position -
                    origin;

                point =
                    other.transform.position;
            }

            /*
             * Viktig regel:
             *
             * Negativ eller noll forward distance betyder att
             * karaktären är bakom/vid sidan av NPC:n.
             *
             * Den får då INTE styra NPC:n.
             */
            float forwardDistance =
                Vector2.Dot(
                    toCharacter,
                    navigationDirection
                );

            if (forwardDistance <=
                    0f ||
                forwardDistance >
                    avoidanceProbeDistance)
            {
                continue;
            }

            Vector2 lateralVector =
                toCharacter -
                navigationDirection *
                forwardDistance;

            float lateralDistance =
                lateralVector.magnitude;

            if (lateralDistance >
                avoidanceCorridorHalfWidth)
            {
                continue;
            }

            if (forwardDistance >=
                bestForwardDistance)
            {
                continue;
            }

            bestForwardDistance =
                forwardDistance;

            bestBlocker =
                other;

            blockerPosition =
                point;
        }

        return bestBlocker;
    }

    private bool IsValidAvoidanceCharacter(
        CharacterStats other,
        CharacterStats ignoredCharacter)
    {
        if (other == null ||
            other == stats ||
            other == ignoredCharacter ||
            !other.IsAlive)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // AVOIDANCE DIRECTION
    // =========================================================

    private Vector2 ResolveAvoidanceDirection(
        Vector2 navigationDirection,
        CharacterStats blocker,
        Vector2 blockerPosition)
    {
        float side =
            ResolveAvoidanceSide(
                navigationDirection,
                blocker,
                blockerPosition
            );

        Vector2 left =
            new Vector2(
                -navigationDirection.y,
                navigationDirection.x
            );

        Vector2 lateralDirection =
            left *
            side;

        Vector2 steering =
            navigationDirection +
            lateralDirection *
            avoidanceStrength;

        if (steering.sqrMagnitude <=
            0.0001f)
        {
            return navigationDirection;
        }

        steering.Normalize();

        if (IsShortSteeringDirectionClear(
                steering))
        {
            return steering;
        }

        /*
         * Föredragen sida blockerad av World.
         *
         * Testa andra sidan innan vi ger upp.
         */
        Vector2 oppositeSteering =
            navigationDirection -
            lateralDirection *
            avoidanceStrength;

        if (oppositeSteering.sqrMagnitude >
            0.0001f)
        {
            oppositeSteering.Normalize();

            if (IsShortSteeringDirectionClear(
                    oppositeSteering))
            {
                /*
                 * Minns även det korrigerade sidovalet.
                 */
                rememberedAvoidanceCharacter =
                    blocker;

                rememberedAvoidanceSide =
                    -side;

                avoidanceMemoryTimer =
                    avoidanceMemoryDuration;

                return oppositeSteering;
            }
        }

        return navigationDirection;
    }

    private float ResolveAvoidanceSide(
        Vector2 navigationDirection,
        CharacterStats blocker,
        Vector2 blockerPosition)
    {
        /*
         * Samma blockerare:
         * behåll tidigare sidoval.
         */
        if (rememberedAvoidanceCharacter ==
                blocker &&
            avoidanceMemoryTimer > 0f &&
            !Mathf.Approximately(
                rememberedAvoidanceSide,
                0f))
        {
            avoidanceMemoryTimer =
                avoidanceMemoryDuration;

            return rememberedAvoidanceSide;
        }

        Vector2 left =
            new Vector2(
                -navigationDirection.y,
                navigationDirection.x
            );

        Vector2 toBlocker =
            blockerPosition -
            rb.position;

        float blockerSide =
            Vector2.Dot(
                toBlocker,
                left
            );

        float side;

        /*
         * Blockeraren står på vänster sida:
         * gå höger.
         */
        if (blockerSide > 0.05f)
        {
            side =
                -1f;
        }
        /*
         * Blockeraren står på höger sida:
         * gå vänster.
         */
        else if (blockerSide < -0.05f)
        {
            side =
                1f;
        }
        else
        {
            /*
             * Nästan exakt framför oss.
             *
             * Använd en stabil, per-instance preferens i stället
             * för Random så beteendet inte flippar mellan frames.
             */
            side =
                (
                    GetInstanceID() &
                    1
                ) == 0
                    ? 1f
                    : -1f;
        }

        rememberedAvoidanceCharacter =
            blocker;

        rememberedAvoidanceSide =
            side;

        avoidanceMemoryTimer =
            avoidanceMemoryDuration;

        return side;
    }

    private void UpdateAvoidanceMemoryTimer()
    {
        if (avoidanceMemoryTimer <= 0f)
        {
            ClearAvoidanceMemory();

            return;
        }

        avoidanceMemoryTimer =
            Mathf.Max(
                0f,
                avoidanceMemoryTimer -
                Time.fixedDeltaTime
            );

        if (avoidanceMemoryTimer <= 0f)
        {
            ClearAvoidanceMemory();
        }
    }

    private void ClearAvoidanceMemory()
    {
        rememberedAvoidanceCharacter =
            null;

        rememberedAvoidanceSide =
            0f;

        avoidanceMemoryTimer =
            0f;
    }

    // =========================================================
    // OVERLAP RECOVERY
    // =========================================================

    /// <summary>
    /// Separation används INTE längre som ett konstant flock-
    /// steering-system.
    ///
    /// Den aktiveras endast när två karaktärer redan står
    /// extremt nära/överlappande.
    /// </summary>
    private Vector2 ResolveOverlapRecovery(
        Vector2 navigationDirection,
        CharacterStats ignoredCharacter)
    {
        if (rb == null ||
            overlapRecoveryDistance <= 0f ||
            overlapRecoveryStrength <= 0f)
        {
            return Vector2.zero;
        }

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                rb.position,
                overlapRecoveryDistance,
                characterBuffer,
                CharacterLayers
            );

        if (hitCount <= 0)
            return Vector2.zero;

        Vector2 recovery =
            Vector2.zero;

        int recoveryCount =
            0;

        Vector2 origin =
            rb.position;

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                characterBuffer[i];

            if (hit == null)
                continue;

            CharacterStats other =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (!IsValidAvoidanceCharacter(
                    other,
                    ignoredCharacter))
            {
                continue;
            }

            Vector2 closestPoint =
                hit.ClosestPoint(
                    origin
                );

            Vector2 away =
                origin -
                closestPoint;

            if (away.sqrMagnitude <=
                0.0001f)
            {
                away =
                    origin -
                    (Vector2)other
                        .transform
                        .position;
            }

            if (away.sqrMagnitude <=
                0.0001f)
            {
                continue;
            }

            recovery +=
                away.normalized;

            recoveryCount++;
        }

        if (recoveryCount <= 0 ||
            recovery.sqrMagnitude <=
            0.0001f)
        {
            return Vector2.zero;
        }

        recovery.Normalize();

        Vector2 result =
            navigationDirection +
            recovery *
            overlapRecoveryStrength;

        if (result.sqrMagnitude <=
            0.0001f)
        {
            return navigationDirection;
        }

        result.Normalize();

        if (!IsShortSteeringDirectionClear(
                result))
        {
            return navigationDirection;
        }

        return result;
    }

    // =========================================================
    // STEERING WORLD VALIDATION
    // =========================================================

    private bool IsShortSteeringDirectionClear(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return false;
        }

        direction.Normalize();

        float probeDistance =
            Mathf.Max(
                0.35f,
                avoidanceProbeDistance *
                0.6f
            );

        if (navigationAgent == null ||
            navigationAgent.CurrentRegion == null)
        {
            return true;
        }

        Vector2 probeEnd =
            rb.position +
            direction *
            probeDistance;

        return navigationAgent
            .CurrentRegion
            .IsDirectPathClear(
                rb.position,
                probeEnd
            );
    }

    private bool IsNavigationSteeringDirectionClear(
        Vector2 direction,
        float moveSpeed)
    {
        if (navigationAgent == null ||
            navigationAgent.CurrentRegion == null)
        {
            return true;
        }

        float probeDistance =
            Mathf.Max(
                0.4f,
                moveSpeed *
                Time.fixedDeltaTime *
                3f
            );

        Vector2 probeEnd =
            rb.position +
            direction.normalized *
            probeDistance;

        return navigationAgent
            .CurrentRegion
            .IsDirectPathClear(
                rb.position,
                probeEnd
            );
    }

    // =========================================================
    // PHYSICAL MOVEMENT
    // =========================================================

    private bool ApplyPhysicalMovement(
        Vector2 movementDirection,
        float moveSpeed,
        Vector2 finalDestination)
    {
        if (rb == null ||
            movementDirection.sqrMagnitude <=
            0.0001f ||
            moveSpeed <=
            0.0001f)
        {
            HoldPosition();

            return false;
        }

        Vector2 desiredMove =
            movementDirection.normalized *
            moveSpeed *
            Time.fixedDeltaTime;

        Vector2 safeMove =
            ResolveWorldCollision(
                desiredMove
            );

        if (safeMove.sqrMagnitude <=
            0.0000001f)
        {
            /*
             * Navigationen trodde att rörelsen var användbar,
             * men Rigidbody-collidern kunde inte genomföra den.
             *
             * Låt NavigationAgent tvinga fram grid-path.
             */
            navigationAgent
                ?.NotifyPhysicalMovementBlocked(
                    finalDestination
                );

            HoldPosition();

            return false;
        }

        rb.MovePosition(
            rb.position +
            safeMove
        );

        SetFacing(
            safeMove.normalized
        );

        visualController?.SetMoving(
            true
        );

        return true;
    }

    // =========================================================
    // WORLD COLLISION SAFETY
    // =========================================================

    /// <summary>
    /// Sista fysisk säkerhetskontroll.
    ///
    /// A* väljer vägen.
    /// Den här metoden får endast förhindra penetration genom
    /// statisk World-geometri.
    /// </summary>
    private Vector2 ResolveWorldCollision(
        Vector2 desiredMove)
    {
        if (desiredMove.sqrMagnitude <=
            0.0000001f)
        {
            return Vector2.zero;
        }

        ContactFilter2D filter =
            new ContactFilter2D
            {
                useLayerMask =
                    true,

                layerMask = ResolveWorldCollisionMask(),

                useTriggers =
                    false
            };

        RaycastHit2D[] hits =
            new RaycastHit2D[1];

        float distance =
            desiredMove.magnitude;

        int hitCount =
            rb.Cast(
                desiredMove.normalized,
                filter,
                hits,
                distance +
                skinWidth
            );

        if (hitCount == 0)
        {
            return desiredMove;
        }

        RaycastHit2D hit =
            hits[0];

        Vector2 hitNormal =
            hit.normal;

        float movementIntoNormal =
            Vector2.Dot(
                desiredMove,
                hitNormal
            );

        /*
         * Positiv dot:
         * rörelsen går bort från ytan.
         */
        if (movementIntoNormal >= 0f)
        {
            return desiredMove;
        }

        /*
         * Ta bort endast komponenten som går in i väggen.
         */
        Vector2 slideMove =
            desiredMove -
            hitNormal *
            movementIntoNormal;

        if (slideMove.sqrMagnitude <=
            0.0000001f)
        {
            return Vector2.zero;
        }

        int slideHitCount =
            rb.Cast(
                slideMove.normalized,
                filter,
                hits,
                slideMove.magnitude +
                skinWidth
            );

        if (slideHitCount <= 0)
        {
            return slideMove;
        }

        return Vector2.zero;
    }

    private int ResolveWorldCollisionMask()
    {
        if (navigationAgent != null &&
            navigationAgent.CurrentRegion != null)
        {
            int navigationMask =
                navigationAgent
                    .CurrentRegion
                    .ObstacleLayers
                    .value;

            if (navigationMask != 0)
            {
                return navigationMask;
            }
        }


        /*
         * Safe fallback för en NPC som tillfälligt befinner sig
         * utanför en NavigationRegion.
         */
        return LayerMask.GetMask(
            "World",
            "Blocking"
        );
    }

    // =========================================================
    // WANDER
    // =========================================================

    public void BeginWander()
    {
        SetMovementMode(
            NPCMovementMode.Wander
        );

        isWandering =
            true;

        isPausing =
            false;

        GenerateNewWanderTarget(
            SpawnPosition
        );

        wanderTimer =
            wanderMoveTime;

        navigationAgent
            ?.ClearDestination();
    }

    public void UpdateWander(
        Vector3 spawnPosition)
    {
        if (!isWandering &&
            !isPausing)
        {
            return;
        }

        wanderTimer -=
            Time.fixedDeltaTime;

        if (isPausing)
        {
            HoldPosition();

            if (wanderTimer > 0f)
                return;

            isPausing =
                false;

            isWandering =
                true;

            GenerateNewWanderTarget(
                spawnPosition
            );

            wanderTimer =
                wanderMoveTime;

            navigationAgent
                ?.ClearDestination();

            return;
        }

        MoveTowardsInternal(
            wanderTarget,
            wanderSpeedMultiplier,
            stopDistance,
            ignoredAvoidanceCharacter: null
        );

        float distance =
            Vector2.Distance(
                transform.position,
                wanderTarget
            );

        bool reachedTarget =
            distance <=
            stopDistance;

        bool wanderExpired =
            wanderTimer <= 0f;

        if (!reachedTarget &&
            !wanderExpired)
        {
            return;
        }

        BeginWanderPause();
    }

    private void BeginWanderPause()
    {
        isWandering =
            false;

        isPausing =
            true;

        HoldPosition();

        navigationAgent
            ?.ClearDestination();

        wanderTimer =
            Random.Range(
                1f,
                Mathf.Max(
                    1f,
                    wanderPauseTime
                )
            );
    }

    private void GenerateNewWanderTarget(
        Vector3 center)
    {
        Vector2 randomDirection =
            Random.insideUnitCircle;

        if (randomDirection.sqrMagnitude <=
            0.0001f)
        {
            randomDirection =
                Vector2.right;
        }

        randomDirection.Normalize();

        float randomDistance =
            Random.Range(
                0.5f,
                Mathf.Max(
                    0.5f,
                    wanderRadius
                )
            );

        wanderTarget =
            (Vector2)center +
            randomDirection *
            randomDistance;
    }

    public void EndWander()
    {
        isWandering =
            false;

        isPausing =
            false;

        wanderTimer =
            0f;

        navigationAgent
            ?.ClearDestination();

        ClearAvoidanceMemory();
    }

    // =========================================================
    // FLEE
    // =========================================================

    public void BeginFlee(
        CharacterStats source,
        float fleeDistance,
        float safeDistance,
        float speedMultiplier = 1f)
    {
        if (source == null ||
            rb == null)
        {
            return;
        }

        fleeSource =
            source;

        this.fleeDistance =
            Mathf.Max(
                0.1f,
                fleeDistance
            );

        this.safeDistance =
            Mathf.Max(
                0f,
                safeDistance
            );

        fleeSpeedMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        fleeStartPosition =
            rb.position;

        Vector2 threatPosition =
            TargetUtility.GetTargetPosition(
                source.gameObject
            );

        Vector2 fleeDirection =
            rb.position -
            threatPosition;

        if (fleeDirection.sqrMagnitude <=
            0.0001f)
        {
            fleeDirection =
                CurrentFacingDirection;
        }

        if (fleeDirection.sqrMagnitude <=
            0.0001f)
        {
            fleeDirection =
                Vector2.right;
        }

        fleeDirection.Normalize();

        fleeTargetPosition =
            rb.position +
            fleeDirection *
            this.fleeDistance;

        isFleeing =
            true;

        SetMovementMode(
            NPCMovementMode.Flee
        );

        navigationAgent
            ?.ForceRepath();
    }

    public bool UpdateFlee()
    {
        if (!isFleeing)
            return true;

        float travelledDistance =
            Vector2.Distance(
                fleeStartPosition,
                rb.position
            );

        float destinationDistance =
            Vector2.Distance(
                rb.position,
                fleeTargetPosition
            );

        bool travelledEnough =
            travelledDistance >=
            fleeDistance *
            0.95f;

        bool reachedDestination =
            destinationDistance <=
            stopDistance;

        if (travelledEnough ||
            reachedDestination)
        {
            HoldPosition();

            navigationAgent
                ?.ClearDestination();

            return true;
        }

        /*
         * Flee source ignoreras vid avoidance.
         *
         * NPC:n ska inte försöka runda den karaktär den aktivt
         * försöker fly från om de fortfarande står nära varandra.
         */
        MoveTowardsInternal(
            fleeTargetPosition,
            fleeSpeedMultiplier,
            stopDistance,
            ignoredAvoidanceCharacter:
                fleeSource
        );

        return false;
    }

    public void EndFlee()
    {
        isFleeing =
            false;

        fleeSource =
            null;

        fleeSpeedMultiplier =
            1f;

        fleeStartPosition =
            Vector2.zero;

        fleeTargetPosition =
            Vector2.zero;

        navigationAgent
            ?.ClearDestination();

        ClearAvoidanceMemory();
    }

    // =========================================================
    // AGGRO
    // =========================================================

    public void UpdateAggroMovement(
        CharacterStats target,
        float attackRange,
        bool forceApproach = false,
        float customStopDistance = -1f)
    {
        if (target == null)
            return;

        SetMovementMode(
            NPCMovementMode.Aggressive
        );

        Vector2 targetPosition =
            TargetUtility.GetTargetPosition(
                target.gameObject
            );

        float desiredStopDistance;

        if (forceApproach)
        {
            desiredStopDistance =
                0.05f;
        }
        else if (customStopDistance >= 0f)
        {
            desiredStopDistance =
                customStopDistance;
        }
        else
        {
            desiredStopDistance =
                Mathf.Max(
                    0f,
                    attackRange *
                    0.9f
                );
        }

        /*
         * VIKTIGT:
         *
         * Combat-targetet ignoreras av character avoidance.
         *
         * Annars försöker chasern runda personen den faktiskt
         * försöker komma inom attack range till.
         */
        MoveTowardsInternal(
            targetPosition,
            1f,
            desiredStopDistance,
            ignoredAvoidanceCharacter:
                target
        );
    }

    public void UpdateAggroReposition(
        CharacterStats target)
    {
        if (target == null)
            return;

        SetMovementMode(
            NPCMovementMode.Aggressive
        );

        MoveTowardsInternal(
            target.transform.position,
            1f,
            0f,
            ignoredAvoidanceCharacter:
                target
        );
    }

    // =========================================================
    // RETURN
    // =========================================================

    public void UpdateReturnMovement(
        Vector3 returnPosition)
    {
        SetMovementMode(
            NPCMovementMode.Default
        );

        MoveTowardsInternal(
            returnPosition,
            1f,
            stopDistance,
            ignoredAvoidanceCharacter: null
        );
    }

    // =========================================================
    // PATROL
    // =========================================================

    public void StartPatrol()
    {
        SetMovementMode(
            NPCMovementMode.Patrol
        );

        waitingAtPatrolNode =
            false;

        patrolWaitTimer =
            0f;

        patrolForward =
            true;

        patrolIndex =
            0;

        navigationAgent
            ?.ClearDestination();

        ClearAvoidanceMemory();
    }

    /// <summary>
    /// Resume återställer INTE patrolIndex.
    ///
    /// Combat eller annan interruption kastar endast aktuell
    /// navigation. Patrol-progressen ligger kvar.
    /// </summary>
    public void ResumePatrol()
    {
        SetMovementMode(
            NPCMovementMode.Patrol
        );

        waitingAtPatrolNode =
            false;

        patrolWaitTimer =
            0f;

        /*
         * Viktigt:
         *
         * Gammal combat-/return-navigation får inte återanvändas.
         * Nästa UpdatePatrol bygger navigation direkt mot det
         * aktuella patrolIndex.
         */
        navigationAgent
            ?.ClearDestination();

        ClearAvoidanceMemory();
    }

    public void EndPatrol()
    {
        /*
         * patrolIndex och patrolForward bevaras.
         *
         * De representerar patrol-progress och ska överleva combat.
         */
        waitingAtPatrolNode =
            false;

        patrolWaitTimer =
            0f;

        navigationAgent
            ?.ClearDestination();

        ClearAvoidanceMemory();
    }

    public void UpdatePatrol(
        PatrolPath patrolPath)
    {
        if (!TryGetCurrentPatrolPoint(
                patrolPath,
                out PatrolPoint point))
        {
            HoldPosition();

            return;
        }

        SetMovementMode(
            NPCMovementMode.Patrol
        );

        if (waitingAtPatrolNode)
        {
            UpdatePatrolWait(
                patrolPath
            );

            return;
        }

        Vector2 nodePosition =
            point.transform.position;

        if (HasReachedPatrolPoint(
                nodePosition))
        {
            CompletePatrolPoint(
                patrolPath,
                point
            );

            return;
        }

        MoveTowardsInternal(
            nodePosition,
            patrolSpeedMultiplier,
            patrolNodeReachDistance,
            ignoredAvoidanceCharacter: null
        );
    }

    private bool TryGetCurrentPatrolPoint(
        PatrolPath patrolPath,
        out PatrolPoint point)
    {
        point =
            null;

        if (patrolPath == null ||
            patrolPath.points == null ||
            patrolPath.points.Count == 0)
        {
            return false;
        }

        patrolIndex =
            Mathf.Clamp(
                patrolIndex,
                0,
                patrolPath.points.Count - 1
            );

        point =
            patrolPath.points[
                patrolIndex
            ];

        return point != null;
    }

    private bool HasReachedPatrolPoint(
        Vector2 nodePosition)
    {
        float distance =
            Vector2.Distance(
                transform.position,
                nodePosition
            );

        if (distance <=
            patrolNodeReachDistance)
        {
            return true;
        }

        /*
         * Semantisk patrol-regel:
         *
         * Om destinationen bokstavligen är upptagen av en
         * karaktär behöver patrulleraren inte trycka sig in
         * i exakt samma koordinat.
         */
        if (distance >
            blockedPatrolNodeAcceptanceDistance)
        {
            return false;
        }

        return IsPatrolNodeBlockedByCharacter(
            nodePosition
        );
    }

    private void CompletePatrolPoint(
        PatrolPath patrolPath,
        PatrolPoint point)
    {
        HoldPosition();

        /*
         * Den gamla nodens navigation ska aldrig råka leva kvar
         * efter att patrolIndex bytts.
         */
        navigationAgent
            ?.ClearDestination();

        ClearAvoidanceMemory();

        float waitTime =
            point != null
                ? Mathf.Max(
                    0f,
                    point.waitTime
                )
                : 0f;

        if (waitTime <= 0f)
        {
            AdvancePatrolPoint(
                patrolPath
            );

            return;
        }

        waitingAtPatrolNode =
            true;

        patrolWaitTimer =
            waitTime;
    }

    private void UpdatePatrolWait(
        PatrolPath patrolPath)
    {
        HoldPosition();

        patrolWaitTimer -=
            Time.fixedDeltaTime;

        if (patrolWaitTimer > 0f)
            return;

        patrolWaitTimer =
            0f;

        waitingAtPatrolNode =
            false;

        AdvancePatrolPoint(
            patrolPath
        );

        navigationAgent
            ?.ClearDestination();
    }

    private void AdvancePatrolPoint(
        PatrolPath patrolPath)
    {
        if (patrolPath == null ||
            patrolPath.points == null ||
            patrolPath.points.Count <= 1)
        {
            patrolIndex =
                0;

            return;
        }

        if (patrolPath.patrolMode ==
            PatrolPath.PatrolMode.Loop)
        {
            patrolIndex++;

            if (patrolIndex >=
                patrolPath.points.Count)
            {
                patrolIndex =
                    0;
            }

            return;
        }

        if (patrolForward)
        {
            patrolIndex++;

            if (patrolIndex >=
                patrolPath.points.Count)
            {
                patrolIndex =
                    patrolPath.points.Count -
                    2;

                patrolForward =
                    false;
            }

            return;
        }

        patrolIndex--;

        if (patrolIndex < 0)
        {
            patrolIndex =
                1;

            patrolForward =
                true;
        }
    }

    private bool IsPatrolNodeBlockedByCharacter(
        Vector2 nodePosition)
    {
        if (patrolNodeBlockRadius <= 0f)
            return false;

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                nodePosition,
                patrolNodeBlockRadius,
                characterBuffer,
                CharacterLayers
            );

        for (int i = 0;
             i < hitCount;
             i++)
        {
            Collider2D hit =
                characterBuffer[i];

            if (hit == null)
                continue;

            CharacterStats other =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (other == null ||
                other == stats ||
                !other.IsAlive)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    // =========================================================
    // HOLD / STOP
    // =========================================================

    /// <summary>
    /// Pausar endast kroppen.
    ///
    /// Navigationen behålls.
    ///
    /// Används under:
    /// - attack/cast/recovery
    /// - cooldown-wait
    /// - tillfälliga pauses
    /// </summary>
    public void HoldPosition()
    {
        if (rb != null)
        {
            rb.linearVelocity =
                Vector2.zero;
        }

        StopVisualMovement();
    }

    /// <summary>
    /// Full movement-reset.
    ///
    /// Använd endast när navigationen faktiskt ska överges.
    /// </summary>
    public void Stop()
    {
        HoldPosition();

        navigationAgent
            ?.ClearDestination();

        ClearAvoidanceMemory();
    }

    private void StopVisualMovement()
    {
        visualController
            ?.SetMoving(
                false
            );
    }
}
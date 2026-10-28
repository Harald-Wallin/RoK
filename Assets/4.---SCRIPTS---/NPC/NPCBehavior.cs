using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PlayerAbilityCollection))]
public class NPCBehavior : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    private PlayerStats subscribedPlayer;

    [Header("Leash")]
    [SerializeField] protected float maxDistanceFromSpawn = 8f;

    [Header("Actions")]

    private CharacterActionController
    actionController;

    private PlayerAbilityCollection
        abilityCollection;

    [Header("Ability Delay")]
    [SerializeField] private float abilityDelayAfterAggro = 3f;
    private float abilityLockTimer;

    [Header("Movement")]
    protected NPCMovement movement;
    [SerializeField]
    float stopDistanceDefault = 1f;

    [Header("Aggro")]


    [SerializeField]
    protected bool canAggro = true;
    public float aggroRange = 4f;
    private float currentAggroRange;
    protected bool isAggro;
    protected bool isReturning;
    protected Vector3 spawnPosition;
    protected CharacterStats currentTargetStats;
    public bool HasTarget => currentTargetStats != null;
    public CharacterStats CurrentTarget => currentTargetStats;
    [SerializeField]
    [Tooltip(
    "Collider-lager som används när NPC:n söker efter hot. " +
    "Ska normalt inkludera Hitbox.")]



    private LayerMask threatDetectionLayers;

    [Header("Combat Chase")]

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip(
    "Hur stor del av attack range target måste lämna " +
    "innan NPC:n börjar jaga. 0.95 = 95% av range.")]

    private float combatChaseEnterFactor = 0.95f;

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip(
        "När en chase väl börjat fortsätter NPC:n tills target " +
        "är innanför denna del av attack range. " +
        "Måste vara lägre än Enter Factor.")]

    private float combatChaseExitFactor = 0.72f;

    private bool combatApproachActive;

    [Header("Wander")]
    [SerializeField] protected bool canWander = true;

    [Header("Patrol")]
    [SerializeField] protected bool canPatrol = false;
    [SerializeField] protected PatrolPath patrolPath;

    [Header("Flee")]
    private float activeFleeSpeedMultiplier = 1f;
    [SerializeField] protected float fleeDistance = 12f; //Första panic-jumpen
    [SerializeField] protected float safeDistanceFromThreat = 18f; //När NPC slutar springa
    [SerializeField] protected float resumeFleeDistance = 12f; //Om spelaren kommer närmre än detta > fly igen
    [SerializeField] protected float maxHoldDistanceFromSpawn = 50f; //Förhindrar oändlig flykt
    protected Vector3 fleeTargetPosition;
    protected CharacterStats fleeSource;

 //Low-health retreat är INTE samma sak som vanlig Flee.
 //Den är en tillfällig ompositionering inom samma encounter.
    private bool lowHealthRetreatActive;
    private CharacterStats lowHealthRetreatThreat;
    private Vector3 lowHealthRetreatStartPosition;

    [Header("Death & Respawn")]

    [SerializeField]
    [Tooltip(
    "Om NPC:n ska återuppstå efter död.")]
    private bool canRespawn = false;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Antal sekunder från död tills NPC:n återuppstår.")]
    private float respawnTime = 30f;

    private const float
        RespawnAggroGraceTime =
            2f;

    private bool isDead = false;

    private Quaternion spawnRotation;

    private float aggroDisableTimer;

    public bool CanRespawn =>
        canRespawn;

    public float RespawnTime =>
        Mathf.Max(
            0f,
            respawnTime
        );

    public Vector3 SpawnPosition =>
        spawnPosition;

    public Quaternion SpawnRotation =>
        spawnRotation;

    private EscortObjectiveRuntime
        activeEscortRuntime;

    private Transform
        escortDestination;

    private float
        escortStartDelayTimer;

    private const float
    EscortCombatRadius =
        5f;

    private const float
        EscortImmediateDefenseRadius =
            2.5f;

    private const float
        EscortBackwardTolerance =
            0.75f;

    private Vector3
        escortCombatAnchorPosition;

    private PlayerStats
        escortPlayerStats;

    private CharacterStateController
    escortPlayerState;

    private bool
    escortPostSuccessHolding;

    private float
        escortPostSuccessTimer;

    private bool
        escortHomeReturnInProgress;

    public bool IsFavourInteractionSuppressed =>
        escortHomeReturnInProgress;


    public bool IsBeingEscorted =>
        activeEscortRuntime != null &&
        activeEscortRuntime.IsEscorting;


    private bool wasPatrollingBeforeCombat;
    private bool restartPatrolOnNextEnter;
    private Vector3 combatAnchorPosition;
    private Vector3 encounterReturnPosition;

    private bool hasCombatAnchor;

    private float combatLeashRadius;

    private bool sharesRetreatLeash;

    public bool IsInCombat => currentState == AIState.Aggro;
    protected AIState currentState = AIState.Idle;
    public AIState CurrentState => currentState;
    public CharacterStats selfStats;

    private BuffSystem buffSystem;
    private NPCReactionController reactionController;
    private NPCThreatTracker threatTracker;

    private bool encounterResetInProgress;

    public bool IsEncounterResetting =>
        encounterResetInProgress ||
        currentState == AIState.Returning;

    //DEBUG
    private float nextCombatDebugTime;
    private const float CombatDebugInterval =
        0.25f;

    [Header("Re-aggro")]
    [SerializeField] private float reaggroCooldown = 0f;
    private float lastReturnTime = -999f;

    void Awake()
    {
        movement =
            GetComponent<NPCMovement>();

        actionController =
             GetComponent<CharacterActionController>();

        abilityCollection =
            GetComponent<PlayerAbilityCollection>();

        selfStats =
            GetComponent<CharacterStats>();

        buffSystem =
            GetComponent<BuffSystem>();

        reactionController =
            GetComponent<NPCReactionController>();

        threatTracker =
            GetComponent<NPCThreatTracker>();

        spawnPosition =
            transform.position;

        spawnRotation =
            transform.rotation;

        combatAnchorPosition =
            spawnPosition;

        encounterReturnPosition =
            spawnPosition;

        combatLeashRadius =
            maxDistanceFromSpawn;

        sharesRetreatLeash =
            false;

        if (selfStats != null)
        {
            selfStats.OnDamagedBy +=
                HandleDamaged;

            selfStats.OnDied +=
                HandleDeath;
        }

        currentAggroRange =
            aggroRange;
    }

    /// <summary>
    /// Utökningspunkt för specialiserade NPC-typer.
    ///
    /// Den generella damage-reaktionen styrs av
    /// NPCReactionController. Den här metoden ska därför inte
    /// starta vanlig Aggro/Flee-logik i basklassen.
    /// </summary>
    protected virtual void HandleDamaged(
        CharacterStats attacker)
    {
    }

    protected virtual void Start()
    {
        if (player == null)
            player = PlayerReference.Player?.transform;

        movement.SetFacing(Vector2.down);

        if (canPatrol && patrolPath != null && patrolPath.points.Count > 0)
        {
            EnterPatrolState(true);
        }
        else if (canWander)
        {
            EnterWanderState();
        }
        else
        {
            EnterIdleState();
        }
    }

    public void PrepareForRespawn()
    {
        aggroDisableTimer =
            Mathf.Max(
                aggroDisableTimer,
                RespawnAggroGraceTime
            );
    }

    public void SetPatrolPath(PatrolPath path)
    {
        patrolPath = path;

        if (canPatrol &&
            patrolPath != null &&
            patrolPath.points.Count > 0)
        {
            EnterPatrolState(true);
        }
    }

    void UpdateTimers()
    {
        if (aggroDisableTimer > 0f)
            aggroDisableTimer -= Time.fixedDeltaTime;

        if (abilityLockTimer > 0f)
            abilityLockTimer -= Time.fixedDeltaTime;
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            player =
                PlayerReference.Player
                    ?.transform;
        }

        UpdateTimers();

        if (escortPostSuccessHolding)
        {
            UpdateEscortPostSuccessHold();

            return;
        }

        if (IsBeingEscorted)
        {
            UpdateEscort();

            return;
        }

        UpdateCurrentState();

        HandleLeash();
    }

    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdleState();
                break;

            case AIState.Wandering:
                UpdateWanderState();
                break;

            case AIState.Patrolling:
                UpdatePatrolState();
                break;

            case AIState.Aggro:
                UpdateAggroState();
                break;

            case AIState.Returning:
                UpdateReturnState();
                break;

            case AIState.Fleeing:
                UpdateFleeState();
                break;

            case AIState.Holding:
                UpdateHoldingState();
                break;
        }
    }

    void ChangeState(AIState newState)
    {
        if (currentState == newState)
            return;

        ExitCurrentState();

        currentState = newState;

        EnterCurrentState();
    }

    void EnterCurrentState()
    {
        switch (currentState)
        {
            case AIState.Idle:
                movement.EndWander();
                movement.EndPatrol();
                movement.EndFlee();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Default);
                break;

            case AIState.Wandering:
                movement.BeginWander();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Wander);
                break;

            case AIState.Patrolling:
                if (restartPatrolOnNextEnter)
                {
                    movement.StartPatrol();
                }
                else
                {
                    movement.ResumePatrol();
                }

                restartPatrolOnNextEnter = false;
                break;

            case AIState.Aggro:
                movement.EndWander();
                movement.EndPatrol();
                movement.EndFlee();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Aggressive);
                break;

            case AIState.Returning:
                movement.EndWander();
                movement.EndPatrol();
                movement.EndFlee();
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Default);
                break;

            case AIState.Fleeing:
                movement.SetMovementMode(NPCMovement.NPCMovementMode.Flee);
                break;

            case AIState.Holding:
                movement.EndFlee();
                movement.Stop();
                break;
        }
    }

    void ExitCurrentState()
    {
        switch (currentState)
        {
            case AIState.Wandering:
                movement.EndWander();
                break;

            case AIState.Patrolling:
                movement.EndPatrol();
                break;

            case AIState.Fleeing:
                movement.EndFlee();
                break;
        }
    }

    void SetupIdle()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    void SetupWander()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    void SetupPatrol()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    void SetupReturn()
    {
        isAggro = false;

        isReturning = true;

        currentTargetStats = null;

        lastReturnTime = Time.time;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= HandleTargetDied;
            subscribedPlayer = null;
        }
    }

    void SetupHolding()
    {
        isAggro = false;
        isReturning = false;

        currentTargetStats = null;
    }

    private void SetupFlee(
    CharacterStats threat,
    float speedMultiplier)
    {
        if (threat == null)
            return;

        fleeSource =
            threat;

        isAggro =
            false;

        isReturning =
            false;

        currentTargetStats =
            null;

        activeFleeSpeedMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        movement.BeginFlee(
            threat,
            fleeDistance,
            safeDistanceFromThreat,
            activeFleeSpeedMultiplier
        );
    }


    void UpdateIdleState()
    {
        HandleAggroDetection();
    }

    protected virtual void EnterIdleState()
    {
        SetupIdle();
        ChangeState(AIState.Idle);
    }

    void UpdateWanderState()
    {
        movement.UpdateWander(spawnPosition);
        HandleAggroDetection();
    }
    protected virtual void EnterWanderState()
    {
        SetupWander();

        ChangeState(AIState.Wandering);
    }

    void UpdatePatrolState()
    {
        movement.UpdatePatrol(patrolPath);
        HandleAggroDetection();
    }
    protected virtual void EnterPatrolState(bool restartPatrol = false)
    {
        SetupPatrol();

        restartPatrolOnNextEnter = restartPatrol;

        if (currentState == AIState.Patrolling)
        {
            if (restartPatrol)
            {
                movement.StartPatrol();
            }
            else
            {
                movement.ResumePatrol();
            }

            restartPatrolOnNextEnter = false;
            return;
        }

        ChangeState(AIState.Patrolling);
    }

    private void SetCurrentCombatTarget(
    CharacterStats target)
    {
        if (currentTargetStats ==
            target)
        {
            return;
        }

        ResetCombatApproach();

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;

            subscribedPlayer =
                null;
        }

        currentTargetStats =
            target;

        player =
            target != null
                ? target.transform
                : PlayerReference.Player
                    ?.transform;

        PlayerStats playerTarget =
            target as PlayerStats;

        if (playerTarget != null)
        {
            SubscribeToPlayerDeath(
                playerTarget
            );
        }
    }

    /// <summary>
    /// Låter threat-listan välja vilket combat-target som bör
    /// användas.
    ///
    /// Returnerar false endast när inget giltigt threat finns kvar.
    /// </summary>
    private bool RefreshCombatTargetFromThreat()
    {
        if (threatTracker == null)
        {
            bool validCurrentTarget =
                currentTargetStats != null &&
                currentTargetStats.IsAlive;

            if (!validCurrentTarget)
            {
                DebugCombatState(
                    "NO THREAT TRACKER + INVALID TARGET"
                );
            }

            return validCurrentTarget;
        }

        CharacterStats previousTarget =
            currentTargetStats;

        CharacterStats preferredTarget =
            threatTracker.GetPreferredTarget(
                currentTargetStats
            );

        if (preferredTarget == null)
        {
            Debug.LogWarning(
                $"[COMBAT TARGET] {name} NO THREAT TARGET | " +
                $"previous=" +
                $"{(previousTarget != null ? previousTarget.name : "NULL")} | " +
                $"threatCount={threatTracker.ThreatSourceCount}",
                this
            );

            SetCurrentCombatTarget(
                null
            );

            return false;
        }

        if (preferredTarget !=
            currentTargetStats)
        {
           /* Debug.Log(
                $"[COMBAT TARGET] {name} SWITCH | " +
                $"from=" +
                $"{(currentTargetStats != null ? currentTargetStats.name : "NULL")} | " +
                $"to={preferredTarget.name} | " +
                $"threat={threatTracker.GetThreat(preferredTarget):F1}",
                this
            );*/

            SetCurrentCombatTarget(
                preferredTarget
            );
        }

        return true;
    }

    /// <summary>
    /// Returnerar combat-avstånd enligt samma grundprincip som
    /// actionsystemets targeting:
    ///
    /// caster origin -> targetens närmaste colliderpunkt.
    ///
    /// Detta gör att AI och TargetResolver inte längre har två
    /// olika uppfattningar om attack range.
    /// </summary>
    private float GetCombatDistance(
        CharacterStats target)
    {
        if (target == null)
        {
            return float.PositiveInfinity;
        }

        Vector2 origin =
            selfStats != null
                ? TargetUtility.GetTargetPosition(
                    selfStats.gameObject
                )
                : (Vector2)transform.position;

        Vector2 closestPoint =
            TargetUtility.GetClosestPoint(
                target.gameObject,
                origin
            );

        return Vector2.Distance(
            origin,
            closestPoint
        );
    }

    /// <summary>
    /// Combat chase använder hysteresis.
    ///
    /// NPC:n börjar inte och slutar inte gå vid exakt samma
    /// avstånd. Det förhindrar:
    ///
    /// stop -> go -> stop -> go
    ///
    /// när target rör sig längs attack-range-gränsen.
    /// </summary>
    private bool ShouldContinueCombatApproach(
        float distance,
        float desiredRange,
        out float stopDistance)
    {
        float safeRange =
            Mathf.Max(
                0.05f,
                desiredRange
            );

        float enterFactor =
            Mathf.Clamp01(
                combatChaseEnterFactor
            );

        float exitFactor =
            Mathf.Clamp(
                combatChaseExitFactor,
                0.05f,
                enterFactor
            );

        float enterDistance =
            safeRange *
            enterFactor;

        stopDistance =
            safeRange *
            exitFactor;

        if (combatApproachActive)
        {
            if (distance <=
                stopDistance)
            {
                combatApproachActive =
                    false;
            }

            return combatApproachActive;
        }

        if (distance >
            enterDistance)
        {
            combatApproachActive =
                true;

            return true;
        }

        return false;
    }

    private void ResetCombatApproach()
    {
        combatApproachActive =
            false;
    }

    private void EndCombatNaturally()
    {
        bool resumeEscort =
            IsBeingEscorted;

        bool resumePatrol =
            !resumeEscort &&
            wasPatrollingBeforeCombat &&
            canPatrol &&
            patrolPath != null &&
            patrolPath.points != null &&
            patrolPath.points.Count > 0;

        movement?.Stop();

        FinishEncounter();

        wasPatrollingBeforeCombat =
            false;

        if (resumeEscort)
        {
            EnterIdleState();

            return;
        }

        if (resumePatrol)
        {
            EnterPatrolState(
                false
            );

            return;
        }

        if (canWander)
        {
            EnterWanderState();

            return;
        }

        EnterIdleState();
    }

    private AbilityData GetActiveBaseAttack()
{
    if (abilityCollection == null)
        return null;

    return abilityCollection
        .GetActiveBaseAttack();
}

private float GetBaseAttackRange()
{
    AbilityData baseAttack =
        GetActiveBaseAttack();

    if (baseAttack == null ||
        baseAttack.TargetingSettings == null)
    {
        return movement != null
            ? movement.DefaultStopDistance
            : 1f;
    }

    return Mathf.Max(
        0f,
        baseAttack
            .TargetingSettings
            .Range
    );
}

private AbilityData[] GetEquippedAbilities()
{
    if (abilityCollection == null)
        return null;

    return abilityCollection
        .GetEquippedAbilities();
}

    void UpdateAggroState()
    {
        // =====================================================
        // TARGET VALIDATION
        // =====================================================

        if (!RefreshCombatTargetFromThreat())
        {
            EndCombatNaturally();

            return;
        }

        if (actionController == null)
        {
            movement.Stop();

            return;
        }

        // =====================================================
        // ACTIVE ACTION
        // =====================================================

        if (actionController.HasActiveAction)
        {
            movement.HoldPosition();

            FaceCurrentTarget();

            return;
        }

        // =====================================================
        // DISTANCE
        // =====================================================

        float distance =  GetCombatDistance(
            currentTargetStats
            );

        AbilityData desiredAction =
            SelectDesiredAction(
                distance
            );

        // =====================================================
        // NO CURRENTLY AVAILABLE ACTION
        // =====================================================


        if (desiredAction == null)
        {
            float fallbackRange = GetBaseAttackRange();

            bool shouldApproach =
                ShouldContinueCombatApproach(
                    distance,
                    fallbackRange,
                    out float chaseStopDistance
                );

            DebugCombatState(
                $"NO ACTION | " +
                $"distance={distance:F2} | " +
                $"range={fallbackRange:F2} | " +
                $"approach={shouldApproach} | " +
                $"stop={chaseStopDistance:F2}"
            );

            if (shouldApproach)
            {
                movement.UpdateAggroMovement(
                    currentTargetStats,
                    fallbackRange,
                    forceApproach: false,
                    customStopDistance:
                        chaseStopDistance
                );

                return;
            }

            movement.HoldPosition();

            FaceCurrentTarget();

            return;
        }

        // =====================================================
        // TARGETING SETTINGS
        // =====================================================

        AbilityTargetingSettings targeting =
            desiredAction.TargetingSettings;

        if (targeting == null)
        {
            movement.HoldPosition();

            return;
        }

        // =====================================================
        // SELF-TARGETED ACTION
        // =====================================================

        if (targeting.TargetingMode ==
            TargetingMode.Self)
        {
            movement.HoldPosition();

            TryStartNPCAction(
                desiredAction
            );

            return;
        }

        // =====================================================
        // APPROACH RANGE
        // =====================================================

        float desiredRange =
            Mathf.Max(
                targeting.Range,
                movement.DefaultStopDistance
            );

        bool shouldApproachForAction =
            ShouldContinueCombatApproach(
            distance,
            desiredRange,
            out float actionChaseStopDistance
            );

        if (shouldApproachForAction)
        {
            DebugCombatState(
                $"APPROACH | " +
                $"distance={distance:F2} | " +
                $"range={desiredRange:F2} | " +
                $"stop={actionChaseStopDistance:F2} | " +
                $"ability={desiredAction.name}"
            );

            movement.UpdateAggroMovement(
                currentTargetStats,
                desiredRange,
                forceApproach: false,
                customStopDistance:
                    actionChaseStopDistance
            );

            return;
        }

        // =====================================================
        // MINIMUM RANGE
        // =====================================================

        /*
         * Target ligger för nära den önskade abilityn.
         *
         * För närvarande försöker vi fallback-base-attack.
         *
         * Ranged repositioning kan läggas till separat senare.
         */
        if (distance <
            targeting.MinimumRange)
        {
            movement.HoldPosition();

            FaceCurrentTarget();

            HandleAttack(
                desiredAction,
                distance
            );

            return;
        }

        // =====================================================
        // CAN THE ACTION ACTUALLY HIT FROM HERE?
        // =====================================================

        /*
         * RANGE är inte samma sak som en giltig combat-position.
         *
         * Exempel:
         *
         * WOLF
         *   |
         * TREE
         *   |
         * PLAYER
         *
         * Vargen kan matematiskt vara inom 2 meter,
         * men trädet gör attacken ogiltig.
         *
         * Abilityns egna targetingregler avgör detta.
         */
        TargetingResult targetingResult =
            actionController
                .EvaluateTargeting(
                    desiredAction,
                    currentTargetStats
                );

        bool canAttackFromCurrentPosition =
            targetingResult != null &&
            targetingResult.IsValid;

        // =====================================================
        // INVALID COMBAT POSITION
        // =====================================================

        if (!canAttackFromCurrentPosition)
        {
            ResetCombatApproach();

            DebugCombatState(
    $"INVALID TARGETING | " +
    $"distance={distance:F2} | " +
    $"ability={desiredAction.name} | " +
    $"failure=" +
    $"{(targetingResult != null ? targetingResult.FailureReason.ToString() : "NULL RESULT")}"
);
            movement.UpdateAggroMovement(
                currentTargetStats,
                desiredRange,
                forceApproach: true
            );

            return;
        }

        // =====================================================
        // VALID COMBAT POSITION
        // =====================================================

        ResetCombatApproach();

        DebugCombatState(
    $"ATTACK POSITION | " +
    $"distance={distance:F2} | " +
    $"ability={desiredAction.name}"
);

        movement.HoldPosition();

        FaceCurrentTarget();

        bool attackStarted =
            HandleAttack(
                desiredAction,
                distance
            );

        if (!attackStarted)
        {
            DebugCombatState(
                $"ATTACK FAILED AFTER VALID TARGETING | " +
                $"distance={distance:F2} | " +
                $"ability={desiredAction.name}"
            );
        }

        if (attackStarted)
        {
            return;
        }
    }

    void SetupAggro(
    CharacterStats target)
    {
        encounterResetInProgress =
            false;

        threatTracker
            ?.EnsureThreat(
                target
            );

        SetCurrentCombatTarget(
            target
        );

        isAggro =
            true;

        isReturning =
            false;

        abilityLockTimer =
            abilityDelayAfterAggro;
    }

    protected virtual void EnterAggroState(
    CharacterStats target)
    {
        if (target == null)
            return;

        if (IsEncounterResetting)
            return;

        if (currentState ==
            AIState.Aggro)
        {
            /*
             * Redan i combat:
             *
             * Flytta INTE combat-anchor.
             *
             * Annars skulle en fiende kunna dra escorten längre och
             * längre bort genom att varje nytt hit flyttar ankaret.
             */
            threatTracker
                ?.EnsureThreat(
                    target
                );

            RefreshCombatTargetFromThreat();

            return;
        }

        bool startingEscortCombat =
            IsBeingEscorted;

        wasPatrollingBeforeCombat =
            !startingEscortCombat &&
            currentState ==
            AIState.Patrolling;

        if (startingEscortCombat)
        {
            /*
             * Escortens combat-anchor är exakt den punkt på färden
             * där striden börjar.
             */
            escortCombatAnchorPosition =
                transform.position;

            combatAnchorPosition =
                escortCombatAnchorPosition;

            encounterReturnPosition =
                escortCombatAnchorPosition;

            combatLeashRadius =
                EscortCombatRadius;

            hasCombatAnchor =
                true;

            sharesRetreatLeash =
                false;
        }
        else
        {
            /*
             * Vanligt NPC-combat behåller sitt tidigare beteende.
             */
            combatAnchorPosition =
                wasPatrollingBeforeCombat
                    ? transform.position
                    : spawnPosition;

            encounterReturnPosition =
                combatAnchorPosition;

            hasCombatAnchor =
                true;

            combatLeashRadius =
                maxDistanceFromSpawn;

            sharesRetreatLeash =
                false;
        }

        ChangeState(
            AIState.Aggro
        );

        SetupAggro(
            target
        );

        PlayerStats playerTarget =
            target as PlayerStats;

        if (playerTarget != null)
        {
            SubscribeToPlayerDeath(
                playerTarget
            );
        }
    }

    private void UpdateReturnState()
    {
        movement.UpdateReturnMovement(
            encounterReturnPosition
        );

        float distance =
            Vector2.Distance(
                transform.position,
                encounterReturnPosition
            );

        if (distance >
            movement.DefaultStopDistance)
        {
            return;
        }

        movement.Stop();

        CompleteEncounterReset();

        bool shouldResumePatrol =
                    wasPatrollingBeforeCombat &&
            canPatrol &&
            patrolPath != null &&
            patrolPath.points.Count > 0;

        wasPatrollingBeforeCombat = false;

        if (shouldResumePatrol)
        {
            EnterPatrolState(
                false
            );

            return;
        }

        if (canWander)
        {
            EnterWanderState();
            return;
        }

        EnterIdleState();
    }

    protected virtual void EnterReturnState()
    {
        SetupReturn();

        ChangeState(AIState.Returning);
    }

    protected virtual void EnterFleeState(
    CharacterStats threat,
    float speedMultiplier = 1f)
    {
        if (threat == null)
            return;

        SetupFlee(
            threat,
            speedMultiplier
        );

        ChangeState(
            AIState.Fleeing
        );
    }

    protected virtual void EnterHoldingState()
    {
        SetupHolding();

        ChangeState(AIState.Holding);
    }

    public bool TryGetSharedRetreatLeash(
        out Vector2 origin,
        out float radius)
    {
        origin =
            combatAnchorPosition;

        radius =
            Mathf.Max(
                0f,
                combatLeashRadius
            );

        if (!sharesRetreatLeash)
            return false;

        if (!hasCombatAnchor)
            return false;

        if (IsEncounterResetting)
            return false;

        if (selfStats == null ||
            !selfStats.IsAlive)
        {
            return false;
        }

        return radius > 0f;
    }

    private void HandleLeash()
    {
        if (currentState !=
            AIState.Aggro)
        {
            return;
        }

        if (IsEncounterResetting)
            return;

        Vector2 leashOrigin =
            hasCombatAnchor
                ? (Vector2)combatAnchorPosition
                : (Vector2)spawnPosition;

        float ownLeashRadius =
            hasCombatAnchor
                ? Mathf.Max(
                    0f,
                    combatLeashRadius
                )
                : Mathf.Max(
                    0f,
                    maxDistanceFromSpawn
                );

        float distanceFromLeashOrigin =
            Vector2.Distance(
                transform.position,
                leashOrigin
            );

        // =========================================================
        // STILL INSIDE OUR OWN ENCOUNTER
        // =========================================================

        if (distanceFromLeashOrigin <=
            ownLeashRadius)
        {
            return;
        }

        // =========================================================
        // TARGET HAS AN ACTIVE RETREAT ENCOUNTER AREA
        // =========================================================

        if (currentTargetStats != null)
        {
            NPCBehavior targetAI =
                currentTargetStats.GetComponent<
                    NPCBehavior>();

            if (targetAI != null &&
                targetAI != this &&
                targetAI.TryGetSharedRetreatLeash(
                    out Vector2 sharedOrigin,
                    out float sharedRadius))
            {
                float distanceFromSharedArea =
                    Vector2.Distance(
                        transform.position,
                        sharedOrigin
                    );

                if (distanceFromSharedArea <=
                    sharedRadius)
                {
                    return;
                }
            }
        }

        // =========================================================
        // CURRENT TARGET PULLED US OUTSIDE THE ENCOUNTER
        // =========================================================

        if (threatTracker != null)
        {
            CharacterStats replacementTarget =
                threatTracker
                    .GetHighestThreatTargetWithinRange(
                        leashOrigin,
                        ownLeashRadius,
                        currentTargetStats
                    );

            if (replacementTarget != null)
            {
                if (currentTargetStats != null)
                {
                    threatTracker.RemoveThreat(
                        currentTargetStats
                    );
                }

                SetCurrentCombatTarget(
                    replacementTarget
                );

                movement?.Stop();

                return;
            }
        }

        // =========================================================
        // NO VALID ENCOUNTER TARGET REMAINS
        // =========================================================

        Debug.LogWarning(
            $"[COMBAT LEASH] {name} LEASH RESET | " +
            $"distanceFromAnchor={distanceFromLeashOrigin:F2} | " +
            $"max={ownLeashRadius:F2} | " +
            $"target=" +
            $"{(currentTargetStats != null ? currentTargetStats.name : "NULL")} | " +
            $"threats=" +
            $"{(threatTracker != null ? threatTracker.ThreatSourceCount : -1)}",
            this
        );

        BeginEncounterResetAndReturn();
    }

    private void BeginEncounterResetAndReturn()
    {
        if (encounterResetInProgress)
            return;

        bool wasInEncounter =
            currentState == AIState.Aggro ||
            currentState == AIState.Fleeing ||
            currentState == AIState.Holding;

        if (!wasInEncounter)
            return;

        encounterResetInProgress =
            true;

        actionController
            ?.ResetRuntimeState();

        threatTracker
            ?.ResetThreat();

        movement?.Stop();

        lowHealthRetreatActive =
            false;

        lowHealthRetreatThreat =
            null;

        lowHealthRetreatStartPosition =
            Vector3.zero;

        fleeSource =
            null;

        ResetCombatApproach();

        currentTargetStats =
            null;

        activeFleeSpeedMultiplier =
            1f;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;

            subscribedPlayer =
                null;
        }

        aggroDisableTimer =
            Mathf.Max(
                aggroDisableTimer,
                reaggroCooldown
            );

        lastReturnTime =
            Time.time;

        isAggro =
            false;

        isReturning =
            true;

        /*
         * Behåll encounterReturnPosition som sattes när encounter
         * startade.
         *
         * En patrullerande NPC går därmed tillbaka till sin
         * encounter-anchor. En stationär NPC använder spawnpunkten.
         */
        EnterReturnState();
    }

    private void FinishEncounter()
    {
        /*
         * Gemensam encounter-cleanup utan att bestämma
         * locomotion-state.
         */
        actionController
            ?.ResetRuntimeState();

        buffSystem
            ?.RemoveEncounterResetBuffs();

        selfStats
            ?.ResetEncounterState();

        reactionController
            ?.ResetEncounterState();

        threatTracker
            ?.ResetThreat();

        fleeSource =
            null;

        SetCurrentCombatTarget(
            null
        );

        lowHealthRetreatActive =
            false;

        lowHealthRetreatThreat =
            null;

        lowHealthRetreatStartPosition =
            Vector3.zero;

        activeFleeSpeedMultiplier =
            1f;

        isAggro =
            false;

        isReturning =
            false;

        hasCombatAnchor =
            false;

        encounterResetInProgress =
            false;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;

            subscribedPlayer =
                null;
        }
    }

    private void CompleteEncounterReset()
    {
        buffSystem
            ?.RemoveEncounterResetBuffs();

        selfStats
            ?.ResetEncounterState();

        reactionController
            ?.ResetEncounterState();

        threatTracker
            ?.ResetThreat();

        fleeSource =
            null;

        ResetCombatApproach();

        currentTargetStats =
            null;

        lowHealthRetreatActive =
            false;

        lowHealthRetreatThreat =
            null;

        lowHealthRetreatStartPosition =
            Vector3.zero;

        activeFleeSpeedMultiplier =
            1f;

        isAggro =
            false;

        isReturning =
            false;

        hasCombatAnchor =
            false;

        encounterResetInProgress =
            false;

        hasCombatAnchor =
            false;

        combatLeashRadius =
            maxDistanceFromSpawn;

        sharesRetreatLeash =
            false;

        encounterResetInProgress =
            false;

        escortHomeReturnInProgress =
            false;

        escortPostSuccessHolding =
            false;

        escortPostSuccessTimer =
            0f;
    }

    // =========================================================
    // ESCORT
    // =========================================================

    public bool TryBeginEscort(
    EscortObjectiveRuntime runtime,
    Transform destination)
    {
        if (runtime == null ||
            destination == null)
        {
            return false;
        }

        if (selfStats == null ||
            !selfStats.IsAlive)
        {
            return false;
        }

        /*
         * Vi startar fortfarande inte en escort mitt under ett
         * redan existerande encounter.
         */
        if (currentState ==
                AIState.Aggro ||
            currentState ==
                AIState.Fleeing ||
            currentState ==
                AIState.Holding ||
            IsEncounterResetting)
        {
            Debug.LogWarning(
                $"NPC '{name}' kan inte börja escort just nu " +
                $"eftersom den befinner sig i state {currentState}.",
                this
            );

            return false;
        }

        if (activeEscortRuntime != null)
        {
            Debug.LogWarning(
                $"NPC '{name}' har redan en aktiv escort-session.",
                this
            );

            return false;
        }

        activeEscortRuntime =
            runtime;

        escortDestination =
            destination;

        escortStartDelayTimer =
            Mathf.Max(
                0f,
                runtime.StartDelay
            );

        escortCombatAnchorPosition =
            transform.position;

        EnterIdleState();

        movement?.Stop();

        SubscribeToEscortPlayerDamage();

        return true;
    }


    private void UpdateEscort()
    {
        if (activeEscortRuntime == null)
        {
            ClearEscortRuntime();

            return;
        }

        if (!activeEscortRuntime.IsEscorting)
        {
            ClearEscortRuntime();

            return;
        }

        if (selfStats == null ||
            !selfStats.IsAlive)
        {
            movement?.HoldPosition();

            return;
        }

        if (escortDestination == null)
        {
            movement?.HoldPosition();

            return;
        }

        /*
         * PlayerReference kan i teorin ha blivit tillgänglig efter
         * att escort-sessionen startade.
         */
        if (escortPlayerStats == null)
        {
            SubscribeToEscortPlayerDamage();
        }

        // =========================================================
        // PLAYER DISTANCE FAILURE
        // =========================================================

        /*
         * Distance-regeln gäller under HELA den aktiva färden:
         *
         * - start delay
         * - vanlig movement
         * - combat
         * - player combat pause
         *
         * Combat är alltså inte ett sätt att kringgå escort-radius.
         */
        if (HasEscortPlayerExceededMaximumDistance())
        {
            FailActiveEscort();

            return;
        }

        // =========================================================
        // ESCORT NPC COMBAT
        // =========================================================

        /*
         * Om NPC:n själv fortfarande har ett giltigt lokalt
         * combat-target ska den slåss.
         */
        if (currentState ==
            AIState.Aggro)
        {
            UpdateEscortCombat();

            return;
        }

        /*
         * Escort får aldrig ligga kvar i vanlig:
         *
         * - Flee
         * - Holding
         * - Returning
         *
         * medan själva escort-sessionen fortfarande är aktiv.
         */
        if (currentState ==
                AIState.Fleeing ||
            currentState ==
                AIState.Holding ||
            currentState ==
                AIState.Returning)
        {
            movement?.Stop();

            EnterIdleState();
        }

        // =========================================================
        // PLAYER COMBAT PAUSE
        // =========================================================

        /*
         * NPC:n kan ha släppt sitt eget combat-target därför att
         * fienden lämnade EscortCombatRadius.
         *
         * Men om SPELAREN fortfarande är i combat ska escorten
         * inte börja gå vidare.
         *
         * NPC:n väntar då där den är tills spelarens riktiga
         * CharacterStateController lämnar combat.
         */
        if (escortPlayerState != null &&
            escortPlayerState.InCombat)
        {
            movement?.HoldPosition();

            return;
        }

        // =========================================================
        // START DELAY
        // =========================================================

        if (escortStartDelayTimer > 0f)
        {
            escortStartDelayTimer =
                Mathf.Max(
                    0f,
                    escortStartDelayTimer -
                    Time.fixedDeltaTime
                );

            movement?.HoldPosition();

            return;
        }

        // =========================================================
        // DESTINATION CHECK
        // =========================================================

        float destinationDistance =
            Vector2.Distance(
                transform.position,
                escortDestination.position
            );

        float arrivalDistance =
            movement != null
                ? movement.DefaultStopDistance
                : 0.8f;

        if (destinationDistance <=
            arrivalDistance)
        {
            CompleteEscortTravel();

            return;
        }

        // =========================================================
        // ESCORT MOVEMENT
        // =========================================================

        if (movement == null)
            return;

        movement.MoveTowards(
            escortDestination.position,
            activeEscortRuntime
                .MovementSpeedMultiplier,
            arrivalDistance
        );
    }

    private void UpdateEscortCombat()
    {
        /*
         * Escort använder fortfarande det vanliga action/combat-
         * systemet.
         *
         * Skillnaden är endast vilka combat-targets NPC:n tillåts
         * följa.
         */
        if (!RefreshEscortCombatTarget())
        {
            EndCombatNaturally();

            return;
        }

        UpdateAggroState();
    }


    public bool TryStartEscortCombat(
        CharacterStats threat)
    {
        if (!IsBeingEscorted ||
            threat == null ||
            threat == selfStats ||
            selfStats == null ||
            !selfStats.IsAlive ||
            !threat.IsAlive)
        {
            return false;
        }

        /*
         * Friendly fire ska inte dra in escort-NPC:n i strid mot
         * den egna factionen.
         */
        if (selfStats.faction != null &&
            threat.faction != null &&
            selfStats.faction ==
            threat.faction)
        {
            return false;
        }

        Vector2 anchor =
            currentState ==
                    AIState.Aggro &&
            hasCombatAnchor
                ? (Vector2)combatAnchorPosition
                : (Vector2)transform.position;

        if (!IsEscortCombatTargetAllowed(
                threat,
                anchor))
        {
            return false;
        }

        threatTracker
            ?.EnsureThreat(
                threat
            );

        ForceAggro(
            threat
        );

        return
            currentState ==
            AIState.Aggro;
    }


    private bool RefreshEscortCombatTarget()
    {
        if (threatTracker == null)
        {
            return
                currentTargetStats != null &&
                IsEscortCombatTargetAllowed(
                    currentTargetStats,
                    combatAnchorPosition
                );
        }

        const int maximumChecks =
            16;

        for (int i = 0;
             i < maximumChecks;
             i++)
        {
            CharacterStats preferredTarget =
                threatTracker.GetPreferredTarget(
                    currentTargetStats
                );

            if (preferredTarget == null)
            {
                SetCurrentCombatTarget(
                    null
                );

                return false;
            }

            if (IsEscortCombatTargetAllowed(
                    preferredTarget,
                    combatAnchorPosition))
            {
                if (preferredTarget !=
                    currentTargetStats)
                {
                    SetCurrentCombatTarget(
                        preferredTarget
                    );
                }

                return true;
            }

            threatTracker.RemoveThreat(
                preferredTarget
            );

            if (preferredTarget ==
                currentTargetStats)
            {
                SetCurrentCombatTarget(
                    null
                );
            }
        }

        SetCurrentCombatTarget(
            null
        );

        return false;
    }


    private bool IsEscortCombatTargetAllowed(
        CharacterStats target,
        Vector2 combatAnchor)
    {
        if (target == null ||
            !target.IsAlive ||
            escortDestination == null)
        {
            return false;
        }

        Vector2 targetPosition =
            target.transform.position;

        float distanceFromCombatAnchor =
            Vector2.Distance(
                combatAnchor,
                targetPosition
            );

        if (distanceFromCombatAnchor >
            EscortCombatRadius)
        {
            return false;
        }

        if (distanceFromCombatAnchor <=
            EscortImmediateDefenseRadius)
        {
            return true;
        }

        float anchorDistanceToDestination =
            Vector2.Distance(
                combatAnchor,
                escortDestination.position
            );

        float targetDistanceToDestination =
            Vector2.Distance(
                targetPosition,
                escortDestination.position
            );

        return
            targetDistanceToDestination <=
            anchorDistanceToDestination +
            EscortBackwardTolerance;
    }

    private void UpdateEscortPostSuccessHold()
    {
        movement?.HoldPosition();

        if (escortPostSuccessTimer > 0f)
        {
            escortPostSuccessTimer =
                Mathf.Max(
                    0f,
                    escortPostSuccessTimer -
                    Time.fixedDeltaTime
                );

            return;
        }

        BeginEscortHomeReturn();
    }

    private void BeginEscortHomeReturn()
    {
        if (escortHomeReturnInProgress)
            return;

        escortPostSuccessHolding =
            false;

        escortPostSuccessTimer =
            0f;

        encounterReturnPosition =
            spawnPosition;


        encounterResetInProgress =
            true;

        escortHomeReturnInProgress =
            true;

        wasPatrollingBeforeCombat =
            false;

        movement?.Stop();

        FinishEncounter();

        EnterReturnState();
    }

    private void CompleteEscortTravel()
    {
        if (activeEscortRuntime == null)
            return;

        EscortObjectiveRuntime completedEscort =
            activeEscortRuntime;

        float postSuccessStaySeconds =
            completedEscort
                .PostSuccessStaySeconds;

        movement?.Stop();


        ClearEscortRuntime();

        completedEscort
            .MarkDestinationReached();

        escortPostSuccessHolding =
            true;

        escortPostSuccessTimer =
            Mathf.Max(
                0f,
                postSuccessStaySeconds
            );

        EnterIdleState();

        movement?.HoldPosition();


        if (escortPostSuccessTimer <=
            0f)
        {
            BeginEscortHomeReturn();
        }
    }

    private void SubscribeToEscortPlayerDamage()
    {
        UnsubscribeFromEscortPlayerDamage();

        escortPlayerStats =
            PlayerReference.Player;

        if (escortPlayerStats == null)
            return;

        escortPlayerState =
            escortPlayerStats.GetComponent<
                CharacterStateController>();

        escortPlayerStats.OnDamagedBy +=
            HandleEscortPlayerDamaged;
    }


    private void UnsubscribeFromEscortPlayerDamage()
    {
        if (escortPlayerStats != null)
        {
            escortPlayerStats.OnDamagedBy -=
                HandleEscortPlayerDamaged;
        }

        escortPlayerStats =
            null;

        escortPlayerState =
            null;
    }

    private bool HasEscortPlayerExceededMaximumDistance()
    {
        if (activeEscortRuntime == null)
            return false;

        if (escortPlayerStats == null)
            return false;

        float maximumDistance =
            activeEscortRuntime
                .MaximumPlayerDistance;

        if (maximumDistance <= 0f)
            return false;

        float distanceToPlayer =
            Vector2.Distance(
                transform.position,
                escortPlayerStats.transform.position
            );

        return
            distanceToPlayer >
            maximumDistance;
    }


    private void HandleEscortPlayerDamaged(
        CharacterStats attacker)
    {
        if (!IsBeingEscorted ||
            attacker == null)
        {
            return;
        }

        TryStartEscortCombat(
            attacker
        );
    }


    private void ClearEscortRuntime()
    {
        UnsubscribeFromEscortPlayerDamage();

        activeEscortRuntime =
            null;

        escortDestination =
            null;

        escortStartDelayTimer =
            0f;

        escortCombatAnchorPosition =
            Vector3.zero;
    }

    private void HandleAggroDetection()
    {
        if (aggroDisableTimer > 0f)
            return;

        if (Time.time - lastReturnTime <
            reaggroCooldown)
        {
            return;
        }

        if (IsEncounterResetting ||
            isReturning)
        {
            return;
        }

        if (currentState == AIState.Aggro ||
            currentState == AIState.Fleeing ||
            currentState == AIState.Holding)
        {
            return;
        }

        if (reactionController == null)
        {
            reactionController =
                GetComponent<
                    NPCReactionController>();
        }

        if (reactionController == null ||
            reactionController.ReactionType ==
            NPCReactionType.None)
        {
            return;
        }

        if (reactionController.ReactionType ==
                NPCReactionType.Aggro &&
            !canAggro)
        {
            return;
        }

        float detectionRadius =
            reactionController
                .CurrentAwarenessRadius;

        if (detectionRadius <= 0f)
            return;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                detectionRadius
            );

        HashSet<CharacterStats>
            checkedCharacters =
                new();

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider2D hit =
                hits[i];

            if (hit == null)
                continue;

            CharacterStats threat =
                hit.GetComponentInParent<
                    CharacterStats>();

            if (threat == null ||
                threat == selfStats)
            {
                continue;
            }

            if (!checkedCharacters.Add(
                    threat))
            {
                continue;
            }

            if (!threat.IsAlive)
                continue;

            if (!LineOfSightUtility
                    .HasLineOfSight(
                        transform.position,
                        threat.transform.position))
            {
                continue;
            }

            bool reacted =
                reactionController
                    .TryReactToProximityThreat(
                        threat
                    );

            if (reacted)
                return;
        }
    }

    private void FailActiveEscort()
    {
        if (activeEscortRuntime == null)
            return;

        EscortObjectiveRuntime failedEscort =
            activeEscortRuntime;

        movement?.Stop();

        FinishEncounter();

        failedEscort.Favour?.TryFail();

        ClearEscortRuntime();


        encounterReturnPosition =
            spawnPosition;

        encounterResetInProgress =
             true;

        escortHomeReturnInProgress =
            true;

        escortPostSuccessHolding =
            false;

        escortPostSuccessTimer =
            0f;

        wasPatrollingBeforeCombat =
            false;

        movement?.Stop();

        EnterReturnState();
    }

    private void FailActiveEscortOnDeath()
    {
        if (activeEscortRuntime == null)
            return;

        EscortObjectiveRuntime failedEscort =
            activeEscortRuntime;

        movement?.Stop();

        FinishEncounter();

        failedEscort.Favour?.TryFail();

        ClearEscortRuntime();
    }

    private void UpdateFleeState()
    {
        if (currentState !=
            AIState.Fleeing)
        {
            return;
        }

        bool fleeFinished =
            movement.UpdateFlee();

        if (!fleeFinished)
            return;

        movement.EndFlee();


        if (lowHealthRetreatActive)
        {
            CompleteLowHealthRetreat();

            return;
        }

        // Vanlig Flee-reaction behåller det gamla beteendet.

        EnterHoldingState();
    }

    private void CompleteLowHealthRetreat()
    {
        CharacterStats previousThreat =
            lowHealthRetreatThreat;

        lowHealthRetreatActive =
            false;

        lowHealthRetreatThreat =
            null;

        fleeSource =
            null;

        activeFleeSpeedMultiplier =
            1f;


        if (previousThreat != null &&
            !previousThreat.IsAlive)
        {
            threatTracker
                ?.RemoveThreat(
                    previousThreat
                );
        }


        if (RefreshCombatTargetFromThreat())
        {
            ResumeAggroAfterLowHealthRetreat(
                currentTargetStats
            );

            return;
        }

        BeginEncounterResetAndReturn();
    }

    private void ResumeAggroAfterLowHealthRetreat(
    CharacterStats threat)
    {
        if (threat == null ||
            !threat.IsAlive)
        {
            encounterReturnPosition =
                spawnPosition;

            BeginEncounterResetAndReturn();

            return;
        }

        currentTargetStats =
            threat;

        player =
            threat.transform;

        isAggro =
            true;

        isReturning =
            false;

        /*
         * VIKTIGT:
         *
         * Vi anropar INTE EnterAggroState här.
         *
         * EnterAggroState skapar normalt ett nytt encounter-anchor.
         * Efter retreat ska ankaret däremot fortsätta vara platsen
         * där retreaten började.
         */
        ChangeState(
            AIState.Aggro
        );

        PlayerStats playerTarget =
            threat as PlayerStats;

        if (playerTarget != null)
        {
            SubscribeToPlayerDeath(
                playerTarget
            );
        }
    }

    void UpdateHoldingState()
    {
        if (currentState != AIState.Holding)
            return;

        if (fleeSource == null)
            return;

        float distanceToThreat =
            Vector2.Distance(
                transform.position,
                fleeSource.transform.position
            );

        if (distanceToThreat <= resumeFleeDistance)
        {
            EnterFleeState(
                fleeSource,
                activeFleeSpeedMultiplier
            );
            return;
        }

        float distanceFromSpawn =
            Vector2.Distance(
                transform.position,
                spawnPosition
            );

        if (distanceFromSpawn >= maxHoldDistanceFromSpawn)
        {
            ReturnToSpawn();
        }

        NPCReactionController reaction = GetComponent<NPCReactionController>();

        if (reaction == null)
            return;

        bool factionStillAlerted =
            FactionAwarenessSystem.Instance != null &&
            FactionAwarenessSystem.Instance
                .IsFactionAlerted(reaction.Faction);

        if (!factionStillAlerted)
        {
            ReturnToSpawn();
        }
    }

    private bool HandleAttack(
    AbilityData desiredAction,
    float distanceToTarget)
    {
        if (desiredAction == null)
            return false;

        if (actionController == null)
            return false;

        if (selfStats == null ||
            !selfStats.CanAct())
        {
            return false;
        }

        if (!isAggro ||
            isReturning ||
            currentTargetStats == null)
        {
            return false;
        }

        if (currentState ==
                AIState.Fleeing ||
            currentState ==
                AIState.Holding)
        {
            return false;
        }

        AbilityTargetingSettings targeting =
            desiredAction.TargetingSettings;

        if (targeting == null)
            return false;

        bool isSelfTargeted =
            targeting.TargetingMode ==
            TargetingMode.Self;

        if (!isSelfTargeted)
        {
            if (distanceToTarget >
                targeting.Range)
            {
                return false;
            }

            if (distanceToTarget <
                targeting.MinimumRange)
            {
                return TryFallbackBaseAttack(
                    distanceToTarget
                );
            }
        }

        bool started =
            TryStartNPCAction(
                desiredAction
            );

        if (started)
            return true;

        /*
         * Abilityn gick inte att använda från nuvarande position.
         *
         * Exempel:
         * - World blockerar Line of Sight
         * - targetingen är spatialt ogiltig
         * - abilityn kan inte påverka target härifrån
         *
         * NPCBehavior får då fortsätta navigationen istället för
         * att fastna i "inom range = stå still".
         */
        if (!desiredAction.IsBaseAttack)
        {
            return TryFallbackBaseAttack(
                distanceToTarget
            );
        }

        return false;
    }

    bool TryStartNPCAction(
    AbilityData ability)
    {
        if (ability == null)
            return false;

        if (actionController == null)
            return false;

        if (actionController.HasActiveAction)
            return false;

        bool started;

        if (ability.TargetingSettings != null &&
            ability.TargetingSettings.TargetingMode ==
            TargetingMode.Self)
        {
            started =
                actionController.TryStartAction(
                    ability
                );
        }
        else
        {
            if (currentTargetStats == null)
                return false;

            started =
                actionController.TryStartAction(
                    ability,
                    currentTargetStats
                );
        }

        if (!started)
            return false;

        /*
         * Spelaren bekräftar en Confirmed-action genom input.
         * NPC:n har ingen sådan input och bekräftar därför sin
         * targeting automatiskt.
         */
        if (actionController.IsPreviewing)
        {
            bool confirmed =
                actionController
                    .ConfirmCurrentAction();

            if (!confirmed)
            {
                actionController
                    .CancelCurrentAction();

                return false;
            }
        }

        return true;
    }

    private bool TryFallbackBaseAttack(
    float distanceToTarget)
{
    if (actionController == null)
        return false;

    AbilityData baseAttack =
        GetActiveBaseAttack();

    if (!CanSelectBaseAttack(
            baseAttack))
    {
        return false;
    }

    AbilityTargetingSettings targeting =
        baseAttack.TargetingSettings;

    if (targeting == null)
        return false;

    if (distanceToTarget >
        targeting.Range)
    {
        return false;
    }

    if (distanceToTarget <
        targeting.MinimumRange)
    {
        return false;
    }

    return TryStartNPCAction(
        baseAttack
    );
}

    private AbilityData SelectDesiredAction(
    float distanceToTarget)
    {
        if (actionController == null ||
            abilityCollection == null)
        {
            return null;
        }

        /*
         * Vanliga abilities får en kort delay efter
         * encounter-start.
         *
         * Base Attack är fortfarande tillgänglig direkt.
         */
        if (abilityLockTimer <= 0f)
        {
            AbilityData[] abilities =
                GetEquippedAbilities();

            if (abilities != null)
            {
                for (int i = 0;
                     i < abilities.Length;
                     i++)
                {
                    AbilityData ability =
                        abilities[i];

                    if (!CanSelectAbility(
                            ability,
                            distanceToTarget))
                    {
                        continue;
                    }

                    return ability;
                }
            }
        }

        AbilityData baseAttack =
            GetActiveBaseAttack();

        if (!CanSelectBaseAttack(
                baseAttack))
        {
            return null;
        }

        return baseAttack;
    }

    private bool CanSelectBaseAttack(
    AbilityData attack)
    {
        if (attack == null)
            return false;

        if (!attack.IsBaseAttack)
            return false;

        if (!attack.UsesActionSettings)
            return false;

        if (attack.TargetingSettings == null)
            return false;

        if (attack.TimingSettings == null)
            return false;

        if (actionController == null)
            return false;

        return actionController
                   .GetCooldownRemaining(
                       attack
                   ) <= 0f;
    }

    bool CanSelectAbility(
    AbilityData ability,
    float distanceToTarget)
    {
        if (ability == null)
            return false;

        /*
         * Base attacks ägs av base attack-slotten och ska inte även
         * ligga bland NPC:ns vanliga equipped abilities.
         */
        if (ability.IsBaseAttack)
            return false;

        /*
         * NPC-migreringen använder endast abilities som har flyttats
         * till det nya actionsystemet.
         */
        if (!ability.UsesActionSettings)
            return false;

        if (ability.TargetingSettings == null)
            return false;

        if (ability.TimingSettings == null)
            return false;

        if (actionController
                .GetCooldownRemaining(
                    ability
                ) > 0f)
        {
            return false;
        }

        AbilityTargetingSettings targeting =
            ability.TargetingSettings;

        /*
         * Self-actions påverkas inte av avståndet till fienden.
         */
        if (targeting.TargetingMode ==
            TargetingMode.Self)
        {
            return true;
        }

        /*
         * NPCMovement kan i den här första versionen gå närmare ett
         * mål, men har ännu inget system för att backa till minimum
         * range.
         *
         * Därför väljer vi inte en ability när NPC:n redan står
         * innanför dess minimum range.
         */
        if (distanceToTarget <
            targeting.MinimumRange)
        {
            return false;
        }

        return true;
    }

    void FaceCurrentTarget()
    {
        if (movement == null ||
            currentTargetStats == null)
        {
            return;
        }

        Vector2 direction =
            (Vector2)currentTargetStats
                .transform
                .position -
            (Vector2)transform.position;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        movement.SetFacing(
            direction.normalized
        );
    }

    protected virtual bool ShouldAggro(CharacterStats potentialTarget)
    {
        if (potentialTarget == null)
            return false;

        if (potentialTarget == selfStats)
            return false;

        // Central regel:
        // Avgör först om detta överhuvudtaget är ett giltigt mål.
        if (!CombatTargeting.CanAttack(selfStats, potentialTarget))
            return false;

        // Därefter krävs fri sikt.
        if (!LineOfSightUtility.HasLineOfSight(
                transform.position,
                potentialTarget.transform.position))
            return false;

        return true;
    }

    public bool IsAggroOnPlayer()
    {
        if (currentState != AIState.Aggro)
            return false;

        return currentTargetStats is PlayerStats;
    }

    public void ForceAggro(
    CharacterStats target)
    {
        if (target == null ||
            !target.IsAlive)
        {
            return;
        }

        threatTracker
            ?.EnsureThreat(
                target
            );

        /*
         * Är NPC:n redan i combat bestämmer ThreatTracker om
         * target faktiskt bör bytas.
         */
        if (currentState ==
            AIState.Aggro)
        {
            RefreshCombatTargetFromThreat();

            return;
        }

        EnterAggroState(
            target
        );
    }

    public void StartFleeing(
    CharacterStats threat,
    float speedMultiplier = 1f)
    {
        if (threat == null)
            return;

        EnterFleeState(
            threat,
            speedMultiplier
        );
    }

    /// <summary>
    /// Startar en taktisk low-health retreat.
    ///
    /// NPC:n flyr en låst sträcka bort från threat men lämnar
    /// INTE sitt encounter.
    ///
    /// Punkten där retreaten började blir därefter encounterts
    /// nya leash-center.
    /// </summary>
    public void StartLowHealthRetreat(
        CharacterStats threat,
        float retreatDistance,
        float speedMultiplier)
    {
        if (threat == null ||
            movement == null)
        {
            return;
        }

        if (!threat.IsAlive)
            return;

        /*
         * Avbryt eventuell attack/cast som pågår när NPC:n
         * bestämmer sig för att retirera.
         *
         * Vi använder CancelCurrentAction i stället för
         * ResetRuntimeState eftersom cooldowns INTE ska
         * återställas av en taktisk retreat.
         */
        if (actionController != null &&
            actionController.HasActiveAction)
        {
            actionController
                .CancelCurrentAction();
        }

        lowHealthRetreatActive =
            true;

        lowHealthRetreatThreat =
            threat;

        /*
         * DENNA punkt blir encounterts nya leash-center.
         */
        lowHealthRetreatStartPosition =
    transform.position;

        /*
         * Retreatens startpunkt blir encounterts nya centrum.
         */
        combatAnchorPosition =
            lowHealthRetreatStartPosition;

        hasCombatAnchor =
            true;

        /*
         * Retreaten får skapa ett större temporärt encounterområde.
         *
         * Exempel:
         *
         * normal leash = 8
         * retreat = 10
         *
         * =>
         *
         * temporary combat leash = 18
         *
         * Det ger NPC:n plats att:
         * - genomföra retreaten
         * - vända för last stand
         * - repositionera under fortsatt combat
         *
         * utan att encountert resetas precis när retreaten är klar.
         */
        combatLeashRadius =
            Mathf.Max(
                maxDistanceFromSpawn,
                maxDistanceFromSpawn +
                Mathf.Max(
                    0f,
                    retreatDistance
                )
            );

        sharesRetreatLeash =
            true;

        /*
         * Efter en low-health retreat ska en riktig encounter-reset
         * alltid ta NPC:n tillbaka till dess ursprungliga spawn.
         */
        encounterReturnPosition =
            spawnPosition;

        fleeSource =
            threat;

        activeFleeSpeedMultiplier =
            Mathf.Max(
                0f,
                speedMultiplier
            );

        
         //NPC:n slåss inte medan retreat-rörelsen pågår.
   
        isAggro =
            false;

        isReturning =
            false;

        currentTargetStats =
            null;

        movement.BeginFlee(
            threat,
            Mathf.Max(
                0.1f,
                retreatDistance
            ),
            0f,
            activeFleeSpeedMultiplier
        );

        ChangeState(
            AIState.Fleeing
        );
    }

    public void ResetAggro()
    {
        fleeSource =
            null;

        activeFleeSpeedMultiplier =
            1f;

        isAggro =
            false;

        isReturning =
            false;

        currentTargetStats =
            null;

        combatLeashRadius =
            maxDistanceFromSpawn;

        sharesRetreatLeash =
            false;

        hasCombatAnchor =
            false;

        threatTracker
            ?.ResetThreat();

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;

            subscribedPlayer =
                null;
        }

        if (canWander)
        {
            EnterWanderState();
        }
        else
        {
            EnterIdleState();
        }
    }

    public void ReturnToSpawn()
    {
        if (currentState == AIState.Aggro ||
            currentState == AIState.Fleeing ||
            currentState == AIState.Holding)
        {
            BeginEncounterResetAndReturn();
            return;
        }

        if (currentState == AIState.Returning)
            return;

        wasPatrollingBeforeCombat = false;
        hasCombatAnchor = false;

        encounterReturnPosition =
            spawnPosition;

        encounterResetInProgress = true;

        movement?.Stop();

        EnterReturnState();
    }

    void OnDestroy()
    {
        if (selfStats != null)
        {
            selfStats.OnDamagedBy -=
                HandleDamaged;

            selfStats.OnDied -=
                HandleDeath;
        }

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -=
                HandleTargetDied;
        }

        UnsubscribeFromEscortPlayerDamage();
    }

    void SubscribeToPlayerDeath(PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        if (subscribedPlayer == playerStats)
            return;

        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnDied -= HandleTargetDied;
        }

        subscribedPlayer = playerStats;
        subscribedPlayer.OnDied += HandleTargetDied;
    }

    private void HandleTargetDied(
    CharacterStats deadTarget)
    {
        threatTracker
            ?.RemoveThreat(
                deadTarget
            );

        if (currentState ==
                AIState.Aggro &&
            RefreshCombatTargetFromThreat())
        {
            return;
        }

        EndCombatNaturally();
    }

    void HandleDeath(
    CharacterStats deadCharacter)
    {
        if (isDead)
            return;

        isDead =
            true;

        if (IsBeingEscorted)
        {
            FailActiveEscortOnDeath();
        }

        if (canRespawn)
        {
            NPCRespawnSystem
                .ScheduleRespawn(
                    this
                );
        }

        DisableBehaviour();
    }

    void DisableBehaviour()
    {
        enabled =
            false;

        actionController
            ?.CancelCurrentAction();

        movement
            ?.Stop();
    }

    private void DebugCombatState(
    string reason)
    {
        if (Time.time <
            nextCombatDebugTime)
        {
            return;
        }

        nextCombatDebugTime =
            Time.time +
            CombatDebugInterval;

        string targetName =
            currentTargetStats != null
                ? currentTargetStats.name
                : "NULL";

        int threatCount =
            threatTracker != null
                ? threatTracker.ThreatSourceCount
                : -1;
    }
}
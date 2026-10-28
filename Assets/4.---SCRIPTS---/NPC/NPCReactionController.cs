using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Styr hur en NPC reagerar på:
/// - närliggande hot
/// - mottagen skada
/// - alerts från andra NPC:er
/// - låg hälsa
/// - tillfällig hostility mot spelaren
///
/// NPCBehavior utför själva Aggro/Flee-statebytet.
/// Denna komponent avgör när och varför reaktionen ska ske.
/// </summary>
public sealed class NPCReactionController :
    MonoBehaviour
{
    // =========================================================
    // AWARENESS
    // =========================================================

    [Header("Awareness")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Grundradie för proximity-detection och alerts."
    )]
    private float awarenessRadius =
        5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Multiplikator för awareness-radien medan NPC:n " +
        "är alertad."
    )]
    private float alertedRadiusMultiplier =
        3f;

    [SerializeField]
    private bool drawGizmos =
        true;

    // =========================================================
    // REACTION
    // =========================================================

    [Header("Reaction")]

    [SerializeField]
    [Tooltip("Hur NPC:n reagerar på ett giltigt hot.")]
    private NPCReactionType reactionType;

    public NPCReactionType ReactionType =>
        reactionType;

    // =========================================================
    // LOW HEALTH
    // =========================================================

    [Header("Low Health Reaction")]

    [SerializeField]
    [Tooltip(
        "Om NPC:n ska övergå till Flee när dess HP når " +
        "den konfigurerade gränsen.")]
    private bool fleeAtLowHealth;

    [SerializeField]
    [Range(0.01f, 1f)]
    [Tooltip(
        "HP-procent där låg-HP-flykt aktiveras. " +
        "0.15 innebär 15 procent HP."
    )]
    private float lowHealthFleeThreshold =
        0.15f;

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip(
        "Movement speed multiplier under låg-HP-flykt. " +
        "0.5 innebär 50 procent av normal movement speed."
    )]
    private float lowHealthFleeSpeedMultiplier =
        0.5f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
    "Hur långt NPC:n försöker retirera från sitt threat " +
    "när Low Health Flee aktiveras."
)]
    private float lowHealthFleeDistance =
    10f;

    [SerializeField]
    [Tooltip(
        "Om låg-HP-flykt endast får aktiveras en gång " +
        "per encounter."
    )]
    private bool lowHealthFleeTriggersOnce =
        true;
    private bool lowHealthFleeTriggered;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
    "Om 'Triggers Once Per Encounter' är avstängd måste " +
    "denna tid gå innan NPC:n kan göra ytterligare en " +
    "low-health retreat."
)]
    private float lowHealthFleeCooldown =
    20f;

    private float lowHealthFleeCooldownTimer;

    // =========================================================
    // ALERTS
    // =========================================================

    [Header("Alerts")]

    [SerializeField]
    [Tooltip(
        "Om denna NPC ska alerta andra när den själv reagerar " +
        "direkt på skada eller ett annat explicit hot."
    )]
    private bool alertsNearbyNPCs;

    [SerializeField]
    [Tooltip(
        "Om en NPC som har mottagit en alert får skicka den " +
        "vidare. Aktivera för kedje-alert genom exempelvis en by."
    )]
    private bool propagatesReceivedAlerts;

    [SerializeField]
    [Tooltip(
        "Om endast NPC:er från samma faction får motta alerten. " +
        "När detta är avstängt kan alla närliggande NPC:er " +
        "motta alerten och använda sin egen Reaction Type."
    )]
    private bool alertSameFactionOnly =
        true;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur länge NPC:n behåller sitt alert-tillstånd."
    )]
    private float alertDuration =
        10f;

    private float alertTimer;

    private readonly HashSet<
        NPCReactionController>
        alreadyAlertedNPCs =
            new();

    // =========================================================
    // TEMPORARY HOSTILITY
    // =========================================================

    [Header("Temporary Hostility")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Hur länge tillfällig hostility mot spelaren varar."
    )]
    private float hostilityDuration =
        300f;

    [SerializeField]
    [Tooltip(
        "Om en spelarattack ska göra hela NPC:ns faction " +
        "tillfälligt fientlig.\n\n" +
        "Aktivera exempelvis för stadsvakter.\n" +
        "Stäng av för djur där endast det attackerade djuret " +
        "ska bli fientligt."
    )]
    private bool createsFactionHostilityWhenAttacked;

    private float localHostilityTimer;

    // =========================================================
    // RUNTIME
    // =========================================================

    private NPCBehavior ai;
    private CharacterStats selfStats;

    private CharacterStats lastThreatSource;

    private Transform player;
    private bool currentlyDetectingPlayer;

    public bool IsAlerted =>
        alertTimer > 0f;

    public bool IsDetectingPlayer =>
        currentlyDetectingPlayer;

    public float CurrentAwarenessRadius =>
    GetCurrentAwarenessRadius();

    public bool IsReacting =>
        ai != null &&
        (
            ai.CurrentState ==
                AIState.Aggro ||

            ai.CurrentState ==
                AIState.Fleeing ||

            ai.CurrentState ==
                AIState.Holding
        );

    public Faction Faction =>
        selfStats != null
            ? selfStats.faction
            : null;

    public bool IsHostile =>
        IsReputationHostile() ||
        IsTemporarilyHostile;

    public bool IsTemporarilyHostile
    {
        get
        {
            if (localHostilityTimer > 0f)
                return true;

            if (!createsFactionHostilityWhenAttacked)
                return false;

            PlayerStats playerStats =
                PlayerReference.Player;

            if (playerStats == null)
                return false;

            return
                FactionHostilitySystem.Instance != null &&
                FactionHostilitySystem.Instance
                    .IsHostileToPlayer(
                        Faction,
                        playerStats
                    );
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        selfStats =
            GetComponent<CharacterStats>();

        ai =
            GetComponent<NPCBehavior>();

        player =
            PlayerReference.Player
                ?.transform;

        if (selfStats != null)
        {
            selfStats.OnDamagedBy +=
                HandleDamaged;
        }
    }

    private void Update()
    {
        ResolvePlayerReference();
        UpdateLowHealthFleeCooldown();
        UpdateLocalHostility();
        UpdatePlayerDetection();
        UpdateAlertTimer();
    }

    private void OnDestroy()
    {
        if (selfStats != null)
        {
            selfStats.OnDamagedBy -=
                HandleDamaged;
        }
    }

    private void OnValidate()
    {
        awarenessRadius =
            Mathf.Max(
                0f,
                awarenessRadius
            );

        alertedRadiusMultiplier =
            Mathf.Max(
                0f,
                alertedRadiusMultiplier
            );

        alertDuration =
            Mathf.Max(
                0f,
                alertDuration
            );

        hostilityDuration =
            Mathf.Max(
                0f,
                hostilityDuration
            );

        lowHealthFleeThreshold =
            Mathf.Clamp01(
                lowHealthFleeThreshold
            );

        lowHealthFleeSpeedMultiplier =
            Mathf.Max(
                0f,
                lowHealthFleeSpeedMultiplier
            );

        lowHealthFleeDistance =
            Mathf.Max(
                0.1f,
                lowHealthFleeDistance
            );

        lowHealthFleeCooldown =
            Mathf.Max(
                0f,
                lowHealthFleeCooldown
            );
    }

    // =========================================================
    // DAMAGE REACTION
    // =========================================================

    private void HandleDamaged(
    CharacterStats attacker)
    {
        /*
         * =========================================================
         * ESCORT OVERRIDE
         * =========================================================
         *
         * En aktiv escort-NPC använder INTE sin normala ReactionType.
         *
         * Även en NPC som normalt skulle Flee ska under escorten
         * försvara sig istället.
         */
        if (ai != null &&
            ai.IsBeingEscorted)
        {
            if (attacker == null ||
                selfStats == null ||
                attacker == selfStats ||
                !selfStats.IsAlive ||
                !attacker.IsAlive)
            {
                return;
            }

            /*
             * Friendly fire startar inte escort-combat.
             */
            if (selfStats.faction != null &&
                attacker.faction != null &&
                selfStats.faction ==
                attacker.faction)
            {
                return;
            }

            lastThreatSource =
                attacker;

            /*
             * Om spelaren själv attackerar escort-NPC:n behåller vi
             * befintlig temporary-hostility-semantik.
             *
             * Senare kan själva Escort-favourn dessutom faila på detta.
             */
            if (attacker is PlayerStats
                playerAttacker)
            {
                ApplyTemporaryHostility(
                    playerAttacker
                );
            }

            ai.TryStartEscortCombat(
                attacker
            );

            /*
             * Ingen:
             * - low-health flee
             * - normal ReactionType
             * - alert propagation
             *
             * under escort.
             */
            return;
        }

        /*
         * =========================================================
         * NORMAL NPC REACTION
         * =========================================================
         */

        if (!IsValidDamageThreat(
                attacker))
        {
            return;
        }

        lastThreatSource =
            attacker;

        RefreshAlert();

        PlayerStats playerAttackerNormal =
            attacker as PlayerStats;

        if (playerAttackerNormal != null)
        {
            ApplyTemporaryHostility(
                playerAttackerNormal
            );
        }

        if (TryTriggerLowHealthFlee(
                attacker))
        {
            if (alertsNearbyNPCs)
            {
                PropagateAlert(
                    attacker
                );
            }

            return;
        }

        ExecuteReaction(
            attacker
        );

        if (alertsNearbyNPCs)
        {
            PropagateAlert(
                attacker
            );
        }
    }

    private bool IsValidDamageThreat(
        CharacterStats attacker)
    {
        if (!CanConsiderThreat(
                attacker))
        {
            return false;
        }

        /*
         * Samma faction ignoreras som standard.
         *
         * Det förhindrar intern Aggro/Flee från exempelvis
         * oavsiktlig friendly-fire-AoE.
         */
        if (selfStats.faction != null &&
            attacker.faction != null &&
            selfStats.faction ==
            attacker.faction)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // PROXIMITY REACTION
    // =========================================================

    public bool TryReactToProximityThreat(
        CharacterStats threat)
    {
        if (!CanReactToProximityThreat(
                threat))
        {
            return false;
        }

        lastThreatSource =
            threat;

        RefreshAlert();

        /*
         * Vanlig proximity-detection drar inte automatiskt med
         * närliggande NPC:er.
         *
         * Alerts skickas från explicita händelser såsom damage,
         * crime eller en mottagen relay.
         */
        ExecuteReaction(
            threat
        );

        return true;
    }

    public bool CanReactToProximityThreat(
        CharacterStats threat)
    {
        if (!CanConsiderThreat(
                threat))
        {
            return false;
        }

        switch (reactionType)
        {
            case NPCReactionType.Aggro:

                /*
                 * En Aggro-NPC reagerar om någon av parterna
                 * betraktar den andra som fientlig.
                 *
                 * Detta gör att exempelvis en vakt reagerar på en
                 * varg som är fientlig mot vakten, även om endast
                 * ena factionrelationen råkar vara konfigurerad.
                 */
                return IsMutualCombatThreat(
                    threat
                );

            case NPCReactionType.Flee:

                /*
                 * En Flee-NPC betraktar någon som hot om hotet kan
                 * eller rimligen skulle kunna attackera NPC:n.
                 */
                return CanThreatAttackSelf(
                    threat
                );

            case NPCReactionType.None:
            default:
                return false;
        }
    }

    private bool IsMutualCombatThreat(
    CharacterStats threat)
    {
        if (selfStats == null ||
            threat == null)
        {
            return false;
        }

        /*
         * PLAYER
         *
         * Murder Mode betyder endast att SPELAREN får attackera
         * factionen.
         *
         * Det betyder INTE att factionen automatiskt betraktar
         * spelaren som fientlig.
         *
         * För NPC -> Player använder vi därför faktisk hostility:
         *
         * - Hated reputation
         * - local temporary hostility
         * - faction temporary hostility
         */
        if (threat is PlayerStats playerStats)
        {
            return selfStats
                .IsHostileToPlayer(
                    playerStats
                );
        }

        /*
         * NPC -> NPC
         *
         * En relation räknas som combat-hostile om någon av
         * factionerna betraktar den andra som fientlig.
         *
         * Detta stödjer även asymmetriska factionrelationer.
         */
        return
            selfStats.IsHostileTo(
                threat
            ) ||
            threat.IsHostileTo(
                selfStats
            );
    }

    private bool CanThreatAttackSelf(
     CharacterStats threat)
    {
        if (selfStats == null ||
            threat == null)
        {
            return false;
        }

        /*
         * Murder Mode ska INTE göra spelaren till ett hot.
         *
         * En Flee-NPC flyr från spelaren först när NPC:ns
         * faction faktiskt betraktar spelaren som hostile.
         *
         * Exempel:
         * - Hated reputation
         * - NPC/faction har blivit attackerad
         * - temporary faction hostility
         */
        if (threat is PlayerStats playerStats)
        {
            return selfStats
                .IsHostileToPlayer(
                    playerStats
                );
        }

        /*
         * NPC-hot.
         *
         * Om någon av factionerna betraktar relationen som hostile
         * behandlar en Flee-NPC motparten som ett hot.
         */
        return
            threat.IsHostileTo(
                selfStats
            ) ||
            selfStats.IsHostileTo(
                threat
            );
    }

    // =========================================================
    // COMMON REACTION
    // =========================================================

    private void ExecuteReaction(
        CharacterStats threat)
    {
        if (threat == null ||
            ai == null)
        {
            return;
        }

        switch (reactionType)
        {
            case NPCReactionType.Flee:

                ai.StartFleeing(
                    threat
                );

                break;

            case NPCReactionType.Aggro:

                ai.ForceAggro(
                    threat
                );

                break;

            case NPCReactionType.None:
            default:
                break;
        }
    }

    private bool CanConsiderThreat(
    CharacterStats threat)
    {
        if (ai != null &&
            ai.IsBeingEscorted)
        {
            return false;
        }

        if (reactionType ==
            NPCReactionType.None)
        {
            return false;
        }

        if (selfStats == null ||
            threat == null)
        {
            return false;
        }

        if (threat == selfStats)
            return false;

        if (!selfStats.IsAlive ||
            !threat.IsAlive)
        {
            return false;
        }

        if (ai == null ||
            ai.IsEncounterResetting)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // LOW HEALTH
    // =========================================================

    private bool TryTriggerLowHealthFlee(
    CharacterStats threat)
    {
        if (!fleeAtLowHealth ||
            selfStats == null ||
            ai == null ||
            threat == null)
        {
            return false;
        }

        if (!selfStats.IsAlive ||
            !threat.IsAlive)
        {
            return false;
        }

        /*
         * Variant A:
         *
         * Får bara ske en gång under hela encountert.
         */
        if (lowHealthFleeTriggersOnce)
        {
            if (lowHealthFleeTriggered)
            {
                return false;
            }
        }
        /*
         * Variant B:
         *
         * Får ske flera gånger, men endast efter cooldown.
         */
        else if (
            lowHealthFleeCooldownTimer > 0f)
        {
            return false;
        }

        int maximumHealth =
            Mathf.Max(
                1,
                selfStats.GetMaxHP()
            );

        float normalizedHealth =
            Mathf.Clamp01(
                selfStats.currentHP /
                (float)maximumHealth
            );

        if (normalizedHealth >
            lowHealthFleeThreshold)
        {
            return false;
        }

        lowHealthFleeTriggered =
            true;

        lowHealthFleeCooldownTimer =
            Mathf.Max(
                0f,
                lowHealthFleeCooldown
            );

        lastThreatSource =
            threat;

        RefreshAlert();

        /*
         * VIKTIGT:
         *
         * Low-health flee använder INTE längre vanlig StartFleeing.
         *
         * Den använder en särskild retreat-väg som behåller encountert
         * och flyttar combat/leash-anchor till retreatens startpunkt.
         */
        ai.StartLowHealthRetreat(
            threat,
            lowHealthFleeDistance,
            lowHealthFleeSpeedMultiplier
        );

        return true;
    }
    private void UpdateLowHealthFleeCooldown()
    {
        if (lowHealthFleeCooldownTimer <=
            0f)
        {
            return;
        }

        lowHealthFleeCooldownTimer =
            Mathf.Max(
                0f,
                lowHealthFleeCooldownTimer -
                Time.deltaTime
            );
    }


    // =========================================================
    // ALERT SENDING
    // =========================================================

    private void PropagateAlert(
        CharacterStats threat)
    {
        if (threat == null ||
            selfStats == null)
        {
            return;
        }

        float radius =
            GetCurrentAwarenessRadius();

        if (radius <= 0f)
            return;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                radius
            );

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            Collider2D hit =
                hits[i];

            if (hit == null)
                continue;

            NPCReactionController other =
                hit.GetComponentInParent<
                    NPCReactionController>();

            if (other == null ||
                other == this)
            {
                continue;
            }

            if (alertSameFactionOnly &&
                other.Faction != Faction)
            {
                continue;
            }

            if (!alreadyAlertedNPCs.Add(
                    other))
            {
                continue;
            }

            other.ReceiveAlert(
                threat
            );
        }
    }

    // =========================================================
    // ALERT RECEIVING
    // =========================================================

    private void ReceiveAlert(
        CharacterStats threat)
    {
        if (!CanConsiderThreat(
                threat))
        {
            return;
        }

        bool wasAlreadyAlerted =
            IsAlerted;

        lastThreatSource =
            threat;

        RefreshAlert();

        ExecuteReaction(
            threat
        );

        /*
         * Relaya endast när:
         * - denna NPC inte redan var alertad
         * - den uttryckligen får relaya mottagna alerts
         *
         * HashSet-minnet på varje NPC stoppar cirkulära kedjor.
         */
        if (!wasAlreadyAlerted &&
            propagatesReceivedAlerts)
        {
            PropagateAlert(
                threat
            );
        }
    }

    /// <summary>
    /// Publik alert-ingång för andra system.
    ///
    /// Exempel:
    /// - faction awareness
    /// - scripted encounters
    /// - guards som bevittnar brott
    /// </summary>
    public void ForceAlert(
        CharacterStats attacker)
    {
        ReceiveAlert(
            attacker
        );
    }

    // =========================================================
    // CRIME
    // =========================================================

    public void OnWitnessedCrime(
        CharacterStats attacker)
    {
        if (!CanConsiderThreat(
                attacker))
        {
            return;
        }

        if (attacker is PlayerStats playerAttacker)
        {
            /*
             * Ett bevittnat brott använder faction-hostility,
             * eftersom det normalt representerar en social
             * factionreaktion snarare än ett individuellt djur.
             */
            ApplyFactionHostility(
                playerAttacker
            );
        }

        lastThreatSource =
            attacker;

        RefreshAlert();

        ExecuteReaction(
            attacker
        );

        if (alertsNearbyNPCs)
        {
            PropagateAlert(
                attacker
            );
        }
    }

    // =========================================================
    // TEMPORARY HOSTILITY
    // =========================================================

    private void ApplyTemporaryHostility(
        PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        if (createsFactionHostilityWhenAttacked)
        {
            ApplyFactionHostility(
                playerStats
            );

            return;
        }

        /*
         * Endast den attackerade NPC:n blir tillfälligt fientlig.
         */
        localHostilityTimer =
            Mathf.Max(
                localHostilityTimer,
                hostilityDuration
            );
    }

    private void ApplyFactionHostility(
        PlayerStats playerStats)
    {
        if (playerStats == null)
            return;

        FactionHostilitySystem.Instance
            ?.AddHostility(
                Faction,
                playerStats,
                hostilityDuration
            );
    }

    private void UpdateLocalHostility()
    {
        if (localHostilityTimer <= 0f)
            return;

        localHostilityTimer =
            Mathf.Max(
                0f,
                localHostilityTimer -
                Time.deltaTime
            );
    }

    private bool IsReputationHostile()
    {
        if (selfStats == null)
            return false;

        PlayerStats playerStats =
            PlayerReference.Player;

        if (playerStats == null)
            return false;

        return selfStats.IsHostileTo(
            playerStats
        );
    }

    // =========================================================
    // ALERT TIMER
    // =========================================================

    private void RefreshAlert()
    {
        alertTimer =
            Mathf.Max(
                alertTimer,
                alertDuration
            );

        FactionAwarenessSystem.Instance
            ?.RegisterAlertedNPC(
                this
            );
    }

    private void UpdateAlertTimer()
    {
        bool shouldRefresh =
            currentlyDetectingPlayer;

        if (ai != null)
        {
            shouldRefresh |=
                ai.CurrentState ==
                AIState.Aggro;

            shouldRefresh |=
                ai.CurrentState ==
                AIState.Fleeing;
        }

        if (shouldRefresh)
        {
            RefreshAlert();
        }

        if (alertTimer <= 0f)
            return;

        alertTimer =
            Mathf.Max(
                0f,
                alertTimer -
                Time.deltaTime
            );

        if (alertTimer > 0f)
            return;

        ClearAwarenessMemory();

        FactionAwarenessSystem.Instance
            ?.UnregisterAlertedNPC(
                this
            );
    }

    private void UpdatePlayerDetection()
    {
        currentlyDetectingPlayer =
            false;

        if (!IsAlerted)
            return;

        ResolvePlayerReference();

        if (player == null)
            return;

        float distance =
            Vector2.Distance(
                transform.position,
                player.position
            );

        currentlyDetectingPlayer =
            distance <=
            GetCurrentAwarenessRadius();
    }

    private float GetCurrentAwarenessRadius()
    {
        float multiplier =
            IsAlerted
                ? alertedRadiusMultiplier
                : 1f;

        return
            awarenessRadius *
            Mathf.Max(
                0f,
                multiplier
            );
    }

    private void ResolvePlayerReference()
    {
        if (player != null)
            return;

        player =
            PlayerReference.Player
                ?.transform;
    }

    public void ClearAwarenessMemory()
    {
        alreadyAlertedNPCs.Clear();
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetEncounterState()
    {
        lastThreatSource =
            null;

        currentlyDetectingPlayer =
            false;

        lowHealthFleeTriggered =
            false;

        lowHealthFleeCooldownTimer =
            0f;

        alertTimer =
            0f;

        localHostilityTimer =
            0f;

        ClearAwarenessMemory();

        FactionAwarenessSystem.Instance
            ?.UnregisterAlertedNPC(
                this
            );
    }

    // =========================================================
    // EXTERNAL QUERIES
    // =========================================================

    public bool DoesCurrentlyDetectPlayer()
    {
        return currentlyDetectingPlayer;
    }

    public bool BlocksInteraction(
        PlayerStats playerStats)
    {
        if (playerStats == null ||
            selfStats == null)
        {
            return false;
        }

        if (selfStats.IsHostileTo(
                playerStats))
        {
            return true;
        }

        return IsTemporarilyHostile;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        bool hostile =
            Application.isPlaying &&
            IsHostile;

        float radius =
            Application.isPlaying
                ? GetCurrentAwarenessRadius()
                : awarenessRadius;

        Gizmos.color =
            hostile
                ? Color.yellow
                : Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            radius
        );
    }
}
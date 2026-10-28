using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auktoritativ threat- och engagement-lista för en NPC.
///
/// Trackern svarar på två frågor:
///
/// 1. Vilka levande combat-deltagare är denna NPC engagerad med?
/// 2. Vem av dem bör NPC:n fokusera just nu?
///
/// Grundregler:
///
/// - engagement skapar en liten threat-bas
/// - faktisk damage ökar threat
/// - högst threat blir normalt target
/// - nuvarande target behålls tills en konkurrent passerar
///   targetSwitchThreatMultiplier
/// - döda targets tas bort automatiskt
/// - encounter-reset tar explicit bort engagement åt båda håll
///
/// DamageContributionTracker är fortfarande separat och avgör
/// reward-credit.
///
/// Threat = combat target selection.
/// Contribution = rewards.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats))]
public sealed class NPCThreatTracker :
    MonoBehaviour
{
    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Threat")]

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "Threat per faktisk damage point."
    )]
    private float damageThreatMultiplier =
        1f;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip(
        "Minsta threat som ges när ett combat-engagement " +
        "skapas innan någon faktisk damage har gjorts."
    )]
    private float initialAggroThreat =
        1f;

    [SerializeField]
    [Min(1f)]
    [Tooltip(
        "Hur mycket mer threat en annan target behöver ha " +
        "för att ta över aggro från nuvarande target. " +
        "1.20 betyder 20 procent mer threat."
    )]
    private float targetSwitchThreatMultiplier =
        1.20f;

    // =========================================================
    // RUNTIME
    // =========================================================

    private readonly Dictionary<
        CharacterStats,
        float>
        threatBySource =
            new();

    private CharacterStats selfStats;

    // =========================================================
    // PUBLIC
    // =========================================================

    public int ThreatSourceCount
    {
        get
        {
            CleanupInvalidTargets();

            return threatBySource.Count;
        }
    }

    public bool HasThreats
    {
        get
        {
            CleanupInvalidTargets();

            return threatBySource.Count > 0;
        }
    }

    public float TotalThreat
    {
        get
        {
            CleanupInvalidTargets();

            float total =
                0f;

            foreach (
                KeyValuePair<CharacterStats, float>
                    pair
                in threatBySource)
            {
                total +=
                    Mathf.Max(
                        0f,
                        pair.Value
                    );
            }

            return total;
        }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        selfStats =
            GetComponent<CharacterStats>();

        if (selfStats != null)
        {
            selfStats.OnDamageApplied +=
                HandleDamageApplied;
        }
    }

    private void OnDestroy()
    {
        if (selfStats != null)
        {
            selfStats.OnDamageApplied -=
                HandleDamageApplied;
        }

        /*
         * Objektet försvinner permanent.
         *
         * Låt andra NPC:er släppa just denna deltagare från
         * sina engagement-listor.
         */
        ResetThreat(
            notifyOtherParticipants: true
        );
    }

    private void OnValidate()
    {
        damageThreatMultiplier =
            Mathf.Max(
                0f,
                damageThreatMultiplier
            );

        initialAggroThreat =
            Mathf.Max(
                0.01f,
                initialAggroThreat
            );

        targetSwitchThreatMultiplier =
            Mathf.Max(
                1f,
                targetSwitchThreatMultiplier
            );
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    private void HandleDamageApplied(
        DamageAppliedEvent damageEvent)
    {
        if (damageEvent.AppliedDamage <= 0)
            return;

        CharacterStats threatSource =
            damageEvent.Source.DirectSource;

        /*
         * Threat riktas normalt mot den faktiska entity som
         * utförde attacken.
         *
         * Exempel:
         *
         * Player
         *   └── Summon
         *        └── attackerar Wolf
         *
         * Wolf kan då fokusera Summon,
         * medan DamageContributionTracker fortfarande ger
         * reward-credit till Player.
         */
        if (threatSource == null)
        {
            threatSource =
                damageEvent.Source.CreditOwner;
        }

        if (threatSource == null)
            return;

        /*
         * Damage innebär alltid ett riktigt combat-engagement.
         */
        EnsureEngagement(
            threatSource
        );

        float threat =
            damageEvent.AppliedDamage *
            damageThreatMultiplier;

        AddThreatInternal(
            threatSource,
            threat
        );
    }

    // =========================================================
    // ENGAGEMENT
    // =========================================================

    /// <summary>
    /// Registrerar source som deltagare i denna NPC:s encounter.
    ///
    /// Om source också är en NPC registreras denna NPC som
    /// deltagare hos source.
    ///
    /// Detta skapar ett enkelt tvåvägs combat-engagement utan
    /// behov av ett separat globalt CombatEncounter-objekt.
    /// </summary>
    public void EnsureEngagement(
        CharacterStats source)
    {
        EnsureEngagementInternal(
            source,
            initialAggroThreat,
            notifyOtherParticipant: true
        );
    }

    public void EnsureEngagement(
        CharacterStats source,
        float minimumThreat)
    {
        EnsureEngagementInternal(
            source,
            minimumThreat,
            notifyOtherParticipant: true
        );
    }

    private void EnsureEngagementInternal(
        CharacterStats source,
        float minimumThreat,
        bool notifyOtherParticipant)
    {
        if (!CanStoreThreat(
                source))
        {
            return;
        }

        EnsureThreatInternal(
            source,
            minimumThreat
        );

        if (!notifyOtherParticipant)
            return;

        NPCThreatTracker otherTracker =
            source.GetComponent<
                NPCThreatTracker>();

        if (otherTracker == null ||
            otherTracker == this)
        {
            return;
        }

        /*
         * Tvåvägs engagement.
         *
         * Viktigt:
         * recursive notify stängs av här så vi inte ping-pongar.
         */
        otherTracker
            .EnsureEngagementInternal(
                selfStats,
                otherTracker.initialAggroThreat,
                notifyOtherParticipant: false
            );
    }

    // =========================================================
    // THREAT API
    // =========================================================

    public void AddThreat(
        CharacterStats source,
        float amount,
        ThreatReason reason =
            ThreatReason.Scripted)
    {
        if (!CanStoreThreat(
                source))
        {
            return;
        }

        /*
         * All riktig threat betyder också att source är en
         * deltagare i encountert.
         */
        EnsureEngagement(
            source
        );

        AddThreatInternal(
            source,
            amount
        );
    }

    private void AddThreatInternal(
        CharacterStats source,
        float amount)
    {
        if (!CanStoreThreat(
                source))
        {
            return;
        }

        float safeAmount =
            Mathf.Max(
                0f,
                amount
            );

        if (safeAmount <= 0f)
            return;

        if (threatBySource.TryGetValue(
                source,
                out float currentThreat))
        {
            threatBySource[source] =
                currentThreat +
                safeAmount;

            return;
        }

        threatBySource.Add(
            source,
            safeAmount
        );
    }

    /// <summary>
    /// Legacy/convenience-API.
    ///
    /// Behålls eftersom NPCBehavior redan använder EnsureThreat.
    /// Den registrerar nu även ett riktigt engagement.
    /// </summary>
    public void EnsureThreat(
        CharacterStats source)
    {
        EnsureEngagement(
            source,
            initialAggroThreat
        );
    }

    public void EnsureThreat(
        CharacterStats source,
        float minimumThreat)
    {
        EnsureEngagement(
            source,
            minimumThreat
        );
    }

    private void EnsureThreatInternal(
        CharacterStats source,
        float minimumThreat)
    {
        if (!CanStoreThreat(
                source))
        {
            return;
        }

        float requiredThreat =
            Mathf.Max(
                0.01f,
                minimumThreat
            );

        if (threatBySource.TryGetValue(
                source,
                out float existingThreat))
        {
            if (existingThreat <
                requiredThreat)
            {
                threatBySource[source] =
                    requiredThreat;
            }

            return;
        }

        threatBySource.Add(
            source,
            requiredThreat
        );
    }

    public float GetThreat(
        CharacterStats source)
    {
        if (source == null)
            return 0f;

        CleanupInvalidTargets();

        return threatBySource.TryGetValue(
            source,
            out float threat)
                ? Mathf.Max(
                    0f,
                    threat
                )
                : 0f;
    }

    public float GetThreatShare(
        CharacterStats source)
    {
        float total =
            TotalThreat;

        if (source == null ||
            total <= 0f)
        {
            return 0f;
        }

        return
            GetThreat(
                source
            ) /
            total;
    }

    // =========================================================
    // DISENGAGEMENT
    // =========================================================

    /// <summary>
    /// Tar bort EN deltagare från encountert.
    ///
    /// Detta betyder inte att hela encountert avslutas.
    /// </summary>
    public void RemoveThreat(
        CharacterStats source)
    {
        RemoveThreatInternal(
            source,
            notifyOtherParticipant: true
        );
    }

    private void RemoveThreatInternal(
        CharacterStats source,
        bool notifyOtherParticipant)
    {
        if (source == null)
            return;

        bool removed =
            threatBySource.Remove(
                source
            );

        if (!removed ||
            !notifyOtherParticipant)
        {
            return;
        }

        NPCThreatTracker otherTracker =
            source.GetComponent<
                NPCThreatTracker>();

        if (otherTracker == null ||
            otherTracker == this)
        {
            return;
        }

        /*
         * Om A lämnar engagement med B ska B också släppa A.
         *
         * Men endast DEN relationen tas bort.
         *
         * B:s övriga threats påverkas inte.
         */
        otherTracker
            .RemoveThreatInternal(
                selfStats,
                notifyOtherParticipant: false
            );
    }

    /// <summary>
    /// Avslutar hela denna NPC:s encounter.
    ///
    /// Varje deltagare informeras om att just denna NPC har
    /// lämnat deras encounter.
    ///
    /// Deras övriga participants påverkas inte.
    /// </summary>
    public void ResetThreat()
    {
        ResetThreat(
            notifyOtherParticipants: true
        );
    }

    private void ResetThreat(
        bool notifyOtherParticipants)
    {
        if (threatBySource.Count == 0)
            return;

        if (!notifyOtherParticipants)
        {
            threatBySource.Clear();

            return;
        }

        List<CharacterStats> participants =
            new(
                threatBySource.Keys
            );

        threatBySource.Clear();

        for (int i = 0;
             i < participants.Count;
             i++)
        {
            CharacterStats participant =
                participants[i];

            if (participant == null)
                continue;

            NPCThreatTracker otherTracker =
                participant.GetComponent<
                    NPCThreatTracker>();

            if (otherTracker == null ||
                otherTracker == this)
            {
                continue;
            }

            otherTracker
                .RemoveThreatInternal(
                    selfStats,
                    notifyOtherParticipant: false
                );
        }
    }

    // =========================================================
    // TARGET SELECTION
    // =========================================================

    /// <summary>
    /// Returnerar det target NPC:n bör fokusera just nu.
    ///
    /// Nuvarande target får hysteresis:
    /// en konkurrent måste överstiga current threat med
    /// targetSwitchThreatMultiplier innan target byts.
    /// </summary>
    public CharacterStats GetPreferredTarget(
        CharacterStats currentTarget)
    {
        CleanupInvalidTargets();

        CharacterStats highestTarget =
            GetHighestThreatTargetInternal(
                out float highestThreat
            );

        if (highestTarget == null)
            return null;

        if (!IsValidThreatTarget(
                currentTarget))
        {
            return highestTarget;
        }

        if (!threatBySource.TryGetValue(
                currentTarget,
                out float currentThreat))
        {
            return highestTarget;
        }

        /*
         * Current är redan högst.
         */
        if (highestTarget ==
            currentTarget)
        {
            return currentTarget;
        }

        float requiredThreat =
            currentThreat *
            targetSwitchThreatMultiplier;

        if (highestThreat + 0.0001f >=
            requiredThreat)
        {
            return highestTarget;
        }

        return currentTarget;
    }

    public CharacterStats
        GetHighestThreatTarget()
    {
        CleanupInvalidTargets();

        return
            GetHighestThreatTargetInternal(
                out _
            );
    }

    public CharacterStats
        GetHighestThreatTargetWithinRange(
            Vector2 origin,
            float maximumDistance,
            CharacterStats ignoreTarget = null)
    {
        CleanupInvalidTargets();

        float safeMaximumDistance =
            Mathf.Max(
                0f,
                maximumDistance
            );

        float maximumDistanceSqr =
            safeMaximumDistance *
            safeMaximumDistance;

        CharacterStats highestTarget =
            null;

        float highestThreat =
            float.NegativeInfinity;

        foreach (
            KeyValuePair<CharacterStats, float>
                pair
            in threatBySource)
        {
            CharacterStats candidate =
                pair.Key;

            if (!IsValidThreatTarget(
                    candidate))
            {
                continue;
            }

            /*
             * Leash-systemet använder detta för targetet som precis
             * drog NPC:n utanför encounterområdet.
             */
            if (candidate ==
                ignoreTarget)
            {
                continue;
            }

            Vector2 candidatePosition =
                candidate.transform.position;

            float distanceSqr =
                (
                    candidatePosition -
                    origin
                ).sqrMagnitude;

            if (distanceSqr >
                maximumDistanceSqr)
            {
                continue;
            }

            float candidateThreat =
                Mathf.Max(
                    0f,
                    pair.Value
                );

            if (highestTarget != null &&
                candidateThreat <=
                highestThreat)
            {
                continue;
            }

            highestTarget =
                candidate;

            highestThreat =
                candidateThreat;
        }

        return highestTarget;
    }

    private CharacterStats
        GetHighestThreatTargetInternal(
            out float highestThreat)
    {
        CharacterStats highestTarget =
            null;

        highestThreat =
            0f;

        foreach (
            KeyValuePair<CharacterStats, float>
                pair
            in threatBySource)
        {
            CharacterStats candidate =
                pair.Key;

            if (!IsValidThreatTarget(
                    candidate))
            {
                continue;
            }

            float threat =
                Mathf.Max(
                    0f,
                    pair.Value
                );

            if (highestTarget != null &&
                threat <=
                highestThreat)
            {
                continue;
            }

            highestTarget =
                candidate;

            highestThreat =
                threat;
        }

        return highestTarget;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool CanStoreThreat(
        CharacterStats source)
    {
        if (source == null ||
            source == selfStats)
        {
            return false;
        }

        return source.IsAlive;
    }

    private bool IsValidThreatTarget(
        CharacterStats target)
    {
        /*
         * Håll valideringen AVSIKTLIGT enkel.
         *
         * Vi försöker inte längre läsa targetets AI-state här.
         *
         * Ett annat systems Returning/Fleeing/Holding ska inte
         * magiskt radera denna NPC:s combat state.
         *
         * Disengagement sker explicit genom RemoveThreat eller
         * ResetThreat.
         */
        return
            target != null &&
            target != selfStats &&
            target.IsAlive;
    }

    private void CleanupInvalidTargets()
    {
        if (threatBySource.Count == 0)
            return;

        List<CharacterStats> invalidTargets =
            null;

        foreach (
            KeyValuePair<CharacterStats, float>
                pair
            in threatBySource)
        {
            if (IsValidThreatTarget(
                    pair.Key))
            {
                continue;
            }

            invalidTargets ??=
                new List<CharacterStats>();

            invalidTargets.Add(
                pair.Key
            );
        }

        if (invalidTargets == null)
            return;

        /*
         * Döda/destroyade targets behöver ingen bilateral callback.
         *
         * Deras tracker håller ändå på att försvinna/resetas.
         */
        for (int i = 0;
             i < invalidTargets.Count;
             i++)
        {
            threatBySource.Remove(
                invalidTargets[i]
            );
        }
    }
}
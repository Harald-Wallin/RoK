using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Markerar ett område som indoor.
///
/// En IndoorZone får bestå av flera överlappande
/// Collider2D-komponenter.
///
/// Spelaren betraktas som indoor så länge minst en av
/// zonens colliders fortfarande överlappar spelaren.
///
/// Ansvar:
/// - registrera spelarens indoor-state
/// - fade:a associerade tak/occluders
///
/// Ansvarar INTE för:
/// - musik
/// - ambience
/// - abilities
/// - rendering implementation
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class IndoorZone :
    MonoBehaviour
{
    // =========================================================
    // OCCLUSION
    // =========================================================

    [Header("Occlusion")]

    [SerializeField]
    [Tooltip(
        "Objekt som ska fade:a när spelaren går in i zonen. " +
        "Vanligtvis byggnadens Roof-root."
    )]
    private OcclusionFader[] occlusionFaders;

    // =========================================================
    // RUNTIME
    // =========================================================

    private Collider2D[] zoneColliders;

    private readonly HashSet<Collider2D>
        playerColliders =
            new();

    private PlayerEnvironmentState
        activePlayerEnvironment;

    private bool playerInside;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        CacheZoneColliders();
        ValidateZoneColliders();
    }

    private void OnEnable()
    {
        CacheZoneColliders();
    }

    private void OnDisable()
    {
        ForceLeaveZone();
    }

    // =========================================================
    // TRIGGERS
    // =========================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!TryGetPlayerEnvironment(
                other,
                out PlayerEnvironmentState
                    environment))
        {
            return;
        }

        playerColliders.Add(
            other
        );

        /*
         * Vi är redan inne.
         *
         * Exempel:
         * spelaren går från entré-triggern in i
         * huvudbyggnadens trigger.
         */
        if (playerInside)
            return;

        EnterZone(
            environment
        );
    }

    private void OnTriggerStay2D(
        Collider2D other)
    {
        /*
         * Stay fungerar här som extra säkerhetsnät.
         *
         * Om Unity skickar Enter/Exit i oväntad ordning
         * mellan flera överlappande triggers kan zonen
         * själv återställa korrekt state.
         */
        if (!TryGetPlayerEnvironment(
                other,
                out PlayerEnvironmentState
                    environment))
        {
            return;
        }

        playerColliders.Add(
            other
        );

        if (playerInside)
            return;

        EnterZone(
            environment
        );
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        if (!IsPlayerCollider(
                other))
        {
            return;
        }

        playerColliders.Remove(
            other
        );

        /*
         * VIKTIGT:
         *
         * Ett Exit-event från EN av zonens colliders betyder
         * inte automatiskt att spelaren lämnat HELA zonen.
         *
         * Kontrollera därför den faktiska geometrin.
         */
        if (IsPlayerStillInsideZone(
                other))
        {
            return;
        }

        ForceLeaveZone();
    }

    // =========================================================
    // ENTER / EXIT
    // =========================================================

    private void EnterZone(
        PlayerEnvironmentState environment)
    {
        if (environment == null)
            return;

        playerInside =
            true;

        activePlayerEnvironment =
            environment;

        activePlayerEnvironment
            .EnterIndoor(
                this
            );

        SetOcclusion(
            true
        );
    }

    private void ForceLeaveZone()
    {
        if (!playerInside &&
            activePlayerEnvironment == null)
        {
            playerColliders.Clear();

            return;
        }

        if (activePlayerEnvironment !=
            null)
        {
            activePlayerEnvironment
                .ExitIndoor(
                    this
                );
        }

        activePlayerEnvironment =
            null;

        playerInside =
            false;

        playerColliders.Clear();

        SetOcclusion(
            false
        );
    }

    // =========================================================
    // ZONE CHECK
    // =========================================================

    /// <summary>
    /// Kontrollerar om någon del av spelarens collider fortfarande
    /// överlappar någon av denna IndoorZones triggercolliders.
    ///
    /// Detta gör att flera överlappande BoxCollider2D kan bilda
    /// en enda logisk indoor-yta.
    /// </summary>
    private bool IsPlayerStillInsideZone(
        Collider2D exitingPlayerCollider)
    {
        if (zoneColliders == null ||
            zoneColliders.Length == 0)
        {
            return false;
        }

        /*
         * Först använder vi de spelarcolliders vi redan känner till.
         */
        foreach (
            Collider2D playerCollider
            in playerColliders)
        {
            if (playerCollider == null ||
                !playerCollider.enabled)
            {
                continue;
            }

            if (OverlapsAnyZoneCollider(
                    playerCollider))
            {
                return true;
            }
        }

        /*
         * Den collider som precis gav Exit kan samtidigt fortfarande
         * ligga i en ANNAN trigger på samma IndoorZone.
         *
         * Därför testar vi även den explicit.
         */
        if (exitingPlayerCollider != null &&
            exitingPlayerCollider.enabled &&
            OverlapsAnyZoneCollider(
                exitingPlayerCollider))
        {
            playerColliders.Add(
                exitingPlayerCollider
            );

            return true;
        }

        return false;
    }

    private bool OverlapsAnyZoneCollider(
        Collider2D playerCollider)
    {
        if (playerCollider == null)
            return false;

        for (int i = 0;
             i < zoneColliders.Length;
             i++)
        {
            Collider2D zoneCollider =
                zoneColliders[i];

            if (zoneCollider == null ||
                !zoneCollider.enabled)
            {
                continue;
            }

            ColliderDistance2D distance =
                Physics2D.Distance(
                    zoneCollider,
                    playerCollider
                );

            if (distance.isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // OCCLUSION
    // =========================================================

    private void SetOcclusion(
        bool occluded)
    {
        if (occlusionFaders == null)
            return;

        for (int i = 0;
             i < occlusionFaders.Length;
             i++)
        {
            OcclusionFader fader =
                occlusionFaders[i];

            if (fader == null)
                continue;

            fader.SetOccluded(
                this,
                occluded
            );
        }
    }

    public void SetOcclusionFaders(
    params OcclusionFader[] faders)
    {
        occlusionFaders =
            faders;
    }


    public void RefreshSetup()
    {
        CacheZoneColliders();

        ValidateZoneColliders();
    }

    // =========================================================
    // PLAYER
    // =========================================================

    /// <summary>
    /// IndoorZone reagerar endast på spelarens FYSISKA collider.
    ///
    /// Hitboxes, interaction-triggers och andra trigger-colliders
    /// under spelaren får inte aktivera indoor-state.
    /// </summary>
    private static bool TryGetPlayerEnvironment(
        Collider2D collider,
        out PlayerEnvironmentState environment)
    {
        environment =
            null;

        if (collider == null)
            return false;

        /*
         * IndoorZone själv är en trigger.
         *
         * Spelarens riktiga body/movement-collider ska däremot
         * vara en fysisk collider.
         *
         * Därmed ignoreras exempelvis:
         * - CombatHitbox
         * - Interaction trigger
         * - Detection trigger
         * - andra gameplay-hitboxes
         */
        if (collider.isTrigger)
            return false;

        PlayerStats player =
            collider.GetComponentInParent<
                PlayerStats>();

        if (player == null)
            return false;

        environment =
            player.GetComponent<
                PlayerEnvironmentState>();

        return environment != null;
    }

    private static bool IsPlayerCollider(
        Collider2D collider)
    {
        if (collider == null)
            return false;

        /*
         * Samma regel vid Exit som vid Enter.
         *
         * Annars skulle en Hitbox-trigger fortfarande kunna
         * skicka Exit och få IndoorZone att tro att spelaren
         * lämnat byggnaden.
         */
        if (collider.isTrigger)
            return false;

        return collider
            .GetComponentInParent<
                PlayerStats>() != null;
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void CacheZoneColliders()
    {
        zoneColliders =
            GetComponents<Collider2D>();
    }

    private void ValidateZoneColliders()
    {
        if (zoneColliders == null)
            return;

        for (int i = 0;
             i < zoneColliders.Length;
             i++)
        {
            Collider2D zoneCollider =
                zoneColliders[i];

            if (zoneCollider == null)
                continue;

            if (zoneCollider.isTrigger)
                continue;

            Debug.LogWarning(
                $"{nameof(IndoorZone)} på '{name}' har en " +
                $"{zoneCollider.GetType().Name} som inte är Trigger.",
                this
            );
        }
    }
}
using UnityEngine;

/// <summary>
/// Speglar PlayerMovements redan existerande road-state till
/// spelarens vanliga BuffSystem.
///
/// Komponenten gör INGEN egen road-query och ger INGEN movement bonus.
/// Den återanvänder endast:
///
/// PlayerMovement.IsOnRoad
/// PlayerMovement.RoadMovementSpeedMultiplier
///
/// och visar/döljer runtime-buffen Wanderlust.
///
/// Lägg komponenten på samma Player-root som PlayerMovement
/// och BuffSystem.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(BuffSystem))]
public sealed class PlayerRoadBuffController :
    MonoBehaviour
{
    // =========================================================
    // PRESENTATION
    // =========================================================

    [Header("Wanderlust")]

    [SerializeField]
    [Tooltip(
        "Ikonen som visas i spelarens vanliga buff row " +
        "medan spelaren färdas på en väg."
    )]
    private Sprite wanderlustIcon;


    // =========================================================
    // RUNTIME
    // =========================================================

    private PlayerMovement playerMovement;

    private BuffSystem buffSystem;

    private WanderlustBuff wanderlustBuff;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        playerMovement =
            GetComponent<PlayerMovement>();

        buffSystem =
            GetComponent<BuffSystem>();
    }


    private void OnEnable()
    {
        RefreshWanderlust();
    }


    private void LateUpdate()
    {
        /*
         * PlayerMovement uppdaterar redan road-state.
         *
         * Vi återanvänder det resultatet här i stället för att
         * göra en andra Surface-mesh-query.
         */
        RefreshWanderlust();
    }


    private void OnDisable()
    {
        RemoveWanderlust();
    }


    // =========================================================
    // STATE SYNC
    // =========================================================

    private void RefreshWanderlust()
    {
        if (playerMovement == null ||
            buffSystem == null)
        {
            return;
        }


        float multiplier =
            playerMovement
                .RoadMovementSpeedMultiplier;


        /*
         * Visa Wanderlust endast när vägen faktiskt ger
         * en positiv speed bonus.
         *
         * En framtida RoadProfile med multiplier 1.00
         * skapar alltså inte en "Increases by 0%" buff.
         */
        bool shouldHaveWanderlust =
            playerMovement.IsOnRoad &&
            multiplier >
                1.0001f;


        if (!shouldHaveWanderlust)
        {
            RemoveWanderlust();
            return;
        }


        // -----------------------------------------------------
        // ALREADY ACTIVE
        // -----------------------------------------------------

        if (wanderlustBuff != null &&
            buffSystem.HasBuff(
                wanderlustBuff))
        {
            /*
             * Om spelaren går direkt från t.ex. en 10%-väg
             * till en 15%-väg behåller vi samma buff-instance
             * men uppdaterar tooltip-värdet.
             */
            wanderlustBuff
                .SetMovementSpeedMultiplier(
                    multiplier
                );

            return;
        }


        // -----------------------------------------------------
        // ADD
        // -----------------------------------------------------

        wanderlustBuff =
            new WanderlustBuff(
                wanderlustIcon,
                multiplier
            );


        if (!buffSystem.AddRuntimeBuff(
                wanderlustBuff))
        {
            wanderlustBuff =
                null;
        }
    }


    private void RemoveWanderlust()
    {
        if (wanderlustBuff == null)
            return;


        if (buffSystem != null &&
            buffSystem.HasBuff(
                wanderlustBuff))
        {
            buffSystem.RemoveBuff(
                wanderlustBuff
            );
        }


        wanderlustBuff =
            null;
    }
}

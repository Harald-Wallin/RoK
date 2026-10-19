using UnityEngine;

/// <summary>
/// Runtime-buff som representerar spelarens road-travel state.
///
/// VIKTIGT:
/// Den här buffen ger INTE movement speed-bonusen.
/// Bonusen ägs fortfarande av RoadProfile + PlayerMovement.
///
/// Buffens enda ansvar är presentation/state:
/// - namn
/// - ikon
/// - tooltip
/// - permanent duration medan spelaren står på väg
/// </summary>
public sealed class WanderlustBuff :
    ActiveBuff
{
    private readonly Sprite icon;

    private float movementSpeedMultiplier;


    public WanderlustBuff(
        Sprite icon,
        float movementSpeedMultiplier)
    {
        this.icon =
            icon;

        this.movementSpeedMultiplier =
            Mathf.Max(
                1f,
                movementSpeedMultiplier
            );

        duration =
            float.PositiveInfinity;
    }


    // =========================================================
    // PRESENTATION
    // =========================================================

    public override Sprite Icon =>
        icon;


    public override string Name =>
        "Wanderlust";


    public override string GetDescription(
        CharacterStats viewer)
    {
        float increasePercent =
            Mathf.Max(
                0f,
                (
                    movementSpeedMultiplier -
                    1f
                ) *
                100f
            );


        return
            "Travelling by road.\n" +
            $"Increases your Movement Speed by " +
            $"{increasePercent:0.#}%.";
    }


    // =========================================================
    // PERMANENT STATE
    // =========================================================

    public override bool IsFinished =>
        false;


    public override float RemainingTime =>
        float.PositiveInfinity;


    /*
     * Wanderlust är ett miljötillstånd, inte en encounter-buff.
     * PlayerRoadBuffController tar bort den när spelaren lämnar vägen.
     */
    public override bool RemoveOnDeath =>
        false;


    public override bool RemoveOnEncounterReset =>
        false;


    // =========================================================
    // RUNTIME
    // =========================================================

    public void SetMovementSpeedMultiplier(
        float multiplier)
    {
        movementSpeedMultiplier =
            Mathf.Max(
                1f,
                multiplier
            );
    }


    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        /*
         * Ingen timer.
         *
         * Buffen lever tills PlayerRoadBuffController uttryckligen
         * tar bort den.
         */
    }
}


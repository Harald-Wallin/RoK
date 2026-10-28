/// <summary>
/// Samlad information om en pågående interaktionsbegäran.
///
/// Contexten skickas till alla IInteractionOption-implementationer
/// så att de inte själva behöver söka efter spelaren eller
/// det klickade världsmålet.
/// </summary>
public readonly struct InteractionContext
{
    public InteractionContext(
        PlayerStats player,
        InteractionTarget target)
    {
        Player = player;
        Target = target;
    }

    public PlayerStats Player { get; }

    public InteractionTarget Target { get; }

    public bool IsValid =>
        Player != null &&
        Target != null;
}

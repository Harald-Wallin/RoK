/// <summary>
/// Transient gameplay fact, consumed by the player's deed manager.
/// The event itself is not saved; the resulting progress is.
/// </summary>
public abstract class DeedProgressEvent
{
    protected DeedProgressEvent(PlayerStats actor)
    {
        Actor = actor;
    }

    public PlayerStats Actor { get; }
}

public sealed class DeedCharacterDefeatedEvent : DeedProgressEvent
{
    public DeedCharacterDefeatedEvent(
        PlayerStats actor,
        CharacterDefeatedResult defeat)
        : base(actor)
    {
        Defeat = defeat;
    }

    public CharacterDefeatedResult Defeat { get; }
}

public sealed class DeedWeaponComboHitEvent : DeedProgressEvent
{
    public DeedWeaponComboHitEvent(
        PlayerStats actor,
        WeaponFamilyDefinition weaponFamily,
        int landedHits)
        : base(actor)
    {
        WeaponFamily = weaponFamily;
        LandedHits = landedHits;
    }

    public WeaponFamilyDefinition WeaponFamily { get; }

    public int LandedHits { get; }
}

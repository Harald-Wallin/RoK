using UnityEngine;

/// <summary>
/// Tidsbegränsad runtime-buff som endast används för
/// presentation.
///
/// Den behöver ingen AbilityEffect och ändrar inga stats.
/// </summary>
public sealed class RuntimeDisplayBuff :
    ActiveBuff
{
    private readonly string displayName;
    private readonly string description;
    private readonly Sprite displayIcon;

    private readonly bool removeOnDeath;
    private readonly bool removeOnEncounterReset;

    public RuntimeDisplayBuff(
        string name,
        string description,
        Sprite icon,
        float duration,
        bool removeOnDeath = true,
        bool removeOnEncounterReset = true)
    {
        displayName =
            string.IsNullOrWhiteSpace(name)
                ? "Active Effect"
                : name;

        this.description =
            description ?? string.Empty;

        displayIcon = icon;

        this.duration =
            Mathf.Max(
                0.01f,
                duration);

        this.removeOnDeath =
            removeOnDeath;

        this.removeOnEncounterReset =
            removeOnEncounterReset;
    }

    public override string Name =>
        displayName;

    public override Sprite Icon =>
        displayIcon;

    public override bool RemoveOnDeath =>
        removeOnDeath;

    public override bool RemoveOnEncounterReset =>
        removeOnEncounterReset;

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        elapsed +=
            Mathf.Max(
                0f,
                deltaTime);
    }

    public override string GetDescription(
        CharacterStats viewer)
    {
        return description;
    }
}

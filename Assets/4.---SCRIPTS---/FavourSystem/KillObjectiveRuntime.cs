using System;

public sealed class KillObjectiveRuntime :
    FavourObjectiveRuntime
{
    private readonly KillObjectiveData
        killData;

    private int currentKills;

    public KillObjectiveRuntime(
        KillObjectiveData data,
        FavourRuntime favour)
        : base(
            data,
            favour)
    {
        killData =
            data;
    }

    public override bool IsComplete =>
        currentKills >=
        RequiredProgress;

    public override int CurrentProgress =>
        currentKills;

    public override int RequiredProgress =>
        killData != null
            ? killData.RequiredKills
            : 1;

    public override bool IsRelevantToEntity(
        string entityId)
    {
        if (killData == null ||
            string.IsNullOrWhiteSpace(
                entityId))
        {
            return false;
        }

        return string.Equals(
            killData.TargetEntityId,
            entityId,
            StringComparison.Ordinal
        );
    }

    protected override void OnCharacterDefeated(
        CharacterDefeatedResult result)
    {
        if (killData == null ||
            result == null)
        {
            return;
        }

        string requiredEntityId =
            killData.TargetEntityId;

        string defeatedEntityId =
            result.VictimEntityId;

        if (string.IsNullOrWhiteSpace(
                requiredEntityId) ||
            string.IsNullOrWhiteSpace(
                defeatedEntityId))
        {
            return;
        }

        if (!string.Equals(
                defeatedEntityId,
                requiredEntityId,
                StringComparison.Ordinal))
        {
            return;
        }

        PlayerStats player =
            Favour?.Manager?.Player;

        if (player == null)
            return;

        if (!result.HasMinimumDamageShare(
                player,
                killData.MinimumDamageShare))
        {
            return;
        }

        currentKills =
            Math.Min(
                currentKills + 1,
                RequiredProgress
            );

        RaiseProgressChanged();
    }

    public override void ResetProgress()
    {
        currentKills = 0;

        RaiseProgressChanged();
    }
}
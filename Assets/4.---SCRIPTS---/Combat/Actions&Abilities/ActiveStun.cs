public sealed class ActiveStun :
    ActiveBuff
{
    private bool applied;

    public ActiveStun(
        StunEffect effect)
    {
        sourceEffect = effect;
        duration = effect.duration;
    }

    public override void OnApplied(
        CharacterStats target)
    {
        if (target == null ||
            applied)
        {
            return;
        }

        target.AddStun();
        applied = true;
    }

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        elapsed += deltaTime;
    }

    public override void OnRemoved(
        CharacterStats target)
    {
        if (target == null ||
            !applied)
        {
            return;
        }

        target.RemoveStun();
        applied = false;
    }
}
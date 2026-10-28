public class ActiveStrengthBuff : ActiveBuff
{
    private bool applied = false;
    private StatModifier modifier;

    public ActiveStrengthBuff(StrengthBuffEffect effect)
    {
        duration = effect.duration;
        sourceEffect = effect;

        modifier = new StatModifier(
            StatType.Strength,
            effect.strengthMultiplier,
            ModifierType.Percent,
            this,
            ModifierSourceType.Buff
        );
    }

    public override void Update(float deltaTime, CharacterStats target)
    {
        if (!applied)
        {
            target.AddModifier(modifier);
            applied = true;
        }

        elapsed += deltaTime;

        if (IsFinished)
        {
            target.RemoveModifiersFromSource(this);
        }
    }
}


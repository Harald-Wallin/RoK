
public abstract class DamageQueryEffectRuntime :
    IEffectRuntime,
    IEffectQuery<DamageContext>
{
    protected readonly CharacterStats Owner;

    protected int Stacks { get; private set; }

    protected DamageQueryEffectRuntime(
        EffectContext context)
    {
        Owner = context.Target;
    }

    public void Activate(int stacks)
    {
        Stacks = stacks > 0 ? stacks : 1;
    }

    public void SetStacks(int stacks)
    {
        Stacks = stacks > 0 ? stacks : 1;
    }

    public void Deactivate()
    {
        Stacks = 0;
    }

    public abstract void Modify(
        DamageContext context);
}


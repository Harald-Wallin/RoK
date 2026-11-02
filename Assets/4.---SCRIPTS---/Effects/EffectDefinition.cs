
using UnityEngine;

/// <summary>
/// Immutable effect configuration.
///
/// No timers, active stacks or character-specific
/// mutable state may be stored in this asset.
/// </summary>
public abstract class EffectDefinition :
    ScriptableObject
{
    public abstract IEffectRuntime CreateRuntime(
        EffectContext context
    );

    public virtual string GetDescription()
    {
        return name;
    }
}

/// <summary>
/// One independently owned active effect instance.
/// </summary>
public interface IEffectRuntime
{
    void Activate(int stacks);

    void SetStacks(int stacks);

    void Deactivate();
}

/// <summary>
/// Optional contract for effects that modify
/// gameplay calculations.
///
/// Future contexts:
/// DamageContext, MovementContext,
/// ExperienceContext, ReputationContext.
/// </summary>
public interface IEffectQuery<TContext>
    where TContext : class
{
    void Modify(TContext context);
}


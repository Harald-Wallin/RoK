
using UnityEngine;

/// <summary>
/// Reusable, immutable gameplay-effect configuration.
///
/// Active timing, stack counts and source ownership
/// belong to runtime instances, not ScriptableObjects.
/// </summary>
public abstract class EffectDefinition : ScriptableObject
{
    public abstract IEffectRuntime CreateRuntime(
        EffectContext context
    );

    public virtual string GetDescription()
    {
        return name;
    }

    /// <summary>
    /// Preview of an effect at a specific stack count
    /// or talent rank.
    /// </summary>
    public virtual string GetDescriptionForStacks(
        int stacks)
    {
        string description = GetDescription();

        return stacks > 1
            ? $"{description} (x{stacks})"
            : description;
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
/// Allows active effects to modify contextual
/// gameplay calculations.
/// </summary>
public interface IEffectQuery<TContext>
    where TContext : class
{
    void Modify(TContext context);
}

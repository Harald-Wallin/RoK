using UnityEngine;

public abstract class ActiveBuff
{
    protected AbilityEffect sourceEffect;

    public float duration;

    protected float elapsed;

    public int stacks = 1;

    public virtual bool IsStackable =>
        sourceEffect != null &&
        sourceEffect.stackable;

    public virtual int MaxStacks =>
        sourceEffect != null
            ? Mathf.Max(
                1,
                sourceEffect.maxStacks)
            : 1;

    public virtual bool IsFinished =>
        elapsed >= duration;

    public virtual float RemainingTime =>
        Mathf.Max(
            0f,
            duration - elapsed);

    public virtual bool RemoveOnDeath =>
        sourceEffect == null ||
        sourceEffect.removeOnDeath;

    public virtual bool RemoveOnEncounterReset =>
        sourceEffect == null ||
        sourceEffect.removeOnEncounterReset;

    public virtual Sprite Icon =>
        sourceEffect != null
            ? sourceEffect.icon
            : null;

    public virtual string Name =>
        sourceEffect != null
            ? sourceEffect.name
            : GetType().Name;

    public AbilityEffect SourceEffect =>
        sourceEffect;

    public virtual void OnApplied(
        CharacterStats target)
    {
    }

    public abstract void Update(
        float deltaTime,
        CharacterStats target);

    public virtual void OnRemoved(
        CharacterStats target)
    {
    }

    public virtual void OnStackChanged(
        CharacterStats target)
    {
    }

    public virtual string GetDescription(
        CharacterStats viewer)
    {
        return sourceEffect != null
            ? sourceEffect.GetTooltipText(
                viewer)
            : string.Empty;
    }

    public void ResetDuration()
    {
        elapsed = 0f;
    }

    public void SetDuration(
        float newDuration)
    {
        duration =
            Mathf.Max(
                0f,
                newDuration);
    }
}
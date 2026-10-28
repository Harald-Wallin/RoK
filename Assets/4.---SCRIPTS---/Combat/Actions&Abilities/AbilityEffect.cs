using UnityEngine;

/// <summary>
/// Basklass för alla modulära ability-effekter.
/// </summary>
public abstract class AbilityEffect :
    ScriptableObject
{
    [Header("Presentation")]

    public Sprite icon;

    [Header("Execution")]

    [SerializeField]
    private AbilityEffectExecutionTiming executionTiming =
        AbilityEffectExecutionTiming.Immediate;

    [SerializeField]
    private AbilityEffectTargetMode targetMode =
        AbilityEffectTargetMode.EachAffectedTarget;

    [SerializeField]
    [Tooltip(
        "Om pipelinen ska hoppa över effekten när inget " +
        "CharacterStats-target finns."
    )]
    private bool requiresCharacterTarget = true;

    [Header("Buff Rules")]

    public bool stackable;

    [Min(1)]
    public int maxStacks = 1;

    public bool refreshDurationOnStack = true;

    public bool removeOnDeath = true;

    [Tooltip(
        "Om en runtime-buff skapad av effekten ska tas bort " +
        "när en NPC gör full encounter-reset, exempelvis vid leash."
    )]
    public bool removeOnEncounterReset = true;

    public AbilityEffectExecutionTiming ExecutionTiming =>
        executionTiming;

    public AbilityEffectTargetMode TargetMode =>
        targetMode;

    public bool RequiresCharacterTarget =>
        requiresCharacterTarget;

    public virtual void Execute(
        AbilityEffectExecutionContext context)
    {
        if (context == null)
            return;

        if (RequiresCharacterTarget &&
            context.Target == null)
        {
            return;
        }

        Apply(
            context.Caster,
            context.Target
        );
    }

    public virtual void Apply(
        CharacterStats caster,
        CharacterStats target)
    {
    }

    /// <summary>
    /// Legacy-factory.
    /// </summary>
    public virtual ActiveBuff CreateActiveBuff(
        CharacterStats source,
        CharacterStats target)
    {
        return null;
    }

    /// <summary>
    /// Context-baserad factory som bevarar direct source,
    /// credit owner och ability genom deferred effects.
    /// </summary>
    public virtual ActiveBuff CreateActiveBuff(
        DamageSourceContext source,
        CharacterStats target)
    {
        return CreateActiveBuff(
            source.DirectSource,
            target
        );
    }

    public virtual string GetTooltipText(
        CharacterStats caster)
    {
        return string.Empty;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        maxStacks =
            Mathf.Max(
                1,
                maxStacks
            );
    }
#endif
}
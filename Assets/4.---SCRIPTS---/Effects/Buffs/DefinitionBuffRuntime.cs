
using UnityEngine;

/// <summary>
/// ActiveBuff adapter for the shared effect system.
///
/// BuffSystem owns duration and stacking.
/// CharacterEffectController owns active effects.
/// </summary>
public sealed class DefinitionBuffRuntime :
    ActiveBuff
{
    private readonly BuffDefinition definition;

    private readonly DamageSourceContext damageSource;

    private CharacterEffectController controller;

    private EffectHandle effectHandle;

    public BuffDefinition Definition =>
        definition;

    public bool HasActiveEffects =>
        effectHandle != null &&
        effectHandle.IsActive;

    public override string Name =>
        definition.DisplayName;

    public override Sprite Icon =>
        definition.Icon;

    public override bool IsStackable =>
        definition.MaxStacks > 1;

    public override int MaxStacks =>
        definition.MaxStacks;

    public override bool RemoveOnDeath =>
        definition.RemoveOnDeath;

    public override bool RemoveOnEncounterReset =>
        definition.RemoveOnEncounterReset;

    // =====================================================
    // CONSTRUCTION
    // =====================================================

    public DefinitionBuffRuntime(
        BuffDefinition definition,
        DamageSourceContext damageSource)
    {
        this.definition = definition;
        this.damageSource = damageSource;

        duration = definition.Duration;
    }

    // =====================================================
    // SOURCE IDENTITY
    // =====================================================

    public bool MatchesSource(
        DamageSourceContext other)
    {
        return damageSource.DirectSource ==
                   other.DirectSource &&
               damageSource.CreditOwner ==
                   other.CreditOwner;
    }

    // =====================================================
    // APPLY
    // =====================================================

    public override void OnApplied(
        CharacterStats target)
    {
        controller =
            CharacterEffectController.GetOrCreate(
                target
            );

        if (controller == null)
            return;

        effectHandle = controller.Activate(
            definition.Effects,
            damageSource,
            ModifierSourceType.Buff,
            stacks
        );
    }

    // =====================================================
    // UPDATE
    // =====================================================

    public override void Update(
        float deltaTime,
        CharacterStats target)
    {
        elapsed += Mathf.Max(
            0f,
            deltaTime
        );
    }

    // =====================================================
    // STACK CHANGED
    // =====================================================

    public override void OnStackChanged(
        CharacterStats target)
    {
        if (controller == null ||
            effectHandle == null)
        {
            return;
        }

        if (!controller.SetStacks(
                effectHandle,
                stacks))
        {
            effectHandle = null;
        }
    }

    // =====================================================
    // REMOVE
    // =====================================================

    public override void OnRemoved(
        CharacterStats target)
    {
        if (controller != null &&
            effectHandle != null)
        {
            controller.Deactivate(
                effectHandle
            );
        }

        effectHandle = null;
        controller = null;
    }

    // =====================================================
    // TOOLTIP
    // =====================================================

    public override string GetDescription(
        CharacterStats viewer)
    {
        return definition.GetDescription();
    }
}


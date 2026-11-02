
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterStats))]
public sealed class CharacterEffectController :
    MonoBehaviour
{
    private readonly List<EffectHandle>
        activeHandles = new();

    private CharacterStats stats;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    private void OnDestroy()
    {
        for (int i = activeHandles.Count - 1;
             i >= 0;
             i--)
        {
            activeHandles[i]?.Cleanup();
        }

        activeHandles.Clear();
    }

    // =====================================================
    // AUTOMATIC RESOLUTION
    // =====================================================

    public static CharacterEffectController GetOrCreate(
        CharacterStats target)
    {
        if (target == null)
            return null;

        CharacterEffectController controller =
            target.GetComponent<CharacterEffectController>();

        if (controller == null)
        {
            controller =
                target.gameObject
                    .AddComponent<CharacterEffectController>();
        }

        return controller;
    }

    // =====================================================
    // ACTIVATE EFFECT COLLECTION
    // =====================================================

    public EffectHandle Activate(
        IReadOnlyList<EffectDefinition> definitions,
        DamageSourceContext source,
        ModifierSourceType modifierSourceType,
        int stacks = 1)
    {
        if (!isActiveAndEnabled || stats == null)
            return null;

        EffectContext context =
            new EffectContext(
                stats,
                source,
                modifierSourceType
            );

        EffectHandle handle =
            new EffectHandle(context);

        if (!handle.TryActivate(
                definitions,
                stacks))
        {
            return null;
        }

        activeHandles.Add(handle);

        return handle;
    }

    // =====================================================
    // STACK UPDATE
    // =====================================================

    public bool SetStacks(
        EffectHandle handle,
        int stacks)
    {
        if (handle == null ||
            !activeHandles.Contains(handle))
        {
            return false;
        }

        if (handle.TrySetStacks(stacks))
            return true;

        Deactivate(handle);
        return false;
    }

    // =====================================================
    // DEACTIVATE
    // =====================================================

    public bool Deactivate(EffectHandle handle)
    {
        if (handle == null ||
            !activeHandles.Remove(handle))
        {
            return false;
        }

        handle.Cleanup();

        return true;
    }

    // =====================================================
    // CONTEXTUAL EFFECT QUERIES
    // =====================================================

    public void ApplyTo<TContext>(TContext query)
        where TContext : class
    {
        if (query == null)
            return;

        for (int i = 0;
             i < activeHandles.Count;
             i++)
        {
            activeHandles[i]?.ApplyTo(query);
        }
    }

    public int ActiveEffectGroupCount =>
        activeHandles.Count;
}


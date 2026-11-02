
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EffectHandle
{
    private readonly EffectContext context;

    private readonly List<IEffectRuntime>
        runtimes = new();

    public bool IsActive { get; private set; }

    internal EffectHandle(EffectContext context)
    {
        this.context = context;
    }

    // =====================================================
    // ACTIVATION
    // =====================================================

    internal bool TryActivate(
        IReadOnlyList<EffectDefinition> definitions,
        int stacks)
    {
        if (IsActive)
            return false;

        try
        {
            if (definitions != null)
            {
                foreach (EffectDefinition definition
                         in definitions)
                {
                    if (definition == null)
                        continue;

                    IEffectRuntime runtime =
                        definition.CreateRuntime(context);

                    if (runtime == null)
                    {
                        throw new InvalidOperationException(
                            $"Effect '{definition.name}' " +
                            "returned a null runtime."
                        );
                    }

                    // Add before activation so partial
                    // activation can also be cleaned up.
                    runtimes.Add(runtime);

                    runtime.Activate(
                        Mathf.Max(1, stacks)
                    );
                }
            }

            IsActive = true;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception,
                context.Target
            );

            Cleanup();
            return false;
        }
    }

    // =====================================================
    // STACK UPDATES
    // =====================================================

    internal bool TrySetStacks(int stacks)
    {
        if (!IsActive)
            return false;

        try
        {
            int count = Mathf.Max(1, stacks);

            foreach (IEffectRuntime runtime in runtimes)
            {
                runtime.SetStacks(count);
            }

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception,
                context.Target
            );

            Cleanup();
            return false;
        }
    }

    // =====================================================
    // GAMEPLAY QUERIES
    // =====================================================

    internal void ApplyTo<TContext>(TContext query)
        where TContext : class
    {
        if (!IsActive || query == null)
            return;

        foreach (IEffectRuntime runtime in runtimes)
        {
            if (runtime is IEffectQuery<TContext> effect)
            {
                effect.Modify(query);
            }
        }
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    internal void Cleanup()
    {
        IsActive = false;

        for (int i = runtimes.Count - 1;
             i >= 0;
             i--)
        {
            try
            {
                runtimes[i]?.Deactivate();
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    context.Target
                );
            }
        }

        runtimes.Clear();
    }
}


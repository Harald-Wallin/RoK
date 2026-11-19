using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public sealed class BuffSystem :
    MonoBehaviour
{
    private readonly List<ActiveBuff>
        activeBuffs =
            new();

    private CharacterStats stats;

    public event Action<ActiveBuff, BuffSystem>
        OnBuffAdded;

    public event Action<ActiveBuff, BuffSystem>
        OnBuffRemoved;

    public event Action<ActiveBuff, BuffSystem>
    OnBuffChanged;

    private void Awake()
    {
        stats =
            GetComponent<CharacterStats>();
    }

    private void Update()
    {
        for (int i = activeBuffs.Count - 1;
             i >= 0;
             i--)
        {
            ActiveBuff buff =
                activeBuffs[i];

            if (buff == null)
            {
                activeBuffs.RemoveAt(i);
                continue;
            }

            buff.Update(
                Time.deltaTime,
                stats
            );

            if (!buff.IsFinished)
                continue;

            RemoveBuffAt(i);
        }
    }

    public List<ActiveBuff> GetActiveBuffs()
    {
        return new List<ActiveBuff>(
            activeBuffs
        );
    }

    public bool AddRuntimeBuff(
    ActiveBuff buff)
    {
        if (buff == null ||
            stats == null)
        {
            return false;
        }

        if (activeBuffs.Contains(buff))
            return true;

        activeBuffs.Add(
            buff);

        buff.OnApplied(
            stats);

        OnBuffAdded?.Invoke(
            buff,
            this);

        return true;
    }


    // =====================================================
    // SHARED EFFECT DEFINITION BUFFS
    // =====================================================

    public bool ApplyDefinition(
    BuffDefinition definition,
    DamageSourceContext source)
    {
        if (!isActiveAndEnabled ||
            definition == null ||
            stats == null)
        {
            return false;
        }

        float remainingBefore =
            GetRemainingDefinitionTime(definition);

        DefinitionBuffRuntime runtime =
            GetDefinitionBuff(definition, source);

        if (runtime != null)
        {
            if (runtime.stacks < runtime.MaxStacks)
            {
                runtime.stacks++;
                runtime.OnStackChanged(stats);

                if (!runtime.HasActiveEffects)
                {
                    RemoveBuff(runtime);
                    return false;
                }
            }

            if (definition.RefreshDurationOnReapply)
                runtime.ResetDuration();

            OnBuffChanged?.Invoke(runtime, this);
        }
        else
        {
            runtime =
                new DefinitionBuffRuntime(
                    definition,
                    source);

            if (!AddRuntimeBuff(runtime))
                return false;

            if (!runtime.HasActiveEffects)
            {
                RemoveBuff(runtime);

                Debug.LogError(
                    $"Failed to activate buff " +
                    $"'{definition.DisplayName}'.",
                    this);

                return false;
            }
        }

        float added = Mathf.Max(
            0f,
            GetRemainingDefinitionTime(definition) -
            remainingBefore);

        if (added > 0f)
        {
            BuffApplicationEvents.Raise(
                new BuffApplicationEvent(
                    source,
                    stats,
                    definition,
                    added / definition.Duration,
                    false));
        }

        return true;
    }

    private float GetRemainingDefinitionTime(
        BuffDefinition definition)
    {
        float remaining = 0f;

        foreach (ActiveBuff buff in activeBuffs)
        {
            if (buff is DefinitionBuffRuntime runtime &&
                runtime.Definition == definition &&
                runtime.HasActiveEffects)
            {
                remaining = Mathf.Max(
                    remaining,
                    runtime.RemainingTime);
            }
        }

        return remaining;
    }

    public bool TryDispelBuff(
        ActiveBuff buff,
        DamageSourceContext dispeller)
    {
        if (stats == null ||
            dispeller.CreditOwner == null ||
            buff is not DefinitionBuffRuntime runtime ||
            runtime.IsFinished ||
            !activeBuffs.Contains(buff))
        {
            return false;
        }

        bool hostile =
            TargetRelationResolver.Resolve(
                dispeller.CreditOwner,
                stats.gameObject) ==
            TargetRelation.Hostile;

        // Cleanse removes harmful effects from allies.
        // Offensive dispel removes beneficial effects from enemies.
        if (runtime.Definition.IsDebuff ? hostile : !hostile)
            return false;

        if (!RemoveBuff(buff))
            return false;

        BuffApplicationEvents.Raise(
            new BuffApplicationEvent(
                dispeller,
                stats,
                runtime.Definition,
                0f,
                true));

        return true;
    }

    private DefinitionBuffRuntime GetDefinitionBuff(
        BuffDefinition definition,
        DamageSourceContext source)
    {
        foreach (ActiveBuff buff in activeBuffs)
        {
            if (buff is not
                DefinitionBuffRuntime runtime)
            {
                continue;
            }

            if (runtime.Definition == definition &&
                runtime.MatchesSource(source))
            {
                return runtime;
            }
        }

        return null;
    }


    public bool RemoveBuff(
        ActiveBuff buff)
    {
        if (buff == null)
            return false;

        int index =
            activeBuffs.IndexOf(
                buff);

        if (index < 0)
            return false;

        RemoveBuffAt(
            index);

        return true;
    }

    public bool HasBuff(
        ActiveBuff buff)
    {
        return
            buff != null &&
            activeBuffs.Contains(buff);
    }

    public bool HasEffect(
        AbilityEffect effect)
    {
        if (effect == null)
            return false;

        return activeBuffs.Exists(
            buff =>
                buff != null &&
                buff.SourceEffect == effect
        );
    }

    public ActiveBuff GetBuff(
        AbilityEffect effect)
    {
        if (effect == null)
            return null;

        return activeBuffs.Find(
            buff =>
                buff != null &&
                buff.SourceEffect == effect
        );
    }


    /// <summary>
    /// Refreshes an existing effect's duration
    /// without adding another stack.
    ///
    /// Useful when multiple effects share one
    /// authoritative lifetime.
    /// </summary>
    public bool RefreshEffectDuration(
        AbilityEffect effect,
        float duration)
    {
        ActiveBuff buff =
            GetBuff(effect);

        if (buff == null)
            return false;

        buff.SetDuration(duration);
        buff.ResetDuration();

        OnBuffChanged?.Invoke(
            buff,
            this
        );

        return true;
    }


    public bool ApplyEffect(
    AbilityEffect effect,
    DamageSourceContext source,
    float? overrideDuration = null)
    {
        if (effect == null ||
            stats == null)
        {
            return false;
        }

        ActiveBuff existing =
            GetBuff(effect);

        if (existing != null)
        {
            HandleExistingBuff(
                existing,
                effect,
                overrideDuration
            );

            OnBuffChanged?.Invoke(
                existing,
                this
            );

            return true;
        }

        ActiveBuff buff =
            effect.CreateActiveBuff(
                source,
                stats
            );

        if (buff == null)
        {
            Debug.LogWarning(
                $"Effekten '{effect.name}' returnerade ingen " +
                $"ActiveBuff från CreateActiveBuff().",
                effect
            );

            return false;
        }

        if (overrideDuration.HasValue)
        {
            buff.SetDuration(
                overrideDuration.Value
            );
        }

        if (buff.duration <= 0f)
        {
            Debug.LogWarning(
                $"Buffen '{effect.name}' har duration 0 och " +
                $"kommer avslutas omedelbart.",
                effect
            );
        }

        activeBuffs.Add(
            buff
        );

        buff.OnApplied(
            stats
        );

        OnBuffAdded?.Invoke(
            buff,
            this
        );

        return true;
    }

    public void RemoveEncounterResetBuffs()
    {
        for (int i = activeBuffs.Count - 1;
             i >= 0;
             i--)
        {
            ActiveBuff buff =
                activeBuffs[i];

            if (buff == null ||
                !buff.RemoveOnEncounterReset)
            {
                continue;
            }

            RemoveBuffAt(i);
        }
    }

    public bool ApplyEffect(
        AbilityEffect effect,
        CharacterStats source = null,
        float? overrideDuration = null)
    {
        if (effect == null ||
            stats == null)
        {
            return false;
        }

        ActiveBuff existing =
            GetBuff(effect);

        if (existing != null)
        {
            HandleExistingBuff(
                existing,
                effect,
                overrideDuration
            );

            OnBuffChanged?.Invoke(
                existing,
                this
            );

            return true;
        }

        ActiveBuff buff =
            effect.CreateActiveBuff(
                source,
                stats
            );

        if (buff == null)
        {
            Debug.LogWarning(
                $"Effekten '{effect.name}' returnerade ingen " +
                $"ActiveBuff från CreateActiveBuff().",
                effect
            );

            return false;
        }

        if (overrideDuration.HasValue)
        {
            buff.SetDuration(
                overrideDuration.Value
            );
        }

        if (buff.duration <= 0f)
        {
            Debug.LogWarning(
                $"Buffen '{effect.name}' har duration 0 och " +
                $"kommer avslutas omedelbart.",
                effect
            );
        }

        activeBuffs.Add(buff);

        buff.OnApplied(
            stats
        );

        OnBuffAdded?.Invoke(
            buff,
            this
        );

        return true;
    }

    private void HandleExistingBuff(
    ActiveBuff existing,
    AbilityEffect effect,
    float? overrideDuration)
    {
        bool stackChanged = false;

        if (effect.stackable &&
            existing.stacks <
            existing.MaxStacks)
        {
            existing.stacks++;
            stackChanged = true;
        }

        if (stackChanged)
        {
            existing.OnStackChanged(
                stats
            );
        }

        if (overrideDuration.HasValue)
        {
            existing.SetDuration(
                overrideDuration.Value
            );

            existing.ResetDuration();
            return;
        }

        if (effect.refreshDurationOnStack)
        {
            existing.ResetDuration();
        }
    }

    public void ClearAll()
    {
        for (int i = activeBuffs.Count - 1;
             i >= 0;
             i--)
        {
            RemoveBuffAt(i);
        }
    }

    public void RemoveDeathRemovableBuffs()
    {
        for (int i = activeBuffs.Count - 1;
             i >= 0;
             i--)
        {
            ActiveBuff buff =
                activeBuffs[i];

            if (buff == null ||
                !buff.RemoveOnDeath)
            {
                continue;
            }

            RemoveBuffAt(i);
        }
    }

    public bool RemoveEffect(
        AbilityEffect effect)
    {
        if (effect == null)
            return false;

        for (int i = activeBuffs.Count - 1;
             i >= 0;
             i--)
        {
            ActiveBuff buff =
                activeBuffs[i];

            if (buff == null ||
                buff.SourceEffect != effect)
            {
                continue;
            }

            RemoveBuffAt(i);
            return true;
        }

        return false;
    }

    private void RemoveBuffAt(
        int index)
    {
        if (index < 0 ||
            index >= activeBuffs.Count)
        {
            return;
        }

        ActiveBuff buff =
            activeBuffs[index];

        if (buff != null)
        {
            buff.OnRemoved(
                stats
            );
        }

        activeBuffs.RemoveAt(index);

        if (buff != null)
        {
            OnBuffRemoved?.Invoke(
                buff,
                this
            );
        }
    }
}
using System;
using System.Collections.Generic;

public sealed partial class PlayerDeedManager
{
    private readonly Dictionary<GeneralTraitDefinition, EffectHandle>
        activeGeneralTraits = new();

    private CharacterEffectController generalTraitController;

    public IReadOnlyCollection<GeneralTraitDefinition> ActiveGeneralTraits =>
        activeGeneralTraits.Keys;

    public event Action GeneralTraitsChanged;

    private void SynchronizeGeneralTraits()
    {
        if (!isActiveAndEnabled || player == null)
            return;

        HashSet<GeneralTraitDefinition> earnedTraits = new();

        foreach (DeedRuntime deed in deeds)
        {
            foreach (DeedRankDefinition rank in deed.Definition.Ranks)
            {
                if (!deed.HasCompleted(rank) || rank.Rewards == null)
                    continue;

                foreach (DeedRewardPreview reward in rank.Rewards)
                {
                    if (reward != null &&
                        reward.Kind == DeedRewardKind.GeneralTrait &&
                        reward.GeneralTrait != null)
                    {
                        earnedTraits.Add(reward.GeneralTrait);
                    }
                }
            }
        }

        List<GeneralTraitDefinition> removedTraits = new();

        foreach (var entry in activeGeneralTraits)
        {
            if (!earnedTraits.Contains(entry.Key) || !entry.Value.IsActive)
                removedTraits.Add(entry.Key);
        }

        bool changed = removedTraits.Count > 0;

        foreach (GeneralTraitDefinition trait in removedTraits)
        {
            EffectHandle handle = activeGeneralTraits[trait];
            activeGeneralTraits.Remove(trait);

            if (generalTraitController != null)
                generalTraitController.Deactivate(handle);
        }

        foreach (GeneralTraitDefinition trait in earnedTraits)
        {
            if (activeGeneralTraits.ContainsKey(trait))
                continue;

            if (generalTraitController == null)
                generalTraitController = CharacterEffectController.GetOrCreate(player);

            if (generalTraitController == null)
                break;

            EffectHandle handle = generalTraitController.Activate(
                trait.Effects,
                DamageSourceContext.FromDirectSource(player),
                ModifierSourceType.GeneralTrait);

            if (handle == null)
                continue;

            activeGeneralTraits.Add(trait, handle);
            changed = true;
        }

        if (changed)
            GeneralTraitsChanged?.Invoke();
    }

    private void ClearGeneralTraits()
    {
        if (activeGeneralTraits.Count == 0)
            return;

        if (generalTraitController != null)
        {
            foreach (EffectHandle handle in activeGeneralTraits.Values)
                generalTraitController.Deactivate(handle);
        }

        activeGeneralTraits.Clear();
        GeneralTraitsChanged?.Invoke();
    }
}

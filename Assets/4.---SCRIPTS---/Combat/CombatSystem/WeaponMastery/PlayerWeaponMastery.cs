using System.Collections.Generic;
using UnityEngine;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats), typeof(CharacterActionController))]
public sealed class PlayerWeaponMastery : MonoBehaviour
{
    private readonly Dictionary<WeaponFamilyDefinition, int> experience = new();
    private readonly HashSet<CharacterStats> eligibleTargets = new();

    private readonly Dictionary<WeaponFamilyDefinition, HashSet<string>>
    selectedMasteryNodes = new();

    private PlayerStats player;
    private CharacterActionController actions;
    private ActionExecutionContext pendingAction;
    private WeaponFamilyDefinition pendingFamily;

    private void Awake()
    {
        player = GetComponent<PlayerStats>();
        actions = GetComponent<CharacterActionController>();
    }

    private void OnEnable()
    {
        actions.OnExecutionStarted += HandleExecutionStarted;
        actions.OnEffectsExecuted += HandleEffectsExecuted;
    }

    private void OnDisable()
    {
        actions.OnExecutionStarted -= HandleExecutionStarted;
        actions.OnEffectsExecuted -= HandleEffectsExecuted;
        ClearPendingAttack();
    }

    public event Action Changed;

    public WeaponFamilyDefinition TrackedFamily { get; private set; }

    public readonly struct Progress
    {
        public int Level { get; }
        public int ExperienceInLevel { get; }
        public int ExperienceForNextLevel { get; }

        public bool IsMaxLevel => ExperienceForNextLevel == 0;

        public float WeaponDamageBonus => Mathf.Clamp(Level, 0, 25) / 100f;

        public float Fraction => IsMaxLevel
            ? 1f
            : (float)ExperienceInLevel / ExperienceForNextLevel;

        public Progress(int level, int experienceInLevel, int experienceForNextLevel)
        {
            Level = level;
            ExperienceInLevel = experienceInLevel;
            ExperienceForNextLevel = experienceForNextLevel;
        }
    }

    public Progress GetProgress(WeaponFamilyDefinition family)
    {
        if (family == null)
            return default;

        int remaining = GetExperience(family);
        int level = 1;
        int required = family.GetExperienceRequiredForNextLevel(level);

        while (required > 0 && remaining >= required)
        {
            remaining -= required;
            level++;
            required = family.GetExperienceRequiredForNextLevel(level);
        }

        return new Progress(level, required == 0 ? 0 : remaining, required);
    }

    public void SetTrackedFamily(WeaponFamilyDefinition family)
    {
        if (TrackedFamily == family)
            return;

        TrackedFamily = family;
        Changed?.Invoke();
    }

    public Dictionary<string, WeaponMasteryTreeDefinition.NodeState> GetMasteryNodeStates(
    WeaponFamilyDefinition family)
    {
        if (family == null || family.MasteryTree == null)
            return new Dictionary<string, WeaponMasteryTreeDefinition.NodeState>(
                StringComparer.Ordinal);

        selectedMasteryNodes.TryGetValue(family, out HashSet<string> selected);

        return family.MasteryTree.Evaluate(GetProgress(family).Level, selected);
    }

    public bool TryChooseMasteryNode(WeaponFamilyDefinition family, string nodeId)
    {
        if (family == null || string.IsNullOrWhiteSpace(nodeId))
            return false;

        var states = GetMasteryNodeStates(family);

        if (!states.TryGetValue(nodeId, out WeaponMasteryTreeDefinition.NodeState state) ||
            state != WeaponMasteryTreeDefinition.NodeState.Available)
        {
            return false;
        }

        if (!selectedMasteryNodes.TryGetValue(family, out HashSet<string> selected))
        {
            selected = new HashSet<string>(StringComparer.Ordinal);
            selectedMasteryNodes.Add(family, selected);
        }

        selected.Add(nodeId);
        Changed?.Invoke();
        return true;
    }

    public int GetExperience(WeaponFamilyDefinition family)
    {
        return family != null && experience.TryGetValue(family, out int amount)
            ? amount
            : 0;
    }

    private void HandleExecutionStarted(ActionExecutionContext action)
    {
        ClearPendingAttack();

        if (action.Caster != player)
            return;

        EquipmentManager equipment = EquipmentManager.Instance;

        if (equipment == null)
            return;

        ItemData weapon = equipment.GetEquippedWeapon(player);
        WeaponFamilyDefinition family = weapon != null ? weapon.WeaponFamily : null;

        if (family == null || !family.CanTrainWith(action.Ability))
            return;

        int minimumLevel = family.GetMinimumTargetLevel(player.Level);

        foreach (CharacterStats target in action.AffectedCharacters)
        {
            if (target != null &&
                target.level >= minimumLevel &&
                TargetRelationResolver.Resolve(player, target.gameObject) ==
                    TargetRelation.Hostile)
            {
                eligibleTargets.Add(target);
            }
        }

        if (eligibleTargets.Count == 0)
            return;

        pendingAction = action;
        pendingFamily = family;
    }

    private void HandleEffectsExecuted(
    ActionExecutionContext action,
    AbilityEffectPipelineResult result)
    {
        if (!ReferenceEquals(pendingAction, action))
            return;

        WeaponFamilyDefinition family = pendingFamily;
        bool earnedExperience = false;

        if (result != null && result.EffectExecutions > 0)
        {
            foreach (AbilityTargetHitResult hit in result.SuccessfulTargets)
            {
                if (hit.Outcome == AbilityTargetHitOutcome.Hit &&
                    eligibleTargets.Contains(hit.Target))
                {
                    earnedExperience = true;
                    break;
                }
            }
        }

        ClearPendingAttack();

        if (!earnedExperience)
            return;

        int previous = GetExperience(family);

        if (previous == int.MaxValue)
            return;

        int total = previous + 1;
        experience[family] = total;

#if UNITY_EDITOR
        Debug.Log($"[Weapon Mastery] {family.name} +1 XP ({total} total)", this);
#endif

        Changed?.Invoke();
    }

    private void ClearPendingAttack()
    {
        pendingAction = null;
        pendingFamily = null;
        eligibleTargets.Clear();
    }
}

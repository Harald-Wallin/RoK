using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RöK/Deeds/Objectives/Weapon Multi-Hit")]
public sealed class WeaponMultiHitDeedObjectiveDefinition : DeedObjectiveDefinition
{
    [SerializeField] private WeaponFamilyDefinition weaponFamily;

    [SerializeField, Min(1)]
    private int minimumEnemies = 3;

    [SerializeField]
    private bool requireSpecificAbilities;

    [SerializeField]
    private AbilityData[] requiredAbilities = Array.Empty<AbilityData>();

    // Kept for migration of assets created with the single-ability field.
    [SerializeField, HideInInspector]
    private AbilityData requiredAbility;

    public override DeedObjectiveRuntime CreateRuntime()
    {
        return new Runtime(this);
    }

    private bool MatchesAbility(AbilityData ability)
    {
        if (ability == null)
            return false;

        if (requireSpecificAbilities)
        {
            return requiredAbilities != null &&
                   Array.IndexOf(requiredAbilities, ability) >= 0;
        }

        // Supports existing assets before they have been migrated in the editor.
        if (requiredAbility != null)
            return ability == requiredAbility;

        return ability.IsBaseAttack || ability.AdvancesWeaponCombo;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (requiredAbility == null)
            return;

        if (requiredAbilities == null || requiredAbilities.Length == 0)
        {
            requiredAbilities = new[] { requiredAbility };
            requireSpecificAbilities = true;
        }

        requiredAbility = null;
    }
#endif

    private sealed class Runtime : DeedObjectiveRuntime
    {
        private readonly WeaponMultiHitDeedObjectiveDefinition definition;

        public Runtime(WeaponMultiHitDeedObjectiveDefinition definition)
        {
            this.definition = definition;
        }

        public override void Receive(DeedProgressEvent progressEvent)
        {
            if (progressEvent is not DeedWeaponAttackHitEvent attack ||
                definition.weaponFamily == null ||
                attack.WeaponFamily != definition.weaponFamily ||
                attack.EnemiesHit < Mathf.Max(1, definition.minimumEnemies) ||
                !definition.MatchesAbility(attack.Ability))
            {
                return;
            }

            AddProgress(1);
        }
    }
}
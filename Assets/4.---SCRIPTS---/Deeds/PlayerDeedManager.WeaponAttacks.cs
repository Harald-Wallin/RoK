using System.Collections.Generic;

public sealed partial class PlayerDeedManager
{
    private readonly HashSet<CharacterStats> weaponAttackEnemies = new();

    private CharacterActionController weaponAttackController;
    private ActionExecutionContext pendingDeedWeaponAction;
    private WeaponFamilyDefinition pendingDeedWeaponFamily;

    private void SubscribeToWeaponAttacks()
    {
        weaponAttackController = GetComponent<CharacterActionController>();

        if (weaponAttackController == null)
            return;

        weaponAttackController.OnExecutionStarted += CaptureDeedWeaponAttack;
        weaponAttackController.OnEffectsExecuted += ReportDeedWeaponAttack;
    }

    private void UnsubscribeFromWeaponAttacks()
    {
        if (weaponAttackController != null)
        {
            weaponAttackController.OnExecutionStarted -= CaptureDeedWeaponAttack;
            weaponAttackController.OnEffectsExecuted -= ReportDeedWeaponAttack;
        }

        weaponAttackController = null;
        ClearDeedWeaponAttack();
    }

    private void CaptureDeedWeaponAttack(ActionExecutionContext action)
    {
        ClearDeedWeaponAttack();

        if (action.Caster != player)
            return;

        EquipmentManager equipment = EquipmentManager.Instance;
        ItemData weapon = equipment != null ? equipment.GetEquippedWeapon(player) : null;
        WeaponFamilyDefinition family = weapon != null ? weapon.WeaponFamily : null;

        if (family == null)
            return;

        foreach (CharacterStats target in action.AffectedCharacters)
        {
            if (target != null &&
                target != player &&
                TargetRelationResolver.Resolve(player, target.gameObject) ==
                    TargetRelation.Hostile)
            {
                weaponAttackEnemies.Add(target);
            }
        }

        if (weaponAttackEnemies.Count == 0)
            return;

        pendingDeedWeaponAction = action;
        pendingDeedWeaponFamily = family;
    }

    private void ReportDeedWeaponAttack(
        ActionExecutionContext action,
        AbilityEffectPipelineResult result)
    {
        if (!ReferenceEquals(pendingDeedWeaponAction, action))
            return;

        int enemiesHit = 0;

        if (result != null)
        {
            foreach (AbilityTargetHitResult hit in result.SuccessfulTargets)
            {
                if (hit.Outcome == AbilityTargetHitOutcome.Hit &&
                    hit.EffectExecuted &&
                    weaponAttackEnemies.Remove(hit.Target))
                {
                    enemiesHit++;
                }
            }
        }

        WeaponFamilyDefinition family = pendingDeedWeaponFamily;
        ClearDeedWeaponAttack();

        if (enemiesHit > 0)
            Report(new DeedWeaponAttackHitEvent(player, family, action.Ability, enemiesHit));
    }

    private void ClearDeedWeaponAttack()
    {
        pendingDeedWeaponAction = null;
        pendingDeedWeaponFamily = null;
        weaponAttackEnemies.Clear();
    }
}

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerStats), typeof(CharacterActionController))]
public sealed class PlayerWeaponCombo : MonoBehaviour
{
    private PlayerStats player;
    private CharacterActionController actions;

    private ItemData chainWeapon;
    private int nextStepIndex;
    private float nextAttackDeadline;

    private ActionContext pendingAction;
    private ItemData pendingWeapon;
    private WeaponFamilyDefinition pendingFamily;
    private WeaponComboStep pendingStep;
    private int pendingStepIndex;

    private void Awake()
    {
        player = GetComponent<PlayerStats>();
        actions = GetComponent<CharacterActionController>();
    }

    private void OnEnable()
    {
        actions.OnPhaseChanged += HandlePhaseChanged;
        actions.OnEffectsExecuted += HandleEffectsExecuted;
        actions.OnActionCancelled += HandleActionCancelled;
        player.OnDied += HandleDeath;
    }

    private void OnDisable()
    {
        actions.OnPhaseChanged -= HandlePhaseChanged;
        actions.OnEffectsExecuted -= HandleEffectsExecuted;
        actions.OnActionCancelled -= HandleActionCancelled;
        player.OnDied -= HandleDeath;

        ResetChain();
        ClearPendingAttack();
    }

    private void HandlePhaseChanged(ActionContext action, ActionPhase phase)
    {
        if (phase != ActionPhase.Casting &&
            phase != ActionPhase.Charging &&
            phase != ActionPhase.Executing)
        {
            return;
        }

        if (pendingAction != action)
            PrepareAttack(action);

        if (phase != ActionPhase.Executing || pendingWeapon == null)
            return;

        if (GetEquippedWeapon() != pendingWeapon)
        {
            ResetChain();
            ClearPendingAttack();
            return;
        }

        action.WeaponComboDamageBonus = pendingStep.WeaponDamageBonus;
    }

    private void PrepareAttack(ActionContext action)
    {
        ClearPendingAttack();
        pendingAction = action;

        AbilityData ability = action.Ability;

        if (action.Caster != player ||
            ability == null ||
            !ability.AdvancesWeaponCombo ||
            (ability.ExecutionSettings != null &&
             !ability.ExecutionSettings.ResolvesTargetHitImmediately))
        {
            return;
        }

        ItemData weapon = GetEquippedWeapon();
        WeaponFamilyDefinition family = weapon != null ? weapon.WeaponFamily : null;

        if (chainWeapon != weapon || Time.time > nextAttackDeadline)
            ResetChain();

        if (family == null || !family.TryGetComboStep(nextStepIndex, out WeaponComboStep step))
            return;

        pendingWeapon = weapon;
        pendingFamily = family;
        pendingStep = step;
        pendingStepIndex = nextStepIndex;
    }

    private void HandleEffectsExecuted(
        ActionExecutionContext action,
        AbilityEffectPipelineResult result)
    {
        if (pendingAction == null || pendingAction.ActionId != action.ActionId)
            return;

        ItemData weapon = pendingWeapon;
        WeaponFamilyDefinition family = pendingFamily;
        int stepIndex = pendingStepIndex;
        float bonus = pendingStep.WeaponDamageBonus;

        ClearPendingAttack();

        if (weapon == null || family == null)
            return;

        if (!HasConfirmedHit(result))
        {
            ResetChain();
            return;
        }

#if UNITY_EDITOR
        Debug.Log(
            $"[Weapon Combo] {family.name}, hit {stepIndex + 1}, " +
            $"+{bonus * 100f:0.#}% weapon damage",
            this);
#endif

        int followingStepIndex = stepIndex + 1;

        if (!family.TryGetComboStep(followingStepIndex, out WeaponComboStep followingStep))
        {
            ResetChain();
            return;
        }

        chainWeapon = weapon;
        nextStepIndex = followingStepIndex;
        nextAttackDeadline = action.ExecutedAt + followingStep.WindowFromPreviousHit;
    }

    private bool HasConfirmedHit(AbilityEffectPipelineResult result)
    {
        if (result == null || result.EffectExecutions == 0)
            return false;

        foreach (AbilityTargetHitResult hit in result.SuccessfulTargets)
        {
            if (hit.Outcome == AbilityTargetHitOutcome.Hit &&
                hit.Target != null &&
                hit.Target != player)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleActionCancelled(ActionContext action)
    {
        if (pendingAction != action)
            return;

        if (pendingWeapon != null)
            ResetChain();

        ClearPendingAttack();
    }

    private void HandleDeath(CharacterStats attacker)
    {
        ResetChain();
        ClearPendingAttack();
    }

    private ItemData GetEquippedWeapon()
    {
        EquipmentManager equipment = EquipmentManager.Instance;
        return equipment != null ? equipment.GetEquippedWeapon(player) : null;
    }

    public bool TryGetComboTimer(out float fraction, out int landedHits)
    {
        fraction = 0f;
        landedHits = 0;

        if (chainWeapon == null || GetEquippedWeapon() != chainWeapon)
            return false;

        WeaponFamilyDefinition family = chainWeapon.WeaponFamily;

        if (family == null ||
            !family.TryGetComboStep(nextStepIndex, out WeaponComboStep step))
        {
            return false;
        }

        bool nextAttackCommitted =
            pendingAction != null &&
            pendingWeapon == chainWeapon &&
            pendingFamily == family &&
            pendingStepIndex == nextStepIndex;

        float remainingSeconds = nextAttackDeadline - Time.time;
        float windowSeconds = step.WindowFromPreviousHit;

        if (nextAttackCommitted || remainingSeconds <= 0f || windowSeconds <= 0f)
            return false;

        fraction = Mathf.Clamp01(remainingSeconds / windowSeconds);
        landedHits = nextStepIndex;
        return true;
    }

    private void ResetChain()
    {
        chainWeapon = null;
        nextStepIndex = 0;
        nextAttackDeadline = 0f;
    }

    private void ClearPendingAttack()
    {
        pendingAction = null;
        pendingWeapon = null;
        pendingFamily = null;
        pendingStep = default;
        pendingStepIndex = 0;
    }
}

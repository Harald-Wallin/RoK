using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auktoritativ runtime-controller för det nya actionsystemet.
///
/// Ansvar:
/// - starta actions
/// - hantera targeting-preview
/// - bekräfta eller avbryta
/// - hantera cast och recovery
/// - betala kostnader
/// - starta cooldowns
/// - exekvera den migrerade abilityn
///
/// Själva targetinglogiken delegeras till TargetResolver.
/// Själva effect-execution delegeras till AbilityEffectPipeline.
/// </summary>
[RequireComponent(typeof(CharacterStats))]
public sealed class CharacterActionController :
    MonoBehaviour
{
    [Header("Cooldown")]

    [SerializeField]
    [Min(0f)]
    private float defaultGlobalCooldown = 0.8f;

    [Header("Targeting")]

    [SerializeField]
    [Min(1)]
    private int targetingBufferSize = 128;

    private readonly Dictionary<AbilityData, float>
        cooldownTimers =
            new();

    private CharacterStats stats;
    private CharacterStateController stateController;
    private WardSystem wardSystem;

    private TargetResolver targetResolver;

    private ActionContext currentContext;
    private ActionRequest currentRequest;

    private float abilityGlobalCooldownTimer;
    private float baseAttackGlobalCooldownTimer;
    private float phaseTimer;
    private float phaseDuration;
    private bool fullChargeReached;

    public ActionExecutionContext
    LastExecutionContext
    {
        get;
        private set;
    }

    public event Action<ActionExecutionContext>
        OnExecutionStarted;

    public event Action<
        ActionExecutionContext,
        AbilityEffectPipelineResult>
        OnEffectsExecuted;

    public ActionContext CurrentContext =>
        currentContext;

    public ActionPhase CurrentPhase =>
        currentContext != null
            ? currentContext.Phase
            : ActionPhase.Idle;

    public bool HasActiveAction =>
        currentContext != null;

    public bool IsPreviewing =>
        CurrentPhase == ActionPhase.Preview;

    public bool IsCasting =>
        CurrentPhase == ActionPhase.Casting;

    public bool IsCharging =>
    CurrentPhase ==
    ActionPhase.Charging;

    public bool IsExecuting =>
        CurrentPhase == ActionPhase.Executing;

    public bool IsRecovering =>
        CurrentPhase == ActionPhase.Recovery;

    public event Action<ActionContext>
        OnActionStarted;

    public event Action<ActionContext>
        OnPreviewStarted;

    public event Action<ActionContext>
        OnTargetingUpdated;

    public event Action<ActionContext, ActionPhase>
        OnPhaseChanged;

    public event Action<ActionContext>
        OnActionExecuted;

    public event Action<ActionContext>
        OnActionCancelled;

    public event Action<ActionContext>
        OnActionCompleted;

    /// <summary>
    /// Anropas exakt en gång när den aktiva actionen når
    /// 100 procent charge.
    ///
    /// Eventet innebär inte att actionen exekveras. Spelaren kan
    /// fortsätta hålla knappen nedtryckt tills release.
    /// </summary>
    public event Action<ActionContext>
        OnFullChargeReached;

    private void Awake()
    {
        stats =
            GetComponent<CharacterStats>();

        stateController =
            GetComponent<CharacterStateController>();

        wardSystem =
            GetComponent<WardSystem>();

        CreateTargetResolver();
    }

    private void OnValidate()
    {
        defaultGlobalCooldown =
            Mathf.Max(
                0f,
                defaultGlobalCooldown
            );

        targetingBufferSize =
            Mathf.Max(
                1,
                targetingBufferSize
            );
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateActiveAction();
    }

    private void CreateTargetResolver()
    {
        targetResolver =
            new TargetResolver(
                targetingBufferSize
            );
    }

    // =========================================================
    // ACTION ACTIVATION
    // =========================================================

    /// <summary>
    /// Försöker starta en migrerad ability.
    ///
    /// För Confirmed-actions startas Preview.
    /// För Immediate-actions går actionen direkt vidare till
    /// timing/execution om targetingen är giltig.
    /// </summary>

    // =========================================================
    // TARGETING QUERY
    // =========================================================

    /// <summary>
    /// Validerar hur en ability skulle targeta ett explicit target
    /// från karaktärens NUVARANDE position.
    ///
    /// Metoden:
    /// - startar ingen action
    /// - betalar ingen cost
    /// - startar ingen cooldown
    /// - ändrar ingen runtime action-state
    ///
    /// Den är främst avsedd för AI som behöver veta om dess
    /// nuvarande position faktiskt är användbar för en ability.
    /// </summary>
    public TargetingResult EvaluateTargeting(
        AbilityData ability,
        CharacterStats explicitTarget)
    {
        if (ability == null ||
            explicitTarget == null ||
            stats == null ||
            targetResolver == null)
        {
            return null;
        }

        GameObject targetObject =
            explicitTarget.gameObject;

        Vector2 aimPoint =
            TargetUtility.GetTargetPosition(
                targetObject
            );

        ActionRequest request =
            new ActionRequest(
                stats,
                ability,
                aimPoint,
                targetObject
            );

        return targetResolver.Resolve(
            request
        );
    }

    public bool TryStartAction(
        AbilityData ability,
        Vector2 requestedAimPoint,
        GameObject explicitTarget = null,
        Vector2 requestedDirection = default)
    {
        if (!CanStartAction(ability))
            return false;

        float initialChargeProgress =
            ability.IsChargeAbility
            ? 0f
            : 1f;

        ActionRequest request =
            CreateRuntimeRequest(
                ability,
                requestedAimPoint,
                explicitTarget,
                requestedDirection,
                initialChargeProgress
            );

        TargetingResult targeting =
            targetResolver.Resolve(request);

        ActionContext context =
            new ActionContext(request);

        context.UpdateTargeting(targeting);

        currentRequest = request;
        currentContext = context;

        OnActionStarted?.Invoke(
            currentContext
        );

        if (ability.RequiresConfirmation)
        {
            SetPhase(
                ActionPhase.Preview
            );

            OnPreviewStarted?.Invoke(
                currentContext
            );

            return true;
        }

        /*
 * Charge-actions måste få börja även om inget target ligger
 * inom den initiala, korta chargade räckvidden.
 *
 * Targetingen valideras igen kontinuerligt under charge och
 * slutligen vid release.
 */
        if (ability.IsChargeAbility)
        {
            BeginTimingOrExecution();
            return true;
        }

        if (!targeting.IsValid)
        {
            FailCurrentAction();
            return false;
        }

        BeginTimingOrExecution();

        return true;
    }

    /// <summary>
    /// Bekvämlighetsmetod för actions som riktas mot ett
    /// uttryckligt target.
    /// </summary>
    public bool TryStartAction(
        AbilityData ability,
        CharacterStats explicitTarget)
    {
        GameObject targetObject =
            explicitTarget != null
                ? explicitTarget.gameObject
                : null;

        Vector2 aimPoint =
            explicitTarget != null
                ? TargetUtility.GetTargetPosition(
                    explicitTarget.gameObject
                )
                : (Vector2)transform.position;

        return TryStartAction(
            ability,
            aimPoint,
            targetObject
        );
    }

    /// <summary>
    /// Bekvämlighetsmetod för self-cast eller abilities som inte
    /// behöver extern aim-data.
    /// </summary>
    public bool TryStartAction(
        AbilityData ability)
    {
        return TryStartAction(
            ability,
            transform.position,
            null,
            Vector2.down
        );
    }

    private bool CanStartAction(
     AbilityData ability)
    {
        if (ability == null)
            return false;

        if (!ability.UsesActionSettings)
            return false;

        if (HasActiveAction)
            return false;

        if (stats == null ||
            !stats.CanAct())
        {
            return false;
        }

        if (stateController != null &&
            !stateController.CanUseAbilities)
        {
            return false;
        }

        // =====================================================
        // COOLDOWN GROUP
        // =====================================================

        if (ability.IsBaseAttack)
        {
            if (baseAttackGlobalCooldownTimer >
                0f)
            {
                ShowAbilityOnCooldown();

                return false;
            }
        }
        else
        {
            if (abilityGlobalCooldownTimer >
                0f)
            {
                ShowAbilityOnCooldown();

                return false;
            }
        }

        /*
         * Individuell cooldown gäller framför allt
         * vanliga abilities.
         *
         * Base Attacks använder i stället sin gemensamma
         * AttackSpeed-baserade cooldown-group.
         */
        if (cooldownTimers.ContainsKey(
                ability))
        {
            ShowAbilityOnCooldown();

            return false;
        }

        AbilityTimingSettings timing =
            ability.TimingSettings;

        if (timing == null)
            return false;

        switch (timing.TimingType)
        {
            case ActionTimingType.Instant:
            case ActionTimingType.Cast:
                return true;

            case ActionTimingType.Charge:

                if (ability.ExecutionSettings ==
                    null)
                {
                    return false;
                }

                if (ability.ExecutionSettings
                        .ActivationMode !=
                    ActionActivationMode
                        .HoldAndRelease)
                {
                    Debug.LogWarning(
                        $"Charge-abilityn '{ability.abilityName}' " +
                        $"bör använda Activation Mode " +
                        $"'Hold And Release'.",
                        ability
                    );
                }

                return true;

            case ActionTimingType.Channel:

                Debug.LogWarning(
                    $"Ability '{ability.abilityName}' använder Channel, " +
                    $"vilket ännu inte exekveras av " +
                    $"{nameof(CharacterActionController)}.",
                    this
                );

                return false;

            default:
                return false;
        }
    }

    private float GetChargeRangeOverride(
    AbilityData ability,
    float chargeProgress)
    {
        if (ability == null ||
            ability.TargetingSettings == null ||
            ability.ChargeSettings == null ||
            !ability.ChargeSettings.ScalesRange)
        {
            return -1f;
        }

        float rangeMultiplier =
            ability.ChargeSettings
                .GetRangeMultiplier(
                    chargeProgress
                );

        return
            ability.TargetingSettings.Range *
            rangeMultiplier;
    }

    private ActionRequest CreateRuntimeRequest(
        AbilityData ability,
        Vector2 requestedAimPoint,
        GameObject explicitTarget,
        Vector2 requestedDirection,
        float chargeProgress)
    {
        return new ActionRequest(
            stats,
            ability,
            requestedAimPoint,
            explicitTarget,
            requestedDirection,
            GetChargeRangeOverride(
                ability,
                chargeProgress
            )
        );
    }

    // =========================================================
    // PREVIEW
    // =========================================================

    /// <summary>
    /// Uppdaterar den aktiva previewns aim och targetingresultat.
    ///
    /// ActionContext behåller den ursprungliga activation-requesten,
    /// medan currentRequest representerar previewns senaste input.
    /// </summary>
    public bool UpdatePreview(
        Vector2 requestedAimPoint,
        GameObject explicitTarget = null,
        Vector2 requestedDirection = default)
    {
        if (!IsPreviewing ||
            currentContext == null ||
            currentContext.Ability == null)
        {
            return false;
        }

        ActionRequest updatedRequest =
            new ActionRequest(
                stats,
                currentContext.Ability,
                requestedAimPoint,
                explicitTarget,
                requestedDirection
            );

        TargetingResult targeting =
            targetResolver.Resolve(
                updatedRequest
            );

        currentRequest =
            updatedRequest;

        currentContext.UpdateTargeting(
            targeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        return targeting.IsValid;
    }

    public bool UpdateChargeTargeting(
    Vector2 requestedAimPoint,
    GameObject explicitTarget = null,
    Vector2 requestedDirection = default)
    {
        if (!IsCharging ||
            currentContext == null ||
            currentContext.Ability == null ||
            stats == null)
        {
            return false;
        }

        ActionRequest updatedRequest =
            CreateRuntimeRequest(
                currentContext.Ability,
                requestedAimPoint,
                explicitTarget,
                requestedDirection,
                currentContext.ChargeProgress
            );

        TargetingResult targeting =
            targetResolver.Resolve(
                updatedRequest
            );

        currentRequest =
            updatedRequest;

        currentContext.UpdateTargeting(
            targeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        return targeting.IsValid;
    }

    private void RefreshChargeTargeting()
    {
        if (!IsCharging ||
            currentContext == null ||
            currentContext.Ability == null ||
            currentRequest == null)
        {
            return;
        }

        ActionRequest updatedRequest =
            CreateRuntimeRequest(
                currentContext.Ability,
                currentRequest.RequestedAimPoint,
                currentRequest.ExplicitTarget,
                currentRequest.RequestedDirection,
                currentContext.ChargeProgress
            );

        TargetingResult targeting =
            targetResolver.Resolve(
                updatedRequest
            );

        currentRequest =
            updatedRequest;

        currentContext.UpdateTargeting(
            targeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );
    }

    /// <summary>
    /// Bekräftar den aktiva targeting-previewn.
    ///
    /// Targetingen resolve:as en sista gång så att execution inte
    /// använder ett gammalt previewresultat.
    /// </summary>
    public bool ConfirmCurrentAction()
    {
        if (!IsPreviewing ||
            currentContext == null ||
            currentRequest == null)
        {
            return false;
        }

        TargetingResult targeting =
            targetResolver.Resolve(
                currentRequest
            );

        currentContext.UpdateTargeting(
            targeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        if (!targeting.IsValid)
            return false;

        BeginTimingOrExecution();

        return true;
    }

    public bool ReleaseCurrentCharge()
    {
        if (!IsCharging ||
            currentContext == null ||
            currentRequest == null)
        {
            return false;
        }

        float finalChargeProgress =
            GetNormalizedPhaseProgress();

        currentContext.ChargeProgress =
            finalChargeProgress;

        currentContext.NormalizedProgress =
            finalChargeProgress;

        /*
         * Skapa en sista request med exakt charge-progress från
         * release-framen.
         */
        ActionRequest releaseRequest =
            CreateRuntimeRequest(
                currentContext.Ability,
                currentRequest.RequestedAimPoint,
                currentRequest.ExplicitTarget,
                currentRequest.RequestedDirection,
                finalChargeProgress
            );

        TargetingResult targeting =
            targetResolver.Resolve(
                releaseRequest
            );

        currentRequest =
            releaseRequest;

        currentContext.UpdateTargeting(
            targeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        if (!targeting.IsValid)
        {
            FailCurrentAction();
            return false;
        }

        ExecuteCurrentAction();

        return true;
    }

    // =========================================================
    // TIMING
    // =========================================================

    private void BeginTimingOrExecution()
    {
        if (currentContext == null ||
            currentContext.Ability == null)
        {
            FailCurrentAction();
            return;
        }

        AbilityTimingSettings timing =
            currentContext
                .Ability
                .TimingSettings;

        switch (timing.TimingType)
        {
            case ActionTimingType.Instant:
                ExecuteCurrentAction();
                break;

            case ActionTimingType.Cast:
                BeginCast(
                    timing.CastDuration
                );
                break;

            case ActionTimingType.Charge:
                BeginCharge(
                    timing.MaximumChargeDuration
                );
                break;

            case ActionTimingType.Channel:
                Debug.LogWarning(
                    "Channel-execution är ännu inte implementerad " +
                    "i CharacterActionController.",
                    this
                );

                FailCurrentAction();
                break;
        }
    }

    private void BeginCharge(
    float maximumDuration)
    {
        if (currentContext == null)
        {
            FailCurrentAction();
            return;
        }

        float safeDuration =
            Mathf.Max(
                0.01f,
                maximumDuration
            );

        phaseTimer = 0f;
        phaseDuration = safeDuration;

        fullChargeReached =
            false;

        currentContext.NormalizedProgress =
            0f;

        currentContext.ChargeProgress =
            0f;

        SetPhase(
            ActionPhase.Charging
        );
    }

    private void BeginCast(
        float duration)
    {
        float safeDuration =
            Mathf.Max(
                0f,
                duration
            );

        if (safeDuration <= 0f)
        {
            ExecuteCurrentAction();
            return;
        }

        phaseTimer = 0f;
        phaseDuration = safeDuration;

        currentContext.NormalizedProgress = 0f;

        SetPhase(
            ActionPhase.Casting
        );
    }

    private void UpdateActiveAction()
    {
        if (currentContext == null)
            return;

        switch (currentContext.Phase)
        {
            case ActionPhase.Casting:
                UpdateCasting();
                break;

            case ActionPhase.Charging:
                UpdateCharging();
                break;

            case ActionPhase.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateCasting()
    {
        if (!CanContinueActiveAction())
        {
            InterruptCurrentAction();
            return;
        }

        phaseTimer += Time.deltaTime;

        currentContext.NormalizedProgress =
            GetNormalizedPhaseProgress();

        if (phaseTimer < phaseDuration)
            return;

        currentContext.NormalizedProgress = 1f;

        ExecuteCurrentAction();
    }

    private void UpdateCharging()
    {
        if (!CanContinueActiveAction())
        {
            InterruptCurrentAction();
            return;
        }

        phaseTimer +=
            Time.deltaTime;

        float previousProgress =
            currentContext
                .ChargeProgress;

        float progress =
            GetNormalizedPhaseProgress();

        currentContext.NormalizedProgress =
            progress;

        currentContext.ChargeProgress =
            progress;

        /*
         * Rangen måste uppdateras även när muspekaren inte rör sig.
         */
        RefreshChargeTargeting();

        /*
         * Eventet skickas exakt en gång när progress först når 1.
         *
         * previousProgress-kontrollen gör dessutom avsikten tydlig:
         * vi reagerar på övergången till full charge, inte varje frame
         * medan spelaren fortsätter hålla knappen.
         */
        if (!fullChargeReached &&
            previousProgress < 1f &&
            progress >= 1f)
        {
            fullChargeReached =
                true;

            OnFullChargeReached?.Invoke(
                currentContext
            );
        }
    }

    private void UpdateRecovery()
    {
        phaseTimer += Time.deltaTime;

        currentContext.NormalizedProgress =
            GetNormalizedPhaseProgress();

        if (phaseTimer < phaseDuration)
            return;

        currentContext.NormalizedProgress = 1f;

        CompleteCurrentAction();
    }

    private float GetNormalizedPhaseProgress()
    {
        if (phaseDuration <= 0f)
            return 1f;

        return Mathf.Clamp01(
            phaseTimer / phaseDuration
        );
    }

    private bool CanContinueActiveAction()
    {
        if (stats == null ||
            !stats.CanAct())
        {
            return false;
        }

        if (stateController != null &&
            !stateController.CanUseAbilities)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // EXECUTION
    // =========================================================

    private void ExecuteCurrentAction()
    {
        if (currentContext == null ||
            currentRequest == null)
        {
            FailCurrentAction();
            return;
        }

        TargetingResult finalTargeting =
            targetResolver.Resolve(
                currentRequest
            );

        currentContext.UpdateTargeting(
            finalTargeting
        );

        OnTargetingUpdated?.Invoke(
            currentContext
        );

        if (!finalTargeting.IsValid)
        {
            FailCurrentAction();
            return;
        }

        if (!TryPayWardCost(
                currentContext.Ability))
        {
            FailCurrentAction();
            return;
        }

        SetPhase(
            ActionPhase.Executing
        );

        currentContext.NormalizedProgress = 1f;

        ActionExecutionContext executionContext;

        try
        {
            executionContext =
                currentContext
                    .CreateExecutionContext();
        }
        catch (Exception exception)
        {
            Debug.LogException(
                exception,
                this
            );

            FailCurrentAction();
            return;
        }

        LastExecutionContext =
            executionContext;

        currentContext.MarkExecuted(
            executionContext.ExecutedAt
        );

        OnExecutionStarted?.Invoke(
            executionContext
        );

        AbilityEffectPipelineResult executionResult =
            currentContext
                .Ability
                .Execute(
                    executionContext
                );

        OnEffectsExecuted?.Invoke(
            executionContext,
            executionResult
        );

        StartCooldowns(
            currentContext.Ability
        );

        OnActionExecuted?.Invoke(
            currentContext
        );

        BeginRecovery();
    }

    private bool TryPayWardCost(
        AbilityData ability)
    {
        if (ability == null)
            return false;

        if (ability.wardCost <= 0)
            return true;

        if (wardSystem == null)
        {
            wardSystem =
                GetComponent<WardSystem>();
        }

        if (wardSystem == null)
            return false;

        if (wardSystem.TrySpendWard(
                ability.wardCost))
        {
            return true;
        }

        NotificationSpawner.Instance?.Show(
            NotificationSpawner
                .Instance
                .Database
                .notEnoughWard
        );

        return false;
    }

    // =========================================================
    // RECOVERY
    // =========================================================

    private void BeginRecovery()
    {
        if (currentContext == null ||
            currentContext.Ability == null)
        {
            CompleteCurrentAction();
            return;
        }

        float duration =
            currentContext
                .Ability
                .TimingSettings
                .RecoveryDuration;

        if (duration <= 0f)
        {
            CompleteCurrentAction();
            return;
        }

        phaseTimer = 0f;
        phaseDuration = duration;

        currentContext.NormalizedProgress = 0f;

        SetPhase(
            ActionPhase.Recovery
        );
    }

    // =========================================================
    // CANCELLATION
    // =========================================================


    /// <summary>
    /// Death interrupts the current action regardless
    /// of normal cancellation restrictions.
    ///
    /// Existing cooldowns are preserved.
    /// </summary>
    public void InterruptForDeath()
    {
        if (currentContext == null)
            return;

        ActionContext interrupted =
            currentContext;

        SetPhase(ActionPhase.Idle);

        ClearCurrentAction();

        OnActionCancelled?.Invoke(
            interrupted
        );
    }


    public bool CancelCurrentAction()
    {
        if (currentContext == null)
            return false;

        if (!CanCancelCurrentPhase())
            return false;

        ActionContext cancelledContext =
            currentContext;

        SetPhase(
            ActionPhase.Idle
        );

        ClearCurrentAction();

        OnActionCancelled?.Invoke(
            cancelledContext
        );

        return true;
    }

    private void InterruptCurrentAction()
    {
        if (currentContext == null)
            return;

        AbilityExecutionSettings execution =
            currentContext
                .Ability
                ?.ExecutionSettings;

        if (execution != null &&
            !execution.CanBeInterrupted)
        {
            return;
        }

        ActionContext cancelledContext =
            currentContext;

        SetPhase(
            ActionPhase.Idle
        );

        ClearCurrentAction();

        OnActionCancelled?.Invoke(
            cancelledContext
        );
    }

    private bool CanCancelCurrentPhase()
    {
        if (currentContext == null)
            return false;

        switch (currentContext.Phase)
        {
            case ActionPhase.Preview:
                return true;

            case ActionPhase.Casting:
            case ActionPhase.Charging:
                AbilityExecutionSettings execution =
                    currentContext
                        .Ability
                        ?.ExecutionSettings;

                return
                    execution == null ||
                    execution.CanBeCancelled;

            case ActionPhase.Executing:
            case ActionPhase.Recovery:
            case ActionPhase.Idle:
                return false;

            default:
                return false;
        }
    }

    private void FailCurrentAction()
    {
        if (currentContext == null)
            return;

        ActionContext failedContext =
            currentContext;

        if (currentContext.Phase !=
            ActionPhase.Idle)
        {
            SetPhase(
                ActionPhase.Idle
            );
        }

        ClearCurrentAction();

        OnActionCancelled?.Invoke(
            failedContext
        );
    }

    // =========================================================
    // COMPLETION
    // =========================================================

    private void CompleteCurrentAction()
    {
        if (currentContext == null)
            return;

        ActionContext completedContext =
            currentContext;

        SetPhase(
            ActionPhase.Idle
        );

        ClearCurrentAction();

        OnActionCompleted?.Invoke(
            completedContext
        );
    }

    private void ClearCurrentAction()
    {
        currentContext = null;
        currentRequest = null;

        phaseTimer = 0f;
        phaseDuration = 0f;

        fullChargeReached = false;
    }

    private void SetPhase(
        ActionPhase phase)
    {
        if (currentContext == null)
            return;

        if (currentContext.Phase == phase)
            return;

        currentContext.Phase = phase;

        OnPhaseChanged?.Invoke(
            currentContext,
            phase
        );
    }

    public void ResetRuntimeState()
    {
        ActionContext cancelledContext =
            currentContext;

        if (currentContext != null)
        {
            if (currentContext.Phase !=
                ActionPhase.Idle)
            {
                SetPhase(
                    ActionPhase.Idle
                );
            }

            ClearCurrentAction();

            OnActionCancelled?.Invoke(
                cancelledContext
            );
        }

        cooldownTimers.Clear();

        abilityGlobalCooldownTimer = 0f;
        baseAttackGlobalCooldownTimer = 0f;
        phaseTimer = 0f;
        phaseDuration = 0f;
        fullChargeReached = false;
        LastExecutionContext = null;
    }

    // =========================================================
    // COOLDOWNS
    // =========================================================

    /// <summary>
    /// Individuell cooldown.
    ///
    /// Base Attack använder AttackSpeed som sin cooldownlängd.
    ///
    /// Skillnaden är att Base Attack-cooldownen sedan appliceras
    /// på hela Base Attack-gruppen i stället för endast asseten.
    /// </summary>
    private float GetCooldownDuration(
        AbilityData ability)
    {
        if (ability == null)
            return 0f;

        if (!ability.IsBaseAttack)
        {
            return ability
                .EffectiveCooldown;
        }

        if (stats == null)
            return 0f;

        float attackSpeed =
            stats.GetStat(
                StatType.AttackSpeed
            );

        if (attackSpeed <= 0f)
        {
            attackSpeed =
                1f;
        }

        return
            1f /
            attackSpeed;
    }

    private void UpdateCooldowns()
    {
        if (abilityGlobalCooldownTimer >
            0f)
        {
            abilityGlobalCooldownTimer =
                Mathf.Max(
                    0f,
                    abilityGlobalCooldownTimer -
                    Time.deltaTime
                );
        }

        if (baseAttackGlobalCooldownTimer >
            0f)
        {
            baseAttackGlobalCooldownTimer =
                Mathf.Max(
                    0f,
                    baseAttackGlobalCooldownTimer -
                    Time.deltaTime
                );
        }

        if (cooldownTimers.Count == 0)
            return;

        AbilityData[] abilities =
            new AbilityData[
                cooldownTimers.Count
            ];

        cooldownTimers.Keys.CopyTo(
            abilities,
            0
        );

        for (int i = 0;
             i < abilities.Length;
             i++)
        {
            AbilityData ability =
                abilities[i];

            float remaining =
                cooldownTimers[
                    ability] -
                Time.deltaTime;

            if (remaining <= 0f)
            {
                cooldownTimers.Remove(
                    ability
                );

                continue;
            }

            cooldownTimers[
                ability] =
                remaining;
        }
    }

    private void StartCooldowns(
        AbilityData ability)
    {
        if (ability == null)
            return;

        // =====================================================
        // BASE ATTACK COOLDOWN GROUP
        // =====================================================

        if (ability.IsBaseAttack)
        {
            float baseAttackCooldown =
                GetCooldownDuration(
                    ability
                );

            if (baseAttackCooldown >
                0f)
            {
                baseAttackGlobalCooldownTimer =
                    Mathf.Max(
                        baseAttackGlobalCooldownTimer,
                        baseAttackCooldown
                    );
            }

            return;
        }

        // =====================================================
        // INDIVIDUAL ABILITY COOLDOWN
        // =====================================================

        float cooldown =
            GetCooldownDuration(
                ability
            );

        if (cooldown > 0f)
        {
            cooldownTimers[
                ability] =
                cooldown;
        }

        // =====================================================
        // NORMAL ABILITY GCD
        // =====================================================

        AbilityExecutionSettings execution =
            ability.ExecutionSettings;

        if (execution == null ||
            !execution.TriggersGlobalCooldown)
        {
            return;
        }

        float globalCooldown =
            execution.UsesGlobalCooldownOverride
                ? execution
                    .GlobalCooldownOverride
                : defaultGlobalCooldown;

        if (globalCooldown <= 0f)
            return;

        abilityGlobalCooldownTimer =
            Mathf.Max(
                abilityGlobalCooldownTimer,
                globalCooldown
            );
    }

    public float GetCooldownRemaining(
        AbilityData ability)
    {
        if (ability == null)
            return 0f;

        if (ability.IsBaseAttack)
        {
            return Mathf.Max(
                0f,
                baseAttackGlobalCooldownTimer
            );
        }

        float individualCooldown =
            0f;

        if (cooldownTimers.TryGetValue(
                ability,
                out float remaining))
        {
            individualCooldown =
                Mathf.Max(
                    0f,
                    remaining
                );
        }

        float globalCooldown =
            Mathf.Max(
                0f,
                abilityGlobalCooldownTimer
            );

        return Mathf.Max(
            individualCooldown,
            globalCooldown
        );
    }

    public float GetMaxCooldown(
        AbilityData ability)
    {
        if (ability == null)
            return 0f;

        if (ability.IsBaseAttack)
        {
            return GetCooldownDuration(
                ability
            );
        }

        float individualMaximum =
            cooldownTimers.ContainsKey(
                ability)
                ? GetCooldownDuration(
                    ability
                )
                : 0f;

        float globalMaximum =
            0f;

        AbilityExecutionSettings execution =
            ability.ExecutionSettings;

        if (execution != null &&
            execution.TriggersGlobalCooldown)
        {
            globalMaximum =
                execution
                    .UsesGlobalCooldownOverride
                    ? execution
                        .GlobalCooldownOverride
                    : defaultGlobalCooldown;
        }

        return Mathf.Max(
            individualMaximum,
            globalMaximum
        );
    }

    private static void ShowAbilityOnCooldown()
    {
        NotificationSpawner.Instance?.Show(
            NotificationSpawner
                .Instance
                .Database
                .abilityOnCooldown
        );
    }

    public float CurrentMovementMultiplier
    {
        get
        {
            if (currentContext == null ||
                currentContext.Ability == null ||
                currentContext.Ability.TimingSettings == null)
            {
                return 1f;
            }

            ActionMovementSettings movementSettings =
                currentContext
                    .Ability
                    .TimingSettings
                    .GetMovementSettings(
                        CurrentPhase
                    );

            if (movementSettings == null)
                return 1f;

            return movementSettings
                .SpeedMultiplier;
        }
    }

    public bool BlocksMovement =>
        CurrentMovementMultiplier <=
        0.0001f;
}

using UnityEngine;

public enum AbilityType
{
    Melee,
    Spell,
    Buff,
    Curse,
    Ranged
}

[CreateAssetMenu(menuName = "RPG/Ability")]
public class AbilityData :
    ScriptableObject,
    ITooltipProvider
{
    [Header("Basic")]

    public string abilityName;

    public string Id =>
    PersistentIdUtility
        .FromDisplayName(
            DisplayName
        );

    public string DisplayName =>
        string.IsNullOrWhiteSpace(
            abilityName
        )
            ? name
            : abilityName;

    [TextArea]
    public string description;

    public Sprite icon;

    [Header("Type")]

    public AbilityType types;

    [Header("Usage")]

    [SerializeField]
    private AbilityUsageType usageType =
        AbilityUsageType.Ability;

    [Header("Tags")]

    public AbilityTag[] tags;

    // =========================================================
    // NEW ACTION SYSTEM
    // =========================================================
    private bool useActionSettings = true;

    [Header("Target Preview")]

    [SerializeField]
    [Tooltip(
    "Bestämmer hur abilityns targeting visas visuellt. " +
    "Påverkar inte targeting, target selection eller execution."
)]
    private ActionPreviewMode previewMode =
    ActionPreviewMode.FullGeometry;

    public ActionPreviewMode PreviewMode =>
        previewMode;

    public bool ShowsTargetingPreview =>
        previewMode !=
        ActionPreviewMode.None;

    [SerializeField]
    private AbilityTargetingSettings targetingSettings =
        new();

    [SerializeField]
    private AbilityTimingSettings timingSettings =
        new();

    [SerializeField]
    private AbilityExecutionSettings executionSettings =
        new();

    [Header("Charge")]

    [SerializeField]
    private AbilityChargeSettings chargeSettings =
    new();

    public ChargeCompletionPreviewEffect
    ChargeCompletionPreviewEffect =>
        chargeCompletionPreviewEffect;

    [SerializeField]
    [Tooltip(
    "Visuell effekt som visas när abilityn når full charge. " +
    "Påverkar aldrig gameplay eller targeting.")]

    private ChargeCompletionPreviewEffect
    chargeCompletionPreviewEffect =
        new();

    // =========================================================
    // LEGACY CONFIGURATION
    // =========================================================

    [Header("Legacy Timing")]

    [Tooltip(
        "Används av det gamla AbilityController-systemet och " +
        "som fallback tills Use Action Settings aktiveras."
    )]
    public float cooldown = 5f;

    public float globalCooldown = 0.8f;
    public float castTime;

    [Header("Costs")]

    public int wardCost;

    [Header("Combat Rules")]

    public bool alwaysHits;
    public bool canCrit = true;
    public bool canMiss = true;
    public bool isSelfCast;
    public bool requiresHitCheck;
    public bool entersCombatState = true;

    [Header("Effects")]

    public AbilityEffect[] effects;

    // =========================================================
    // USAGE API
    // =========================================================

    public AbilityUsageType UsageType =>
        usageType;

    public bool IsBaseAttack =>
        usageType ==
        AbilityUsageType.BaseAttack;

    // =========================================================
    // ACTION SYSTEM API
    // =========================================================

    public bool UsesActionSettings =>
        useActionSettings;

    public AbilityEffect[] Effects =>
    effects;

    public AbilityTargetingSettings TargetingSettings =>
        targetingSettings;

    public AbilityTimingSettings TimingSettings =>
        timingSettings;

    public AbilityExecutionSettings ExecutionSettings =>
        executionSettings;

    public AbilityChargeSettings ChargeSettings =>
    chargeSettings;

    public float EffectiveCooldown =>
        useActionSettings
            ? executionSettings.Cooldown
            : Mathf.Max(0f, cooldown);

    public bool IsChargeAbility =>
    useActionSettings &&
    timingSettings != null &&
    timingSettings.TimingType ==
    ActionTimingType.Charge;

    public bool ChargeScalesDamage =>
    IsChargeAbility &&
    chargeSettings != null &&
    chargeSettings.ScalesDamage;

    public bool ChargeScalesRange =>
        IsChargeAbility &&
        chargeSettings != null &&
        chargeSettings.ScalesRange;

    public bool ChargeScalesDamageAndRange =>
        ChargeScalesDamage &&
        ChargeScalesRange;

    public bool UsesHoldAndRelease =>
        useActionSettings &&
        executionSettings != null &&
        executionSettings.ActivationMode ==
        ActionActivationMode.HoldAndRelease;

    public float EffectiveGlobalCooldown
    {
        get
        {
            if (!useActionSettings)
            {
                return Mathf.Max(
                    0f,
                    globalCooldown
                );
            }

            if (!executionSettings
                    .TriggersGlobalCooldown)
            {
                return 0f;
            }

            if (executionSettings
                .UsesGlobalCooldownOverride)
            {
                return executionSettings
                    .GlobalCooldownOverride;
            }

            return 0f;
        }
    }

    public float EffectiveCastTime
    {
        get
        {
            if (!useActionSettings)
            {
                return Mathf.Max(
                    0f,
                    castTime
                );
            }

            if (timingSettings.TimingType !=
                ActionTimingType.Cast)
            {
                return 0f;
            }

            return timingSettings.CastDuration;
        }
    }

    public bool EffectiveIsSelfCast =>
        useActionSettings
            ? targetingSettings.TargetingMode ==
              TargetingMode.Self
            : isSelfCast;

    public bool RequiresConfirmation =>
        useActionSettings &&
        executionSettings.ActivationMode ==
        ActionActivationMode.Confirmed;

    public bool ActivatesImmediately =>
        !useActionSettings ||
        executionSettings.ActivationMode ==
        ActionActivationMode.Immediate;

    // =========================================================
    // EXECUTION
    // =========================================================

    /// <summary>
    /// Ny auktoritativ execution-ingång för actionsystemet.
    /// </summary>
    public virtual AbilityEffectPipelineResult Execute(
        ActionExecutionContext context)
    {
        if (context == null)
        {
            return new AbilityEffectPipelineResult();
        }

        if (context.Ability != this)
        {
            Debug.LogError(
                $"Ability '{abilityName}' försökte exekvera en " +
                $"context som tillhör en annan ability.",
                this
            );

            return new AbilityEffectPipelineResult();
        }

        return AbilityEffectPipeline.Execute(
            context
        );
    }
    /// <summary>
    /// Legacy execution för äldre system.
    ///
    /// CharacterActionController använder inte längre denna metod.
    /// </summary>
    [System.Obsolete(
        "Använd Execute(ActionExecutionContext) genom " +
        "CharacterActionController."
    )]
    public virtual void Use(
        CharacterStats caster,
        CharacterStats target)
    {
        if (caster == null ||
            target == null)
        {
            return;
        }

        if (requiresHitCheck)
        {
            if (!CombatResolver.RollHit(
                    caster,
                    target))
            {
                DamageResult missResult =
                    new DamageResult
                    {
                        isMiss = true
                    };

                target.TakeDamage(
                    missResult,
                    caster
                );

                return;
            }

            if (CombatResolver.RollDodge(
                    caster,
                    target))
            {
                DamageResult evadeResult =
                    new DamageResult
                    {
                        isEvaded = true
                    };

                target.TakeDamage(
                    evadeResult,
                    caster
                );

                return;
            }
        }

        if (entersCombatState)
        {
            CharacterStateController casterState =
                caster.GetComponent<
                    CharacterStateController
                >();

            casterState?.NotifyCombatActivity();

            CharacterStateController targetState =
                target.GetComponent<
                    CharacterStateController
                >();

            targetState?.NotifyCombatActivity();
        }

        if (effects == null)
            return;

        foreach (AbilityEffect effect in effects)
        {
            if (effect == null)
                continue;

            effect.Apply(
                caster,
                target
            );
        }
    }

    // =========================================================
    // TOOLTIP
    // =========================================================

    public virtual TooltipData GetTooltipData(
        CharacterStats caster)
    {
        TooltipData data =
            new TooltipData
            {
                title = abilityName,

                subtitle =
                    IsBaseAttack
                        ? "Base Attack"
                        : types.ToString(),

                description = description
            };

        if (wardCost > 0)
        {
            data.stats.Add(
                $"<color=#7FD9FF>" +
                $"{wardCost} Wards" +
                $"</color>"
            );
        }

        if (effects != null)
        {
            foreach (AbilityEffect effect in effects)
            {
                if (effect == null)
                    continue;

                string text =
                    effect.GetTooltipText(
                        caster
                    );

                if (!string.IsNullOrEmpty(
                        text))
                {
                    data.stats.Add(
                        $"<color=#FF5555>" +
                        $"{text}" +
                        $"</color>"
                    );
                }
            }
        }

        AddTimingTooltip(
            data
        );

        AddCooldownTooltip(
            data,
            caster
        );

        return data;
    }

    private void AddTimingTooltip(
        TooltipData data)
    {
        if (!useActionSettings)
        {
            if (castTime > 0f)
            {
                data.stats.Add(
                    $"<color=white>" +
                    $"Cast Time: " +
                    $"{castTime:0.0}s" +
                    $"</color>"
                );
            }

            return;
        }

        switch (timingSettings.TimingType)
        {
            case ActionTimingType.Instant:
                break;

            case ActionTimingType.Cast:
                if (timingSettings
                        .CastDuration > 0f)
                {
                    data.stats.Add(
                        $"<color=white>" +
                        $"Cast Time: " +
                        $"{timingSettings.CastDuration:0.0}s" +
                        $"</color>"
                    );
                }

                break;

            case ActionTimingType.Channel:
                if (timingSettings
                        .ChannelDuration > 0f)
                {
                    data.stats.Add(
                        $"<color=white>" +
                        $"Channel Time: " +
                        $"{timingSettings.ChannelDuration:0.0}s" +
                        $"</color>"
                    );
                }

                break;

            case ActionTimingType.Charge:
                if (timingSettings
                        .MaximumChargeDuration > 0f)
                {
                    data.stats.Add(
                        $"<color=white>" +
                        $"Maximum Charge: " +
                        $"{timingSettings.MaximumChargeDuration:0.0}s" +
                        $"</color>"
                    );
                }

                break;
        }
    }

    private void AddCooldownTooltip(
        TooltipData data,
        CharacterStats caster)
    {
        if (IsBaseAttack)
        {
            if (caster == null)
                return;

            float attackSpeed =
                caster.GetStat(
                    StatType.AttackSpeed
                );

            if (attackSpeed <= 0f)
                attackSpeed = 1f;

            float attackCooldown =
                1f / attackSpeed;

            data.stats.Add(
                $"<color=white>" +
                $"Attack Speed: " +
                $"{attackCooldown:0.00}s" +
                $"</color>"
            );

            return;
        }

        float displayedCooldown =
            EffectiveCooldown;

        if (displayedCooldown <= 0f)
            return;

        data.stats.Add(
            $"<color=white>" +
            $"Cooldown: " +
            $"{displayedCooldown:0.#}s" +
            $"</color>"
        );
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        cooldown =
            Mathf.Max(
                0f,
                cooldown
            );

        globalCooldown =
            Mathf.Max(
                0f,
                globalCooldown
            );

        castTime =
            Mathf.Max(
                0f,
                castTime
            );

        wardCost =
            Mathf.Max(
                0,
                wardCost
            );

        targetingSettings ??=
            new AbilityTargetingSettings();

        timingSettings ??=
            new AbilityTimingSettings();

        executionSettings ??=
            new AbilityExecutionSettings();

        chargeSettings ??=
            new AbilityChargeSettings();

        chargeSettings.Validate(
            this
        );

        chargeCompletionPreviewEffect ??=
            new ChargeCompletionPreviewEffect();

        chargeCompletionPreviewEffect.Validate();
        timingSettings.Validate();
    }
#endif
}
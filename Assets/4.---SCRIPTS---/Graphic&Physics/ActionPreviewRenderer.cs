using UnityEngine;

/// <summary>
/// Auktoritativ preview-renderer för spelarens actionsystem.
///
/// Hanterar två typer av presentation:
///
/// PASSIVE BASE ATTACK PREVIEW
/// - visar den aktiva base attackens grundform
/// - visar cooldown/readiness
/// - visas endast när ingen aktiv action tar över
///
/// ACTIVE ACTION PREVIEW
/// - targeting preview
/// - charge preview
/// - valid / invalid state
///
/// Gameplay-geometrin ägs fortfarande av TargetResolver.
/// Denna klass visualiserar endast AbilityData / TargetingResult.
/// </summary>
[RequireComponent(
    typeof(CharacterActionController)
)]
[RequireComponent(
    typeof(CharacterStats)
)]
[RequireComponent(
    typeof(PlayerAbilityCollection)
)]
public sealed class ActionPreviewRenderer :
    MonoBehaviour
{
    // =========================================================
    // ACTIVE PREVIEW COLORS
    // =========================================================

    [Header("Active Preview Colors")]

    [SerializeField]
    private Color validFillColor =
        new Color(
            0.2f,
            0.8f,
            1f,
            0.22f
        );

    [SerializeField]
    private Color validOutlineColor =
        new Color(
            0.35f,
            0.9f,
            1f,
            0.95f
        );

    [SerializeField]
    private Color invalidFillColor =
        new Color(
            1f,
            0.15f,
            0.15f,
            0.22f
        );

    [SerializeField]
    private Color invalidOutlineColor =
        new Color(
            1f,
            0.25f,
            0.25f,
            0.95f
        );

    // =========================================================
    // CHARGE COLORS
    // =========================================================

    [Header("Charge Preview Colors")]

    [SerializeField]
    [Tooltip(
        "Outline som visar abilityns maximala form eller " +
        "chargeområde."
    )]
    private Color chargeMaximumOutlineColor =
        new Color(
            0.7f,
            0.7f,
            0.7f,
            0.65f
        );

    [SerializeField]
    [Tooltip(
        "Fill som visar aktuell charge."
    )]
    private Color chargeFillColor =
        new Color(
            0.2f,
            0.8f,
            1f,
            0.35f
        );

    [SerializeField]
    [Tooltip(
        "Fill när aktuell charge-targeting är ogiltig."
    )]
    private Color invalidChargeFillColor =
        new Color(
            1f,
            0.15f,
            0.15f,
            0.35f
        );

    // =========================================================
    // PASSIVE BASE ATTACK
    // =========================================================

    [Header("Passive Base Attack")]

    [SerializeField]
    [Tooltip(
        "Om den passiva base attack-formen ska visas " +
        "även utanför combat."
    )]
    private bool showBaseAttackOutsideCombat =
        true;

    [SerializeField]
    [Tooltip(
        "Om abilityns Preview Mode ska påverka den " +
        "passiva base attack-previewn."
    )]
    private bool respectBaseAttackPreviewMode =
        true;

    [SerializeField]
    [Tooltip(
        "Tillåter readiness-fyllnaden även när " +
        "Preview Mode är None."
    )]
    private bool showBaseAttackReadinessWhenPreviewModeIsNone =
        true;

    [SerializeField]
    [Tooltip(
        "Döljer readiness-fyllnaden när attacken är " +
        "helt redo."
    )]
    private bool hideReadinessFillWhenReady;

    [Header("Passive Base Attack Colors")]

    [SerializeField]
    private Color passiveBaseAttackFillColor =
        new Color(
            0.25f,
            0.8f,
            1f,
            0.07f
        );

    [SerializeField]
    private Color passiveBaseAttackOutlineColor =
        new Color(
            0.35f,
            0.9f,
            1f,
            0.22f
        );

    [SerializeField]
    private Color combatBaseAttackFillColor =
        new Color(
            0.25f,
            0.8f,
            1f,
            0.12f
        );

    [SerializeField]
    private Color combatBaseAttackOutlineColor =
        new Color(
            0.35f,
            0.9f,
            1f,
            0.38f
        );

    [SerializeField]
    private Color readinessFillColor =
        new Color(
            1f,
            1f,
            1f,
            0.16f
        );

    // =========================================================
    // SHAPE QUALITY
    // =========================================================

    [Header("Shape Quality")]

    [SerializeField]
    [Range(12, 128)]
    private int circleSegments =
        48;

    [SerializeField]
    [Range(4, 128)]
    private int coneSegments =
        32;

    [SerializeField]
    [Min(0.01f)]
    private float pointRadius =
        0.15f;

    [SerializeField]
    [Min(0.01f)]
    private float selfRadius =
        0.5f;

    [SerializeField]
    [Min(0.001f)]
    private float outlineWidth =
        0.04f;

    // =========================================================
    // RENDERING
    // =========================================================

    [Header("Rendering")]

    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder =
        100;

    // =========================================================
    // REFERENCES
    // =========================================================

    private CharacterStats stats;

    private CharacterStateController
        stateController;

    private CharacterActionController
        actionController;

    private PlayerAbilityCollection
        abilityCollection;

    private Camera worldCamera;

    // =========================================================
    // ACTIVE RENDERERS
    // =========================================================

    private GameObject
        activePreviewRoot;

    private TargetShapeRenderer
        activeOutlineRenderer;

    private TargetShapeRenderer
        activeFillRenderer;

    // =========================================================
    // PASSIVE RENDERERS
    // =========================================================

    private GameObject
        passivePreviewRoot;

    private TargetShapeRenderer
        passiveBaseRenderer;

    private TargetShapeRenderer
        passiveReadinessRenderer;

    // =========================================================
    // RUNTIME
    // =========================================================

    private bool activeChargeIsFull;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        ResolveReferences();

        /*
         * NPC:s använder samma AbilityData och targeting,
         * men behöver ingen lokal spelar-preview.
         */
        if (!(stats is PlayerStats))
        {
            enabled =
                false;

            return;
        }

        CreateRenderingObjects();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (!(stats is PlayerStats))
            return;

        Subscribe();

        RefreshPresentation();
    }

    private void OnDisable()
    {
        Unsubscribe();

        ClearActivePreview();
        ClearPassivePreview();
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (activePreviewRoot != null)
        {
            Destroy(
                activePreviewRoot
            );
        }

        if (passivePreviewRoot != null)
        {
            Destroy(
                passivePreviewRoot
            );
        }
    }

    private void OnValidate()
    {
        circleSegments =
            Mathf.Clamp(
                circleSegments,
                12,
                128
            );

        coneSegments =
            Mathf.Clamp(
                coneSegments,
                4,
                128
            );

        pointRadius =
            Mathf.Max(
                0.01f,
                pointRadius
            );

        selfRadius =
            Mathf.Max(
                0.01f,
                selfRadius
            );

        outlineWidth =
            Mathf.Max(
                0.001f,
                outlineWidth
            );
    }

    private void LateUpdate()
    {
        if (!(stats is PlayerStats))
            return;

        /*
         * Full-charge shake behöver uppdateras även när
         * TargetingResult i sig inte förändras.
         */
        if (activeChargeIsFull &&
            actionController != null &&
            actionController.IsCharging)
        {
            ActionContext context =
                actionController
                    .CurrentContext;

            if (context != null)
            {
                RenderActivePreview(
                    context
                );
            }

            ClearPassivePreview();

            return;
        }

        /*
         * Passiv base attack-preview följer musens riktning
         * kontinuerligt.
         */
        if (actionController == null ||
            !actionController.HasActiveAction)
        {
            RenderPassiveBaseAttack();
        }
        else
        {
            ClearPassivePreview();
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void ResolveReferences()
    {
        if (stats == null)
        {
            stats =
                GetComponent<
                    CharacterStats>();
        }

        if (stateController == null)
        {
            stateController =
                GetComponent<
                    CharacterStateController>();
        }

        if (actionController == null)
        {
            actionController =
                GetComponent<
                    CharacterActionController>();
        }

        if (abilityCollection == null)
        {
            abilityCollection =
                GetComponent<
                    PlayerAbilityCollection>();
        }

        if (worldCamera == null)
        {
            worldCamera =
                Camera.main;
        }
    }

    // =========================================================
    // EVENTS
    // =========================================================

    private void Subscribe()
    {
        if (actionController == null)
            return;

        actionController.OnPreviewStarted -=
            HandlePreviewStarted;

        actionController.OnTargetingUpdated -=
            HandleTargetingUpdated;

        actionController.OnPhaseChanged -=
            HandlePhaseChanged;

        actionController.OnActionCancelled -=
            HandleActionEnded;

        actionController.OnActionCompleted -=
            HandleActionEnded;

        actionController.OnFullChargeReached -=
            HandleFullChargeReached;

        actionController.OnPreviewStarted +=
            HandlePreviewStarted;

        actionController.OnTargetingUpdated +=
            HandleTargetingUpdated;

        actionController.OnPhaseChanged +=
            HandlePhaseChanged;

        actionController.OnActionCancelled +=
            HandleActionEnded;

        actionController.OnActionCompleted +=
            HandleActionEnded;

        actionController.OnFullChargeReached +=
            HandleFullChargeReached;
    }

    private void Unsubscribe()
    {
        if (actionController == null)
            return;

        actionController.OnPreviewStarted -=
            HandlePreviewStarted;

        actionController.OnTargetingUpdated -=
            HandleTargetingUpdated;

        actionController.OnPhaseChanged -=
            HandlePhaseChanged;

        actionController.OnActionCancelled -=
            HandleActionEnded;

        actionController.OnActionCompleted -=
            HandleActionEnded;

        actionController.OnFullChargeReached -=
            HandleFullChargeReached;
    }

    // =========================================================
    // CREATION
    // =========================================================

    private void CreateRenderingObjects()
    {
        CreateActiveRenderers();
        CreatePassiveRenderers();
    }

    private void CreateActiveRenderers()
    {
        activePreviewRoot =
            new GameObject(
                "Active Action Preview Runtime"
            );

        activePreviewRoot.transform.SetParent(
            transform,
            false
        );

        GameObject outlineObject =
            new GameObject(
                "Active Outline"
            );

        outlineObject.transform.SetParent(
            activePreviewRoot.transform,
            false
        );

        activeOutlineRenderer =
            outlineObject.AddComponent<
                TargetShapeRenderer>();

        activeOutlineRenderer.Initialize(
            sortingLayerName,
            sortingOrder,
            circleSegments,
            coneSegments,
            pointRadius,
            selfRadius,
            outlineWidth,
            renderOutline: true
        );

        GameObject fillObject =
            new GameObject(
                "Active Fill"
            );

        fillObject.transform.SetParent(
            activePreviewRoot.transform,
            false
        );

        activeFillRenderer =
            fillObject.AddComponent<
                TargetShapeRenderer>();

        activeFillRenderer.Initialize(
            sortingLayerName,
            sortingOrder - 1,
            circleSegments,
            coneSegments,
            pointRadius,
            selfRadius,
            outlineWidth,
            renderOutline: false
        );

        activePreviewRoot.SetActive(
            false
        );
    }

    private void CreatePassiveRenderers()
    {
        passivePreviewRoot =
            new GameObject(
                "Passive Base Attack Preview Runtime"
            );

        passivePreviewRoot.transform.SetParent(
            transform,
            false
        );

        GameObject baseObject =
            new GameObject(
                "Passive Base Shape"
            );

        baseObject.transform.SetParent(
            passivePreviewRoot.transform,
            false
        );

        passiveBaseRenderer =
            baseObject.AddComponent<
                TargetShapeRenderer>();

        passiveBaseRenderer.Initialize(
            sortingLayerName,
            sortingOrder - 10,
            circleSegments,
            coneSegments,
            pointRadius,
            selfRadius,
            outlineWidth,
            renderOutline: true
        );

        GameObject readinessObject =
            new GameObject(
                "Passive Readiness Fill"
            );

        readinessObject.transform.SetParent(
            passivePreviewRoot.transform,
            false
        );

        passiveReadinessRenderer =
            readinessObject.AddComponent<
                TargetShapeRenderer>();

        passiveReadinessRenderer.Initialize(
            sortingLayerName,
            sortingOrder - 9,
            circleSegments,
            coneSegments,
            pointRadius,
            selfRadius,
            outlineWidth,
            renderOutline: false
        );

        passivePreviewRoot.SetActive(
            false
        );
    }

    // =========================================================
    // ACTIVE EVENTS
    // =========================================================

    private void HandlePreviewStarted(
        ActionContext context)
    {
        ClearPassivePreview();

        RenderActivePreview(
            context
        );
    }

    private void HandleTargetingUpdated(
        ActionContext context)
    {
        if (!ShouldRenderActiveContext(
                context))
        {
            ClearActivePreview();

            return;
        }

        ClearPassivePreview();

        RenderActivePreview(
            context
        );
    }

    private void HandlePhaseChanged(
        ActionContext context,
        ActionPhase phase)
    {
        if (phase ==
                ActionPhase.Preview ||
            phase ==
                ActionPhase.Charging)
        {
            if (phase ==
                    ActionPhase.Charging &&
                context != null &&
                context.ChargeProgress < 1f)
            {
                activeChargeIsFull =
                    false;
            }

            ClearPassivePreview();

            RenderActivePreview(
                context
            );

            return;
        }

        activeChargeIsFull =
            false;

        ClearActivePreview();

        /*
         * Passive preview återkommer automatiskt i
         * LateUpdate när hela actionen är klar.
         */
    }

    private void HandleActionEnded(
        ActionContext context)
    {
        activeChargeIsFull =
            false;

        ClearActivePreview();

        RenderPassiveBaseAttack();
    }

    private void HandleFullChargeReached(
        ActionContext context)
    {
        if (context == null ||
            actionController == null ||
            context !=
                actionController.CurrentContext)
        {
            return;
        }

        activeChargeIsFull =
            true;

        RenderActivePreview(
            context
        );
    }

    // =========================================================
    // ACTIVE PREVIEW
    // =========================================================

    private static bool
        ShouldRenderActiveContext(
            ActionContext context)
    {
        if (context == null)
            return false;

        return
            context.Phase ==
                ActionPhase.Preview ||
            context.Phase ==
                ActionPhase.Charging;
    }

    private void RenderActivePreview(
        ActionContext context)
    {
        if (activeOutlineRenderer == null ||
            activeFillRenderer == null ||
            activePreviewRoot == null)
        {
            return;
        }

        AbilityData ability =
            context?.Ability;

        if (ability == null)
        {
            ClearActivePreview();

            return;
        }

        TargetingResult targeting =
            context.Targeting;

        if (targeting == null ||
            targeting.Settings == null)
        {
            ClearActivePreview();

            return;
        }

        activePreviewRoot.SetActive(
            true
        );

        switch (ability.PreviewMode)
        {
            case ActionPreviewMode.None:

                ClearActivePreview();

                break;

            case ActionPreviewMode.FullGeometry:

                RenderFullGeometry(
                    targeting
                );

                break;

            case ActionPreviewMode.ChargeIndicator:

                RenderChargeIndicator(
                    context,
                    targeting
                );

                break;

            case ActionPreviewMode.ChargeFilledGeometry:

                RenderChargeFilledGeometry(
                    context,
                    targeting
                );

                break;

            default:

                ClearActivePreview();

                break;
        }
    }

    private void RenderFullGeometry(
        TargetingResult targeting)
    {
        activeFillRenderer.Clear();

        Color fillColor =
            targeting.IsValid
                ? validFillColor
                : invalidFillColor;

        Color outlineColor =
            targeting.IsValid
                ? validOutlineColor
                : invalidOutlineColor;

        activeOutlineRenderer.Render(
            targeting,
            fillColor,
            outlineColor
        );
    }

    private void RenderChargeIndicator(
        ActionContext context,
        TargetingResult targeting)
    {
        activeFillRenderer.Clear();

        Color outlineColor =
            targeting.IsValid
                ? validOutlineColor
                : invalidOutlineColor;

        Vector2 terminalOffset =
            GetFullChargeTerminalOffset(
                context,
                targeting
            );

        activeOutlineRenderer.Render(
            targeting,
            Color.clear,
            outlineColor,
            1f,
            terminalOffset
        );
    }

    private void RenderChargeFilledGeometry(
        ActionContext context,
        TargetingResult targeting)
    {
        AbilityData ability =
            context.Ability;

        AbilityChargeSettings
            chargeSettings =
                ability.ChargeSettings;

        if (chargeSettings == null)
        {
            RenderFullGeometry(
                targeting
            );

            return;
        }

        float chargeProgress =
            Mathf.Clamp01(
                context.ChargeProgress
            );

        bool scalesDamage =
            chargeSettings.ScalesDamage;

        bool scalesRange =
            chargeSettings.ScalesRange;

        Color currentFillColor =
            targeting.IsValid
                ? chargeFillColor
                : invalidChargeFillColor;

        Vector2 terminalOffset =
            GetFullChargeTerminalOffset(
                context,
                targeting
            );

        /*
         * DAMAGE + RANGE
         *
         * TargetResolver har redan byggt aktuell storlek.
         */
        if (scalesDamage &&
            scalesRange)
        {
            activeOutlineRenderer.Clear();

            activeFillRenderer.Render(
                targeting,
                currentFillColor,
                Color.clear,
                1f,
                terminalOffset
            );

            return;
        }

        /*
         * RANGE ONLY
         */
        if (scalesRange)
        {
            activeOutlineRenderer.Clear();

            activeFillRenderer.Render(
                targeting,
                currentFillColor,
                Color.clear,
                1f,
                terminalOffset
            );

            return;
        }

        /*
         * DAMAGE ONLY
         *
         * Full targetingform + växande fill.
         */
        if (scalesDamage)
        {
            activeOutlineRenderer.Render(
                targeting,
                Color.clear,
                chargeMaximumOutlineColor
            );

            activeFillRenderer.Render(
                targeting,
                currentFillColor,
                Color.clear,
                chargeProgress,
                terminalOffset
            );

            return;
        }

        /*
         * Charge utan damage/range scaling.
         */
        activeOutlineRenderer.Render(
            targeting,
            Color.clear,
            chargeMaximumOutlineColor
        );

        activeFillRenderer.Render(
            targeting,
            currentFillColor,
            Color.clear,
            chargeProgress,
            terminalOffset
        );
    }

    // =========================================================
    // PASSIVE BASE ATTACK PREVIEW
    // =========================================================

    private void RenderPassiveBaseAttack()
    {
        ResolveReferences();

        if (!CanRenderPassivePreview())
        {
            ClearPassivePreview();

            return;
        }

        AbilityData attack =
            abilityCollection
                .GetActiveBaseAttack();

        if (!CanDisplayPassiveBaseAttack(
                attack))
        {
            ClearPassivePreview();

            return;
        }

        bool inCombat =
            stateController != null &&
            stateController.InCombat;

        if (!showBaseAttackOutsideCombat &&
            !inCombat)
        {
            ClearPassivePreview();

            return;
        }

        AbilityTargetingSettings settings =
            attack.TargetingSettings;

        bool previewModeIsNone =
            attack.PreviewMode ==
            ActionPreviewMode.None;

        bool showBaseShape =
            !respectBaseAttackPreviewMode ||
            !previewModeIsNone;

        bool showReadiness =
            !previewModeIsNone ||
            showBaseAttackReadinessWhenPreviewModeIsNone;

        Vector2 origin =
            TargetUtility.GetTargetPosition(
                stats.gameObject
            );

        Vector2 direction =
            GetPassiveAimDirection();

        float range =
            Mathf.Max(
                0f,
                settings.Range
            );

        Vector2 targetPoint =
            ResolvePassiveTargetPoint(
                settings,
                origin,
                direction,
                range
            );

        float distance =
            Vector2.Distance(
                origin,
                targetPoint
            );

        float readiness =
            GetAbilityReadiness(
                attack
            );

        bool renderedAnything =
            false;

        if (showBaseShape)
        {
            renderedAnything |=
                RenderPassiveBaseShape(
                    settings,
                    origin,
                    targetPoint,
                    direction,
                    distance,
                    inCombat
                );
        }
        else
        {
            passiveBaseRenderer
                ?.Clear();
        }

        if (showReadiness)
        {
            renderedAnything |=
                RenderPassiveReadiness(
                    settings,
                    origin,
                    targetPoint,
                    direction,
                    distance,
                    readiness
                );
        }
        else
        {
            passiveReadinessRenderer
                ?.Clear();
        }

        passivePreviewRoot
            .SetActive(
                renderedAnything
            );
    }

    private bool CanRenderPassivePreview()
    {
        if (!(stats is PlayerStats))
            return false;

        if (abilityCollection == null ||
            actionController == null ||
            passivePreviewRoot == null ||
            passiveBaseRenderer == null ||
            passiveReadinessRenderer == null)
        {
            return false;
        }

        /*
         * Aktiv action äger presentationen helt.
         */
        if (actionController.HasActiveAction)
            return false;

        return true;
    }

    private bool RenderPassiveBaseShape(
        AbilityTargetingSettings settings,
        Vector2 origin,
        Vector2 targetPoint,
        Vector2 direction,
        float distance,
        bool inCombat)
    {
        Color fillColor =
            inCombat
                ? combatBaseAttackFillColor
                : passiveBaseAttackFillColor;

        Color outlineColor =
            inCombat
                ? combatBaseAttackOutlineColor
                : passiveBaseAttackOutlineColor;

        return passiveBaseRenderer.Render(
            settings,
            origin,
            targetPoint,
            direction,
            distance,
            null,
            fillColor,
            outlineColor,
            shapeScale: 1f,
            rangeOverride: settings.Range
        );
    }

    private bool RenderPassiveReadiness(
        AbilityTargetingSettings settings,
        Vector2 origin,
        Vector2 targetPoint,
        Vector2 direction,
        float distance,
        float readiness)
    {
        bool shouldShow =
            readiness > 0f &&
            (
                !hideReadinessFillWhenReady ||
                readiness < 0.999f
            );

        if (!shouldShow)
        {
            passiveReadinessRenderer
                .Clear();

            return false;
        }

        return passiveReadinessRenderer.Render(
            settings,
            origin,
            targetPoint,
            direction,
            distance,
            null,
            readinessFillColor,
            Color.clear,
            shapeScale: readiness,
            rangeOverride: settings.Range
        );
    }

    private float GetAbilityReadiness(
        AbilityData ability)
    {
        if (ability == null ||
            actionController == null)
        {
            return 0f;
        }

        float remaining =
            actionController
                .GetCooldownRemaining(
                    ability
                );

        /*
         * Ingen cooldown kvar = helt redo.
         *
         * Detta specialfall behövs eftersom
         * GetMaxCooldown(baseAttack) får returnera 0 när
         * ingen aktiv cooldown längre finns.
         */
        if (remaining <= 0f)
            return 1f;

        float maximum =
            actionController
                .GetMaxCooldown(
                    ability
                );

        if (maximum <= 0f)
            return 1f;

        return Mathf.Clamp01(
            1f -
            remaining /
            maximum
        );
    }

    private Vector2 GetPassiveAimDirection()
    {
        ResolveReferences();

        Vector2 origin =
            TargetUtility.GetTargetPosition(
                stats.gameObject
            );

        if (worldCamera != null)
        {
            Vector3 mouseWorld =
                worldCamera
                    .ScreenToWorldPoint(
                        Input.mousePosition
                    );

            Vector2 direction =
                new Vector2(
                    mouseWorld.x,
                    mouseWorld.y
                ) -
                origin;

            if (direction.sqrMagnitude >
                0.0001f)
            {
                return
                    direction.normalized;
            }
        }

        /*
         * Fallback om kameran saknas eller musen råkar
         * ligga exakt över player-origin.
         */
        return Vector2.down;
    }

    private static Vector2
        ResolvePassiveTargetPoint(
            AbilityTargetingSettings settings,
            Vector2 origin,
            Vector2 direction,
            float range)
    {
        direction =
            GetSafeDirection(
                direction
            );

        if (settings == null)
            return origin;

        if (settings.TargetingMode ==
            TargetingMode.Self)
        {
            return origin;
        }

        /*
         * Passiv preview visar abilityns fulla
         * konfigurerade räckvidd.
         */
        return
            origin +
            direction *
            Mathf.Max(
                0f,
                range
            );
    }

    private static bool
        CanDisplayPassiveBaseAttack(
            AbilityData attack)
    {
        if (attack == null)
            return false;

        if (!attack.IsBaseAttack)
            return false;

        if (!attack.UsesActionSettings)
            return false;

        if (attack.TargetingSettings == null)
            return false;

        return SupportsPermanentPreview(
            attack.TargetingSettings
                .TargetingMode
        );
    }

    private static bool SupportsPermanentPreview(
        TargetingMode targetingMode)
    {
        return
            targetingMode ==
                TargetingMode.Cone ||

            targetingMode ==
                TargetingMode.Circle ||

            targetingMode ==
                TargetingMode.Self ||

            targetingMode ==
                TargetingMode.Line;
    }

    // =========================================================
    // CHARGE TERMINAL EFFECT
    // =========================================================

    private Vector2 GetFullChargeTerminalOffset(
        ActionContext context,
        TargetingResult targeting)
    {
        if (!activeChargeIsFull ||
            context == null ||
            targeting == null ||
            context.Phase !=
                ActionPhase.Charging ||
            targeting.Settings.TargetingMode !=
                TargetingMode.Line)
        {
            return Vector2.zero;
        }

        ChargeCompletionPreviewEffect effect =
            context
                .Ability
                ?.ChargeCompletionPreviewEffect;

        if (effect == null ||
            !effect.IsEnabled)
        {
            return Vector2.zero;
        }

        switch (effect.EffectType)
        {
            case ChargeCompletionPreviewEffectType
                .LineEndShake:

                return effect
                    .GetTerminalOffset(
                        targeting.Direction,
                        Time.unscaledTime
                    );

            case ChargeCompletionPreviewEffectType.None:
            default:

                return Vector2.zero;
        }
    }

    // =========================================================
    // CLEAR
    // =========================================================

    private void RefreshPresentation()
    {
        if (actionController != null &&
            ShouldRenderActiveContext(
                actionController.CurrentContext))
        {
            ClearPassivePreview();

            RenderActivePreview(
                actionController
                    .CurrentContext
            );

            return;
        }

        ClearActivePreview();

        RenderPassiveBaseAttack();
    }

    private void ClearActivePreview()
    {
        activeChargeIsFull =
            false;

        activeOutlineRenderer
            ?.Clear();

        activeFillRenderer
            ?.Clear();

        if (activePreviewRoot != null)
        {
            activePreviewRoot
                .SetActive(
                    false
                );
        }
    }

    private void ClearPassivePreview()
    {
        passiveBaseRenderer
            ?.Clear();

        passiveReadinessRenderer
            ?.Clear();

        if (passivePreviewRoot != null)
        {
            passivePreviewRoot
                .SetActive(
                    false
                );
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static Vector2 GetSafeDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.0001f)
        {
            return Vector2.down;
        }

        return direction.normalized;
    }
}
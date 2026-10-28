using UnityEngine;

/// <summary>
/// Koordinerar hela targeting-pipelinen.
///
/// ActionRequest
///     ↓
/// Förbered runtime-data
///     ↓
/// TargetGeometry
///     ↓
/// TargetValidator
///     ↓
/// TargetSelector
///     ↓
/// TargetingResult
///
/// Klassen innehåller ingen egen faction-, fysik- eller
/// urvalslogik. Den samordnar de specialiserade systemen.
/// </summary>
public sealed class TargetResolver
{
    private const float DirectionEpsilon = 0.0001f;

    private readonly TargetGeometry geometry;
    private readonly TargetSelector selector;

    public TargetResolver(
        int geometryBufferSize = 128)
    {
        geometry =
            new TargetGeometry(
                geometryBufferSize
            );

        selector =
            new TargetSelector();
    }

    /// <summary>
    /// Alternativ konstruktor med explicit random-seed.
    ///
    /// Användbart senare för tester, AI eller deterministisk
    /// simulation.
    /// </summary>
    public TargetResolver(
        int geometryBufferSize,
        int randomSeed)
    {
        geometry =
            new TargetGeometry(
                geometryBufferSize
            );

        selector =
            new TargetSelector(
                randomSeed
            );
    }

    /// <summary>
    /// Skapar ett komplett targetingresultat för requesten.
    ///
    /// Resultatet innehåller alltid en FailureReason när
    /// targetingen inte kan utföras.
    /// </summary>
    public TargetingResult Resolve(
        ActionRequest request)
    {
        TargetingResult result =
            new TargetingResult();

        if (!TryPrepareResult(
                request,
                result))
        {
            return result;
        }

        TargetingFailureReason pointFailure =
            ValidateTargetingPoint(
                request,
                result
            );

        if (pointFailure !=
            TargetingFailureReason.None)
        {
            result.SetInvalid(
                pointFailure
            );

            return result;
        }

        geometry.Build(
            request,
            result
        );

        ValidateGeometryTargets(
            request,
            result
        );

        selector.Select(
            result
        );

        FinalizeResult(
            request,
            result
        );

        return result;
    }

    /// <summary>
    /// Validerar requesten och beräknar all gemensam
    /// targetingdata.
    /// </summary>
    private static bool TryPrepareResult(
    ActionRequest request,
    TargetingResult result)
    {
        if (request == null)
        {
            result.SetInvalid(
                TargetingFailureReason.MissingAction
            );

            return false;
        }

        if (request.Caster == null)
        {
            result.SetInvalid(
                TargetingFailureReason.MissingCaster
            );

            return false;
        }

        if (request.Ability == null)
        {
            result.SetInvalid(
                TargetingFailureReason.MissingAction
            );

            return false;
        }

        AbilityTargetingSettings settings =
            request.Ability.TargetingSettings;

        if (settings == null)
        {
            result.SetInvalid(
                TargetingFailureReason.MissingAction
            );

            return false;
        }

        result.Settings =
            settings;

        result.Origin =
            TargetUtility.GetTargetPosition(
                request.Caster.gameObject
            );

        result.RawAimPoint =
            ResolveRawAimPoint(
                request,
                result.Origin,
                settings.TargetingMode
            );

        result.Direction =
            ResolveDirection(
                request,
                result.Origin,
                result.RawAimPoint
            );

        result.EffectiveRange =
            GetEffectiveRange(
                request,
                settings
            );

        result.TargetPoint =
            ResolveTargetPoint(
            result.Origin,
            result.RawAimPoint,
            result.Direction,
            settings,
            result.EffectiveRange
            );

        result.Distance =
            Vector2.Distance(
                result.Origin,
                result.TargetPoint
            );

        return true;
    }

    /// <summary>
    /// Bestämmer den ursprungliga aim-punkten.
    ///
    /// Self använder alltid casterns position.
    ///
    /// SingleTarget använder det explicita targetets position
    /// när ett sådant finns.
    ///
    /// Övriga targetingformer använder RequestedAimPoint.
    /// </summary>
    private static Vector2 ResolveRawAimPoint(
        ActionRequest request,
        Vector2 origin,
        TargetingMode targetingMode)
    {
        if (targetingMode ==
            TargetingMode.Self)
        {
            return origin;
        }

        if (targetingMode ==
                TargetingMode.SingleTarget &&
            request.ExplicitTarget != null)
        {
            GameObject resolvedTarget =
                TargetUtility.ResolveCharacterTarget(
                    request.ExplicitTarget
                );

            return
                TargetUtility.GetTargetPosition(
                    resolvedTarget
                );
        }

        return request.RequestedAimPoint;
    }

    /// <summary>
    /// Bestämmer targetingens riktning.
    ///
    /// Aim-punkten prioriteras när den ger en användbar
    /// riktning. RequestedDirection används som fallback.
    /// </summary>
    private static Vector2 ResolveDirection(
        ActionRequest request,
        Vector2 origin,
        Vector2 rawAimPoint)
    {
        Vector2 aimDelta =
            rawAimPoint - origin;

        if (aimDelta.sqrMagnitude >
            DirectionEpsilon)
        {
            return aimDelta.normalized;
        }

        if (request.HasRequestedDirection)
        {
            return request
                .RequestedDirection
                .normalized;
        }

        return Vector2.down;
    }

    private static float GetEffectiveRange(
    ActionRequest request,
    AbilityTargetingSettings settings)
    {
        if (settings == null)
            return 0f;

        float maximumRange =
            settings.Range;

        if (request == null ||
            !request.HasRangeOverride)
        {
            return maximumRange;
        }

        return Mathf.Clamp(
            request.RangeOverride,
            0f,
            maximumRange
        );
    }

    /// <summary>
    /// Begränsar den slutgiltiga targetpunkten till abilityns
    /// maximum range.
    ///
    /// Ett aim utanför range flyttas till rangegränsen i samma
    /// riktning. Den råa aim-punkten bevaras i RawAimPoint.
    /// </summary>
    /// <summary>
    /// Beräknar den slutgiltiga targetpunkten utifrån abilityns
    /// effektiva runtime-range.
    ///
    /// För Line + FullRange bestämmer aim-punkten endast riktningen.
    /// Linjen sträcks alltid till hela den effektiva räckvidden.
    ///
    /// För övriga former används aim-punkten, begränsad till den
    /// effektiva räckvidden.
    /// </summary>
    private static Vector2 ResolveTargetPoint(
        Vector2 origin,
        Vector2 rawAimPoint,
        Vector2 direction,
        AbilityTargetingSettings settings,
        float effectiveRange)
    {
        if (settings == null)
            return origin;

        if (settings.TargetingMode ==
            TargetingMode.Self)
        {
            return origin;
        }

        float safeRange =
            Mathf.Max(
                0f,
                effectiveRange
            );

        direction =
            direction.sqrMagnitude >
            DirectionEpsilon
                ? direction.normalized
                : Vector2.down;

        /*
         * FullRange-Line använder alltid hela den aktuella
         * runtime-rangen.
         *
         * Charge kan alltså successivt öka linjens längd även om
         * muspekaren står nära spelaren.
         */
        if (settings.TargetingMode ==
                TargetingMode.Line &&
            settings.LineLengthMode ==
                LineLengthMode.FullRange)
        {
            return
                origin +
                direction * safeRange;
        }

        Vector2 aimDelta =
            rawAimPoint - origin;

        float requestedDistance =
            aimDelta.magnitude;

        if (requestedDistance <=
            safeRange)
        {
            return rawAimPoint;
        }

        return
            origin +
            direction * safeRange;
    }

    /// <summary>
    /// Validerar själva världspunkten för targetingformer där
    /// punkten representerar en faktisk destination eller
    /// effektposition.
    ///
    /// SingleTarget valideras mot targetets collider senare.
    /// Cone använder minimum range per target, inte på
    /// riktningspunkten.
    /// Self kräver ingen punktvalidering.
    /// </summary>
    private static TargetingFailureReason
        ValidateTargetingPoint(
            ActionRequest request,
            TargetingResult result)
    {
        TargetingMode mode =
            result.Settings.TargetingMode;

        if (mode == TargetingMode.Self ||
            mode == TargetingMode.SingleTarget ||
            mode == TargetingMode.Cone)
        {
            return TargetingFailureReason.None;
        }

        if (result.Distance + 0.001f <
            result.Settings.MinimumRange)
        {
            return TargetingFailureReason.TooClose;
        }

        bool hasLineOfSight =
            TargetValidator.HasLineOfSightToPoint(
                request.Caster,
                result.Origin,
                result.TargetPoint,
                result.Settings.LineOfSight
            );

        if (!hasLineOfSight)
        {
            return
                TargetingFailureReason.NoLineOfSight;
        }

        return TargetingFailureReason.None;
    }

    /// <summary>
    /// Validerar samtliga geometriska targets och flyttar de
    /// godkända objekten till ValidTargets.
    /// </summary>
    private static void ValidateGeometryTargets(
        ActionRequest request,
        TargetingResult result)
    {
        for (int i = 0;
             i < result.GeometryTargets.Count;
             i++)
        {
            GameObject target =
                result.GeometryTargets[i];

            TargetingFailureReason failure =
                ValidateTarget(
                    request,
                    result,
                    target
                );

            if (failure !=
                TargetingFailureReason.None)
            {
                continue;
            }

            result.AddValidTarget(
                target
            );
        }
    }

    /// <summary>
    /// Kör alla generella gameplayfilter för ett target.
    /// </summary>
    private static TargetingFailureReason
        ValidateTarget(
            ActionRequest request,
            TargetingResult result,
            GameObject target)
    {
        if (target == null)
        {
            return
                TargetingFailureReason.MissingTarget;
        }

        if (!TargetValidator.IsSupportedTarget(
                target))
        {
            return
                TargetingFailureReason.TargetInvalid;
        }

        if (!TargetValidator.IsAlive(
                target))
        {
            return
                TargetingFailureReason.TargetDead;
        }

        if (!TargetValidator.IsAllowedRelation(
                request.Caster,
                target,
                result.Settings))
        {
            return
                TargetingFailureReason.TargetNotAllowed;
        }

        TargetingFailureReason rangeFailure =
            GetTargetRangeFailure(
                result.Origin,
                target,
                result.Settings,
                result.EffectiveRange
            );

        if (rangeFailure !=
            TargetingFailureReason.None)
        {
            return rangeFailure;
        }

        if (!TargetValidator
                .HasLineOfSightToTarget(
                    request.Caster,
                    result.Origin,
                    target,
                    result.Settings.LineOfSight))
        {
            return
                TargetingFailureReason.NoLineOfSight;
        }

        return TargetingFailureReason.None;
    }

    /// <summary>
    /// Returnerar en specifik failure reason för targetets
    /// avstånd i stället för enbart true eller false.
    /// </summary>
    private static TargetingFailureReason
    GetTargetRangeFailure(
        Vector2 origin,
        GameObject target,
        AbilityTargetingSettings settings,
        float effectiveRange)
    {
        Vector2 closestPoint =
            TargetUtility.GetClosestPoint(
                target,
                origin
            );

        float distance =
            Vector2.Distance(
                origin,
                closestPoint
            );

        const float tolerance =
            0.001f;

        if (distance + tolerance <
            settings.MinimumRange)
        {
            return
                TargetingFailureReason.TooClose;
        }

        if (distance - tolerance >
            effectiveRange)
        {
            return
                TargetingFailureReason.OutOfRange;
        }

        return TargetingFailureReason.None;
    }

    /// <summary>
    /// Avgör om det färdiga resultatet kan bekräftas.
    /// </summary>
    private static void FinalizeResult(
        ActionRequest request,
        TargetingResult result)
    {
        TargetingMode mode =
            result.Settings.TargetingMode;

        if (mode ==
                TargetingMode.SingleTarget &&
            request.ExplicitTarget == null)
        {
            result.SetInvalid(
                TargetingFailureReason.MissingTarget
            );

            return;
        }

        if (mode ==
                TargetingMode.SingleTarget &&
            result.AffectedTargets.Count == 0)
        {
            result.SetInvalid(
                GetSingleTargetFailure(
                    request,
                    result
                )
            );

            return;
        }

        if (result.Settings.RequiresAffectedTarget &&
            result.AffectedTargets.Count == 0)
        {
            result.SetInvalid(
                TargetingFailureReason.NoValidTargets
            );

            return;
        }

        result.SetValid();
    }

    /// <summary>
    /// Försöker ge ett single-target-resultat en specifik och
    /// användbar failure reason.
    ///
    /// Detta gör att UI senare kan skilja på exempelvis ett dött
    /// target, fel relation, för långt avstånd och blockerad LoS.
    /// </summary>
    private static TargetingFailureReason
        GetSingleTargetFailure(
            ActionRequest request,
            TargetingResult result)
    {
        if (request.ExplicitTarget == null)
        {
            return
                TargetingFailureReason.MissingTarget;
        }

        GameObject target =
            TargetUtility.ResolveCharacterTarget(
                request.ExplicitTarget
            );

        return ValidateTarget(
            request,
            result,
            target
        );
    }
}

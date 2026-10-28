using UnityEngine;

/// <summary>
/// Samlar targets som befinner sig inom en actions geometriska
/// targetingform.
///
/// Klassen ansvarar endast för fysik och geometri.
/// Den känner inte till faction, livsstatus, line of sight,
/// murder mode eller abilityns slutliga targeturval.
/// </summary>
public sealed class TargetGeometry
{
    private const int DefaultBufferSize = 128;
    private const float MinimumPointRadius = 0.01f;

    private readonly Collider2D[] colliderBuffer;

    public TargetGeometry(
        int bufferSize = DefaultBufferSize)
    {
        colliderBuffer =
            new Collider2D[
                Mathf.Max(1, bufferSize)
            ];
    }

    /// <summary>
    /// Samlar samtliga unika targetobjekt som ligger inom
    /// resultatets targetingform.
    ///
    /// TargetingResult förväntas redan innehålla:
    /// Origin, TargetPoint, Direction, Distance och Settings.
    /// </summary>
    public void Build(
        ActionRequest request,
        TargetingResult result)
    {
        if (request == null ||
            result == null ||
            result.Settings == null)
        {
            return;
        }

        switch (result.Settings.TargetingMode)
        {
            case TargetingMode.Self:
                BuildSelf(
                    request,
                    result
                );
                break;

            case TargetingMode.SingleTarget:
                BuildSingleTarget(
                    request,
                    result
                );
                break;

            case TargetingMode.Point:
                BuildPoint(
                    result
                );
                break;

            case TargetingMode.Circle:
                BuildCircle(
                    result
                );
                break;

            case TargetingMode.Cone:
                BuildCone(
                    result
                );
                break;

            case TargetingMode.Line:
                BuildLine(
                    result
                );
                break;
        }
    }

    private static void BuildSelf(
        ActionRequest request,
        TargetingResult result)
    {
        if (request.Caster == null)
            return;

        result.AddGeometryTarget(
            request.Caster.gameObject
        );
    }

    private static void BuildSingleTarget(
    ActionRequest request,
    TargetingResult result)
    {
        if (request.ExplicitTarget == null)
            return;

        GameObject resolvedTarget =
            TargetUtility.ResolveCharacterTarget(
                request.ExplicitTarget
            );

        if (resolvedTarget == null)
            return;

        if (!HasColliderOnAllowedLayer(
                resolvedTarget,
                result.Settings.TargetLayers))
        {
            return;
        }

        result.AddGeometryTarget(
            resolvedTarget
        );
    }

    /// <summary>
    /// Point-targeting samlar colliders direkt under den valda
    /// världspunkten.
    ///
    /// Point-actions kan fortfarande vara giltiga utan targets.
    /// Exempel är teleport, markeringar och rena markeffekter.
    /// </summary>
    private void BuildPoint(
        TargetingResult result)
    {
        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                result.TargetPoint,
                MinimumPointRadius,
                colliderBuffer,
                result.Settings.TargetLayers
            );

        AddResolvedTargets(
            hitCount,
            result
        );
    }

    /// <summary>
    /// Circle använder TargetPoint som cirkelns centrum.
    /// </summary>
    private void BuildCircle(
        TargetingResult result)
    {
        float radius =
            Mathf.Max(
                0f,
                result.Settings.Radius
            );

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                result.TargetPoint,
                radius,
                colliderBuffer,
                result.Settings.TargetLayers
            );

        AddResolvedTargets(
            hitCount,
            result
        );
    }

    /// <summary>
    /// Cone börjar vid Origin och sträcker sig hela abilityns
    /// maximala range i Direction.
    ///
    /// OverlapCircle samlar först möjliga kandidater. Därefter
    /// filtreras kandidaterna med ett vinkeltest.
    /// </summary>
    private void BuildCone(
        TargetingResult result)
    {
        float range =
            Mathf.Max(
                0f,
                result.EffectiveRange
            );

        if (range <= 0f)
            return;

        int hitCount =
            Physics2D.OverlapCircleNonAlloc(
                result.Origin,
                range,
                colliderBuffer,
                result.Settings.TargetLayers
            );

        float halfAngle =
            Mathf.Clamp(
                result.Settings.ConeAngle * 0.5f,
                0f,
                180f
            );

        Vector2 direction =
            GetSafeDirection(
                result.Direction
            );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D candidateCollider =
                colliderBuffer[i];

            if (candidateCollider == null)
                continue;

            GameObject candidate =
                TargetUtility.ResolveTargetObject(
                    candidateCollider
                );

            if (candidate == null)
                continue;

            Vector2 candidatePoint =
                TargetUtility.GetClosestPoint(
                    candidate,
                    result.Origin
                );

            Vector2 toCandidate =
                candidatePoint - result.Origin;

            if (toCandidate.sqrMagnitude <=
                Mathf.Epsilon)
            {
                result.AddGeometryTarget(
                    candidate
                );

                continue;
            }

            float angle =
                Vector2.Angle(
                    direction,
                    toCandidate.normalized
                );

            if (angle > halfAngle)
                continue;

            result.AddGeometryTarget(
                candidate
            );
        }

        WarnIfBufferWasFilled(
            hitCount,
            TargetingMode.Cone
        );

        ClearUsedBufferEntries(hitCount);
    }

    /// <summary>
    /// Line skapar en orienterad box mellan Origin och
    /// TargetPoint.
    ///
    /// LineWidth bestämmer boxens tjocklek och resultatets
    /// Distance bestämmer dess längd.
    /// </summary>
    private void BuildLine(
    TargetingResult result)
    {
        if (result == null ||
            result.Settings == null)
        {
            return;
        }

        Vector2 direction =
            GetSafeDirection(
                result.Direction
            );

        /*
         * Distance är avståndet från Origin till den slutliga
         * TargetPoint.
         *
         * För FullRange är detta alltid abilityns Range.
         * För ToCursor kan det vara kortare.
         */
        float endDistance =
            Mathf.Max(
                0f,
                result.Distance
            );

        /*
         * MinimumRange skapar en tom lucka närmast castern.
         */
        float startDistance =
            Mathf.Clamp(
                result.Settings.MinimumRange,
                0f,
                endDistance
            );

        float lineLength =
            endDistance -
            startDistance;

        if (lineLength <= 0f)
            return;

        float width =
            Mathf.Max(
                MinimumPointRadius,
                result.Settings.LineWidth
            );

        Vector2 lineStart =
            result.Origin +
            direction *
            startDistance;

        Vector2 center =
            lineStart +
            direction *
            (
                lineLength *
                0.5f
            );

        Vector2 size =
            new Vector2(
                lineLength,
                width
            );

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        int hitCount =
            Physics2D.OverlapBoxNonAlloc(
                center,
                size,
                angle,
                colliderBuffer,
                result.Settings.TargetLayers
            );

        AddResolvedTargets(
            hitCount,
            result
        );
    }

    /// <summary>
    /// Normaliserar colliderträffar till stabila targetobjekt
    /// och lägger dem i GeometryTargets.
    /// </summary>
    private void AddResolvedTargets(
        int hitCount,
        TargetingResult result)
    {
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider =
                colliderBuffer[i];

            if (hitCollider == null)
                continue;

            GameObject resolvedTarget =
                TargetUtility.ResolveTargetObject(
                    hitCollider
                );

            result.AddGeometryTarget(
                resolvedTarget
            );
        }

        WarnIfBufferWasFilled(
            hitCount,
            result.Settings.TargetingMode
        );

        ClearUsedBufferEntries(hitCount);
    }

    /// <summary>
    /// Förhindrar att gamla colliderreferenser ligger kvar i
    /// buffern mellan targetingberäkningar.
    /// </summary>
    private void ClearUsedBufferEntries(
        int hitCount)
    {
        int safeCount =
            Mathf.Min(
                hitCount,
                colliderBuffer.Length
            );

        for (int i = 0; i < safeCount; i++)
        {
            colliderBuffer[i] = null;
        }
    }

    private void WarnIfBufferWasFilled(
        int hitCount,
        TargetingMode targetingMode)
    {
        if (hitCount < colliderBuffer.Length)
            return;

        Debug.LogWarning(
            $"TargetGeometry-buffer på " +
            $"{colliderBuffer.Length} colliders fylldes för " +
            $"{targetingMode}. Några targets kan ha utelämnats. " +
            $"Skapa TargetGeometry med en större buffer om " +
            $"detta händer regelbundet."
        );
    }

    private static Vector2 GetSafeDirection(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            Mathf.Epsilon)
        {
            return Vector2.down;
        }

        return direction.normalized;
    }

    private static bool HasColliderOnAllowedLayer(
    GameObject target,
    LayerMask allowedLayers)
    {
        if (target == null)
            return false;

        Collider2D[] colliders =
            target.GetComponentsInChildren<
                Collider2D
            >(true);

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider2D targetCollider =
                colliders[i];

            if (targetCollider == null)
                continue;

            int colliderLayerMask =
                1 << targetCollider.gameObject.layer;

            if ((allowedLayers.value &
                 colliderLayerMask) != 0)
            {
                return true;
            }
        }

        return false;
    }
}

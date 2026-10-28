using UnityEngine;

public static class TargetValidator
{
    /// <summary>
    /// Kontrollerar om objektet tillhör en targettyp som det
    /// nuvarande systemet kan hantera.
    ///
    /// Just nu stöds CharacterStats. DestructibleObject läggs
    /// till när dess slutgiltiga komponent finns.
    /// </summary>
    public static bool IsSupportedTarget(
        GameObject target)
    {
        if (target == null)
            return false;

        return
            TargetUtility.GetCharacterStats(target) != null;
    }

    /// <summary>
    /// Kontrollerar targetens livsstatus.
    ///
    /// Icke-karaktärer betraktas tills vidare som levande.
    /// När DestructibleObject byggs frågar vi även dess state.
    /// </summary>
    public static bool IsAlive(
        GameObject target)
    {
        if (target == null)
            return false;

        CharacterStats stats =
            TargetUtility.GetCharacterStats(target);

        if (stats == null)
            return true;

        return stats.currentHP > 0;
    }

    /// <summary>
    /// Kontrollerar om targetens verkliga relation är tillåten.
    ///
    /// För spelaren kan Murder mode ge ett särskilt undantag
    /// när abilityn tillåter Hostile targets.
    /// </summary>
    public static bool IsAllowedRelation(
        CharacterStats caster,
        GameObject target,
        AbilityTargetingSettings settings)
    {
        if (caster == null ||
            target == null ||
            settings == null)
        {
            return false;
        }

        TargetRelation relation =
            TargetRelationResolver.Resolve(
                caster,
                target
            );

        if (settings.AllowsRelation(relation))
            return true;

        return HasMurderTargetingOverride(
            caster,
            target,
            settings
        );
    }

    /// <summary>
    /// Murder mode innebär att spelaren frivilligt har tillåtit
    /// offensiv targeting mot den aktuella factionen.
    ///
    /// Undantaget gäller endast när abilityn tillåter Hostile,
    /// eftersom en heal eller friendly buff inte ska påverkas.
    /// </summary>
    public static bool HasMurderTargetingOverride(
        CharacterStats caster,
        GameObject target,
        AbilityTargetingSettings settings)
    {
        if (caster == null ||
            target == null ||
            settings == null)
        {
            return false;
        }

        if (!settings.AllowsRelation(
                TargetRelation.Hostile))
        {
            return false;
        }

        PlayerStats player =
            caster as PlayerStats;

        if (player == null)
            return false;

        CharacterStats targetStats =
            TargetUtility.GetCharacterStats(target);

        if (targetStats == null ||
            targetStats == caster ||
            targetStats.faction == null)
        {
            return false;
        }

        PlayerReputationManager reputation =
            player.GetComponent<
                PlayerReputationManager
            >();

        if (reputation == null)
            return false;

        return reputation.IsMurderEnabled(
            targetStats.faction
        );
    }

    /// <summary>
    /// Kontrollerar om en target befinner sig inom abilityns
    /// minimum- och maximum-range.
    ///
    /// Range mäts till närmaste punkt på targetens collider,
    /// vilket fungerar bättre för stora mål.
    /// </summary>
    public static bool IsWithinRange(
        Vector2 origin,
        GameObject target,
        AbilityTargetingSettings settings)
    {
        if (target == null || settings == null)
            return false;

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

        return IsDistanceWithinRange(
            distance,
            settings
        );
    }

    /// <summary>
    /// Kontrollerar ett redan beräknat avstånd.
    /// </summary>
    public static bool IsDistanceWithinRange(
        float distance,
        AbilityTargetingSettings settings)
    {
        if (settings == null)
            return false;

        float safeDistance =
            Mathf.Max(0f, distance);

        const float tolerance = 0.001f;

        if (safeDistance + tolerance <
            settings.MinimumRange)
        {
            return false;
        }

        if (safeDistance - tolerance >
            settings.Range)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Kontrollerar LoS från origin till targetens närmaste
    /// colliderpunkt.
    /// </summary>
    public static bool HasLineOfSightToTarget(
        CharacterStats caster,
        Vector2 origin,
        GameObject target,
        AbilityLineOfSightSettings settings)
    {
        if (target == null)
            return false;

        Vector2 targetPoint =
            TargetUtility.GetClosestPoint(
                target,
                origin
            );

        return HasLineOfSightToPoint(
            caster,
            origin,
            targetPoint,
            settings
        );
    }

    /// <summary>
    /// Kontrollerar om en blockerande collider finns mellan
    /// origin och targetPoint.
    ///
    /// Caster-entitetens egna colliders ignoreras.
    /// </summary>
    public static bool HasLineOfSightToPoint(
        CharacterStats caster,
        Vector2 origin,
        Vector2 targetPoint,
        AbilityLineOfSightSettings settings)
    {
        if (settings == null ||
            !settings.RequiresLineOfSight)
        {
            return true;
        }

        Vector2 delta =
            targetPoint - origin;

        float distance =
            delta.magnitude;

        if (distance <= 0.0001f)
            return true;

        Vector2 direction =
            delta / distance;

        float offset =
            Mathf.Min(
                settings.OriginOffset,
                distance
            );

        Vector2 rayOrigin =
            origin + direction * offset;

        float rayDistance =
            Mathf.Max(
                0f,
                distance - offset
            );

        RaycastHit2D[] hits =
            Physics2D.RaycastAll(
                rayOrigin,
                direction,
                rayDistance,
                settings.BlockingLayers
            );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider =
                hits[i].collider;

            if (hitCollider == null)
                continue;

            CharacterStats hitOwner =
                TargetUtility.GetCharacterStats(
                    hitCollider
                );

            if (caster != null &&
                hitOwner == caster)
            {
                continue;
            }

            return false;
        }

        return true;
    }
}

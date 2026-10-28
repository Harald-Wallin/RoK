using UnityEngine;

public static class TargetUtility
{
    /// <summary>
    /// Försöker hitta CharacterStats på objektet eller på någon
    /// av dess föräldrar.
    ///
    /// Detta gör att colliders kan ligga på child-objekt utan
    /// att targetingen behandlar varje collider som en egen target.
    /// </summary>
    public static CharacterStats GetCharacterStats(
        GameObject target)
    {
        if (target == null)
            return null;

        return target.GetComponentInParent<CharacterStats>();
    }

    /// <summary>
    /// Försöker hitta CharacterStats från en collider.
    /// </summary>
    public static CharacterStats GetCharacterStats(
        Collider2D collider)
    {
        if (collider == null)
            return null;

        return collider.GetComponentInParent<CharacterStats>();
    }

    /// <summary>
    /// Returnerar det GameObject som representerar karaktären
    /// som helhet.
    ///
    /// Om objektet inte tillhör en CharacterStats-entitet
    /// returneras det ursprungliga objektet.
    /// </summary>
    public static GameObject ResolveCharacterTarget(
        GameObject target)
    {
        if (target == null)
            return null;

        CharacterStats stats =
            GetCharacterStats(target);

        return stats != null
            ? stats.gameObject
            : target;
    }

    /// <summary>
    /// Returnerar ett stabilt targetobjekt från en collider.
    ///
    /// Karaktärer normaliseras till GameObjectet med
    /// CharacterStats. För andra fysikobjekt används i första
    /// hand objektet som äger dess Rigidbody2D.
    ///
    /// Stöd för DestructibleObject läggs till när den komponenten
    /// finns och vi har bestämt dess exakta API.
    /// </summary>
    public static GameObject ResolveTargetObject(
        Collider2D collider)
    {
        if (collider == null)
            return null;

        CharacterStats stats =
            GetCharacterStats(collider);

        if (stats != null)
            return stats.gameObject;

        if (collider.attachedRigidbody != null)
        {
            return collider
                .attachedRigidbody
                .gameObject;
        }

        return collider.gameObject;
    }

    /// <summary>
    /// Returnerar den bästa representativa världspositionen för
    /// ett target.
    ///
    /// Collider bounds används när en collider finns. Det ger
    /// bättre avståndsmätning än transform.position för stora
    /// eller offsetade objekt.
    /// </summary>
    public static Vector2 GetTargetPosition(
        GameObject target)
    {
        if (target == null)
            return Vector2.zero;

        Collider2D collider =
            target.GetComponentInChildren<Collider2D>();

        if (collider != null)
            return collider.bounds.center;

        return target.transform.position;
    }

    /// <summary>
    /// Hittar den punkt på targetens collider som ligger närmast
    /// den angivna världspositionen.
    ///
    /// Användbart för range- och LoS-kontroller mot större mål.
    /// </summary>
    public static Vector2 GetClosestPoint(
        GameObject target,
        Vector2 fromPosition)
    {
        if (target == null)
            return fromPosition;

        Collider2D collider =
            target.GetComponentInChildren<Collider2D>();

        if (collider == null)
            return target.transform.position;

        return collider.ClosestPoint(fromPosition);
    }

    /// <summary>
    /// Kontrollerar om targetobjektet representerar samma
    /// CharacterStats-entitet som castern.
    /// </summary>
    public static bool IsSelf(
        CharacterStats caster,
        GameObject target)
    {
        if (caster == null || target == null)
            return false;

        CharacterStats targetStats =
            GetCharacterStats(target);

        return targetStats == caster;
    }

    /// <summary>
    /// Kontrollerar om objektet tillhör en karaktär.
    /// </summary>
    public static bool IsCharacter(
        GameObject target)
    {
        return GetCharacterStats(target) != null;
    }

    /// <summary>
    /// Avgör den gameplayrelation som target har i förhållande
    /// till castern.
    /// </summary>
    public static TargetRelation GetRelation(
        CharacterStats caster,
        CharacterStats target)
    {
        if (caster == null ||
            target == null)
        {
            return TargetRelation.None;
        }

        if (caster == target)
        {
            return TargetRelation.Self;
        }

        /*
         * Båda riktningarna kontrolleras eftersom faction- och
         * reputationdata kan vara asymmetrisk.
         */
        if (caster.IsHostileTo(target) ||
            target.IsHostileTo(caster))
        {
            return TargetRelation.Hostile;
        }

        if (caster.IsFriendlyTo(target) ||
            target.IsFriendlyTo(caster))
        {
            return TargetRelation.Friendly;
        }

        return TargetRelation.Neutral;
    }
}

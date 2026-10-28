using System;
using UnityEngine;

public static class EntityTargetUtility
{
    public static EntityIdentity GetIdentity(
        UnityEngine.Object target)
    {
        if (target == null)
            return null;

        if (target is GameObject gameObject)
        {
            return GetIdentity(
                gameObject
            );
        }

        if (target is Component component)
        {
            return GetIdentity(
                component.gameObject
            );
        }

        return null;
    }

    public static EntityIdentity GetIdentity(
        GameObject target)
    {
        if (target == null)
            return null;

        EntityIdentity identity =
            target.GetComponent<
                EntityIdentity>();

        if (identity != null)
            return identity;

        identity =
            target.GetComponentInParent<
                EntityIdentity>();

        if (identity != null)
            return identity;

        return target
            .GetComponentInChildren<
                EntityIdentity>(
                true
            );
    }

    public static string GetId(
        GameObject target)
    {
        EntityIdentity identity =
            GetIdentity(
                target
            );

        return identity != null
            ? identity.Id
            : string.Empty;
    }

    public static string GetDisplayName(
        GameObject target)
    {
        EntityIdentity identity =
            GetIdentity(
                target
            );

        return identity != null
            ? identity.DisplayName
            : target != null
                ? target.name
                : string.Empty;
    }

    public static bool Matches(
        EntityIdentity runtimeIdentity,
        string targetEntityId)
    {
        if (runtimeIdentity == null ||
            string.IsNullOrWhiteSpace(
                runtimeIdentity.Id) ||
            string.IsNullOrWhiteSpace(
                targetEntityId))
        {
            return false;
        }

        return string.Equals(
            runtimeIdentity.Id,
            targetEntityId,
            StringComparison.Ordinal
        );
    }

    public static bool Matches(
        string firstEntityId,
        string secondEntityId)
    {
        if (string.IsNullOrWhiteSpace(
                firstEntityId) ||
            string.IsNullOrWhiteSpace(
                secondEntityId))
        {
            return false;
        }

        return string.Equals(
            firstEntityId,
            secondEntityId,
            StringComparison.Ordinal
        );
    }
}
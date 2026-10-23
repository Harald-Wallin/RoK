
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Connects rebuilt building collision geometry
/// to the existing Navigation V3 system.
///
/// Only affected navigation regions are rebaked.
///
/// This is an event-driven integration:
/// no Update, no independent navigation grid.
/// </summary>
public static class BuildingNavigationSync
{
    // Remember the last generated collision area
    // so moving and rebuilding a building can
    // refresh both its old and new locations.
    private static readonly Dictionary<int, Bounds>
        lastKnownBounds = new();

    // =====================================================
    // COLLIDER BOUNDS
    // =====================================================

    public static Bounds? GetColliderBounds(
        Transform collisionRoot)
    {
        if (collisionRoot == null)
            return null;

        Collider2D[] colliders =
            collisionRoot.GetComponentsInChildren<
                Collider2D>(true);

        Bounds? result = null;

        foreach (Collider2D collider in colliders)
        {
            if (collider == null ||
                !collider.enabled ||
                !collider.gameObject.activeInHierarchy ||
                collider.isTrigger)
            {
                continue;
            }

            if (!result.HasValue)
            {
                result = collider.bounds;
                continue;
            }

            Bounds combined = result.Value;
            combined.Encapsulate(collider.bounds);
            result = combined;
        }

        return result;
    }

    // =====================================================
    // BUILDING COLLISION CHANGED
    // =====================================================

    public static void NotifyCollidersRebuilt(
        BuildingRoot root,
        Transform collisionRoot,
        Bounds? previousColliderBounds)
    {
        if (!Application.isPlaying ||
            root == null)
        {
            return;
        }

        // Ensure newly generated collider transforms
        // are visible to Physics2D queries.
        Physics2D.SyncTransforms();

        Bounds? currentBounds =
            GetColliderBounds(collisionRoot);

        int rootId = root.GetInstanceID();

        Bounds? oldBounds =
            previousColliderBounds;

        if (lastKnownBounds.TryGetValue(
                rootId,
                out Bounds lastBounds))
        {
            if (oldBounds.HasValue)
            {
                Bounds combined = oldBounds.Value;
                combined.Encapsulate(lastBounds);
                oldBounds = combined;
            }
            else
            {
                oldBounds = lastBounds;
            }
        }

        if (currentBounds.HasValue)
        {
            lastKnownBounds[rootId] =
                currentBounds.Value;
        }
        else
        {
            lastKnownBounds.Remove(rootId);
        }

        if (!NavigationWorld.HasInstance)
            return;

        NavigationWorld world =
            NavigationWorld.Instance;

        bool anyRegionRebaked = false;

        // =================================================
        // REFRESH AFFECTED REGIONS
        // =================================================

        foreach (NavigationRegion region
                 in world.Regions)
        {
            if (region == null ||
                !region.isActiveAndEnabled)
            {
                continue;
            }

            float clearance =
                region.NavigationRadius;

            bool affected =
                (oldBounds.HasValue &&
                 AffectsRegion(
                     region.RegionBounds,
                     oldBounds.Value,
                     clearance)) ||

                (currentBounds.HasValue &&
                 AffectsRegion(
                     region.RegionBounds,
                     currentBounds.Value,
                     clearance));

            if (!affected)
                continue;

            region.Bake();
            anyRegionRebaked = true;
        }

        if (!anyRegionRebaked)
            return;

        // =================================================
        // INVALIDATE OLD NPC PATHS
        // =================================================

        NPCNavigationAgent[] agents =
            Object.FindObjectsByType<
                NPCNavigationAgent>(
                    FindObjectsSortMode.None
                );

        foreach (NPCNavigationAgent agent in agents)
        {
            if (agent == null ||
                !agent.isActiveAndEnabled ||
                !agent.HasDestination)
            {
                continue;
            }

            // Existing navigation handles scheduling
            // the replacement path.
            agent.ForceRepath();
        }
    }

    // =====================================================
    // REGION INTERSECTION
    // =====================================================

    private static bool AffectsRegion(
        Bounds region,
        Bounds obstacle,
        float clearance)
    {
        float padding =
            Mathf.Max(0f, clearance);

        return
            region.min.x <=
                obstacle.max.x + padding &&

            region.max.x >=
                obstacle.min.x - padding &&

            region.min.y <=
                obstacle.max.y + padding &&

            region.max.y >=
                obstacle.min.y - padding;
    }
}

